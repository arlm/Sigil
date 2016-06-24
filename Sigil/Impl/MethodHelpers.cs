using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
#if COREFX
using System.IO;
using System.Linq;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Decoding;
using System.Reflection.PortableExecutable;
#endif

namespace Sigil.Impl
{
#if COREFX

    /// <remarks>
    /// Source: https://github.com/dotnet/corefx/blob/d595e342f948bb1e81696a4bf67be82461917c3c/src/System.Reflection.Metadata/src/System/Reflection/System.Reflection.cs#L227
    /// </remarks>
    internal static class TypeAttributesExtensions
    {
        // This flag will be added to the BCL (Bug #1041207), but we still 
        // need to define a copy here for downlevel portability.
        private const TypeAttributes Forwarder = (TypeAttributes)0x00200000;

        // This mask is the fastest way to check if a type is nested from its flags,
        // but it should not be added to the BCL enum as its semantics can be misleading.
        // Consider, for example, that (NestedFamANDAssem & NestedMask) == NestedFamORAssem.
        // Only comparison of the masked value to 0 is meaningful, which is different from
        // the other masks in the enum.
        private const TypeAttributes NestedMask = (TypeAttributes)0x00000006;

        public static bool IsForwarder(this TypeAttributes flags)
        {
            return (flags & Forwarder) != 0;
        }

        public static bool IsNested(this TypeAttributes flags)
        {
            return (flags & NestedMask) != 0;
        }
    }

#endif

    /// <summary>
    /// Contains helper methods to shim over the difference between different Type APIs in
    /// different frameworks
    /// </summary>
    internal static class MethodHelpers
    {
        internal class LocalVariable
        {
            public bool IsPinned { get; set; }
            public int LocalIndex { get; set; }
            public Type LocalType { get; set; }
        }

        internal class ExceptionClause
        {
            public Type CatchType { get; set; } // Returns a TypeRef, TypeDef, or TypeSpec handle if the region represents a catch, nil token otherwise.
            public int FilterOffset { get; set; } // IL offset of the start of the filter block, or -1 if the region is not a filter.
            public int HandlerLength { get; set; }
            public int HandlerOffset { get; set; }
            public ExceptionClauseKind Kind { get; set; }
            public int TryLength { get; set; }
            public int TryOffset { get; set; }
        }

        [Flags]
        internal enum ExceptionClauseKind : ushort
        {
            Catch = 0,
            Filter = 1,
            Finally = 2,
            Fault = 4
        }

#if COREFX

        /// <remarks>
        /// Source: https://github.com/dotnet/corefx/blob/7152971f4a940aa897c29fcdbed8934a692a928e/src/System.Reflection.Metadata/tests/Metadata/Decoding/DisassemblingTypeProvider.cs
        /// </remarks>
        internal class DisassemblingTypeProvider : ISignatureTypeProvider<string>
        {
            public virtual string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Boolean:
                        return "bool";

                    case PrimitiveTypeCode.Byte:
                        return "uint8";

                    case PrimitiveTypeCode.Char:
                        return "char";

                    case PrimitiveTypeCode.Double:
                        return "float64";

                    case PrimitiveTypeCode.Int16:
                        return "int16";

                    case PrimitiveTypeCode.Int32:
                        return "int32";

                    case PrimitiveTypeCode.Int64:
                        return "int64";

                    case PrimitiveTypeCode.IntPtr:
                        return "native int";

                    case PrimitiveTypeCode.Object:
                        return "object";

                    case PrimitiveTypeCode.SByte:
                        return "int8";

                    case PrimitiveTypeCode.Single:
                        return "float32";

                    case PrimitiveTypeCode.String:
                        return "string";

                    case PrimitiveTypeCode.TypedReference:
                        return "typedref";

                    case PrimitiveTypeCode.UInt16:
                        return "uint16";

                    case PrimitiveTypeCode.UInt32:
                        return "uint32";

                    case PrimitiveTypeCode.UInt64:
                        return "uint64";

                    case PrimitiveTypeCode.UIntPtr:
                        return "native uint";

                    case PrimitiveTypeCode.Void:
                        return "void";

                    default:
                        //Debug.Assert(false);
                        throw new ArgumentOutOfRangeException(nameof(typeCode));
                }
            }

            public virtual string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);

                string name = definition.Namespace.IsNil
                    ? reader.GetString(definition.Name)
                    : reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

                if (definition.Attributes.IsNested())
                {
                    TypeDefinitionHandle declaringTypeHandle = definition.GetDeclaringType();
                    return GetTypeFromDefinition(reader, declaringTypeHandle, 0) + "/" + name;
                }

                return name;
            }

            public virtual string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                TypeReference reference = reader.GetTypeReference(handle);
                Handle scope = reference.ResolutionScope;

                string name = reference.Namespace.IsNil
                    ? reader.GetString(reference.Name)
                    : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

                switch (scope.Kind)
                {
                    case HandleKind.ModuleReference:
                        return "[.module  " + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)scope).Name) + "]" + name;

                    case HandleKind.AssemblyReference:
                        var assemblyReferenceHandle = (AssemblyReferenceHandle)scope;
                        var assemblyReference = reader.GetAssemblyReference(assemblyReferenceHandle);
                        return "[" + reader.GetString(assemblyReference.Name) + "]" + name;

                    case HandleKind.TypeReference:
                        return GetTypeFromReference(reader, (TypeReferenceHandle)scope) + "/" + name;

                    default:
                        // rare cases:  ModuleDefinition means search within defs of current module (used by WinMDs for projections)
                        //              nil means search exported types of same module (haven't seen this in practice). For the test
                        //              purposes here, it's sufficient to format both like defs.
                        //Debug.Assert(scope == Handle.ModuleDefinition || scope.IsNil);
                        return name;
                }
            }

            public virtual string GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                return reader.GetTypeSpecification(handle).DecodeSignature(this);
            }

            public virtual string GetSZArrayType(string elementType)
            {
                return elementType + "[]";
            }

            public virtual string GetPointerType(string elementType)
            {
                return elementType + "*";
            }

            public virtual string GetByReferenceType(string elementType)
            {
                return elementType + "&";
            }

            public virtual string GetGenericMethodParameter(int index)
            {
                return "!!" + index;
            }

            public virtual string GetGenericTypeParameter(int index)
            {
                return "!" + index;
            }

            public virtual string GetPinnedType(string elementType)
            {
                return elementType + " pinned";
            }

            public virtual string GetGenericInstance(string genericType, ImmutableArray<string> typeArguments)
            {
                return genericType + "<" + String.Join(",", typeArguments) + ">";
            }

            public virtual string GetArrayType(string elementType, ArrayShape shape)
            {
                var builder = new StringBuilder();

                builder.Append(elementType);
                builder.Append('[');

                for (int i = 0; i < shape.Rank; i++)
                {
                    int lowerBound = 0;

                    if (i < shape.LowerBounds.Length)
                    {
                        lowerBound = shape.LowerBounds[i];
                        builder.Append(lowerBound);
                    }

                    builder.Append("...");

                    if (i < shape.Sizes.Length)
                    {
                        builder.Append(lowerBound + shape.Sizes[i] - 1);
                    }

                    if (i < shape.Rank - 1)
                    {
                        builder.Append(',');
                    }
                }

                builder.Append(']');
                return builder.ToString();
            }

            public virtual string GetTypeFromHandle(MetadataReader reader, EntityHandle handle)
            {
                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        return GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle);

                    case HandleKind.TypeReference:
                        return GetTypeFromReference(reader, (TypeReferenceHandle)handle);

                    case HandleKind.TypeSpecification:
                        return GetTypeFromSpecification(reader, (TypeSpecificationHandle)handle);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(handle));
                }
            }

            public virtual string GetModifiedType(MetadataReader reader, bool isRequired, string modifierType, string unmodifiedType)
            {
                return unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";
            }

            public virtual string GetFunctionPointerType(MethodSignature<string> signature)
            {
                ImmutableArray<string> parameterTypes = signature.ParameterTypes;

                int requiredParameterCount = signature.RequiredParameterCount;

                var builder = new StringBuilder();
                builder.Append("method ");
                builder.Append(signature.ReturnType);
                builder.Append(" *(");

                int i;
                for (i = 0; i < requiredParameterCount; i++)
                {
                    builder.Append(parameterTypes[i]);
                    if (i < parameterTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }

                if (i < parameterTypes.Length)
                {
                    builder.Append("..., ");
                    for (; i < parameterTypes.Length; i++)
                    {
                        builder.Append(parameterTypes[i]);
                        if (i < parameterTypes.Length - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }

                builder.Append(')');
                return builder.ToString();
            }
        }

        internal class LocalVariableProvider : ISignatureTypeProvider<LocalVariable>
        {
            public virtual LocalVariable GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Boolean:
                        return new LocalVariable { LocalType = typeof(bool) };

                    case PrimitiveTypeCode.Byte:
                        return new LocalVariable { LocalType = typeof(byte) };

                    case PrimitiveTypeCode.Char:
                        return new LocalVariable { LocalType = typeof(char) };

                    case PrimitiveTypeCode.Double:
                        return new LocalVariable { LocalType = typeof(double) };

                    case PrimitiveTypeCode.Int16:
                        return new LocalVariable { LocalType = typeof(short) };

                    case PrimitiveTypeCode.Int32:
                        return new LocalVariable { LocalType = typeof(int) };

                    case PrimitiveTypeCode.Int64:
                        return new LocalVariable { LocalType = typeof(long) };

                    case PrimitiveTypeCode.IntPtr:
                        return new LocalVariable { LocalType = typeof(IntPtr) };

                    case PrimitiveTypeCode.Object:
                        return new LocalVariable { LocalType = typeof(object) };

                    case PrimitiveTypeCode.SByte:
                        return new LocalVariable { LocalType = typeof(sbyte) };

                    case PrimitiveTypeCode.Single:
                        return new LocalVariable { LocalType = typeof(float) };

                    case PrimitiveTypeCode.String:
                        return new LocalVariable { LocalType = typeof(string) };

                    case PrimitiveTypeCode.TypedReference:
                        throw new NotSupportedException();
                    //return "typedref";
                    //return new LocalVariable { LocalType = typeof(TypedReference) };

                    case PrimitiveTypeCode.UInt16:
                        return new LocalVariable { LocalType = typeof(ushort) };

                    case PrimitiveTypeCode.UInt32:
                        return new LocalVariable { LocalType = typeof(uint) };

                    case PrimitiveTypeCode.UInt64:
                        return new LocalVariable { LocalType = typeof(ulong) };

                    case PrimitiveTypeCode.UIntPtr:
                        return new LocalVariable { LocalType = typeof(UIntPtr) };

                    case PrimitiveTypeCode.Void:
                        return new LocalVariable { LocalType = typeof(void) };

                    default:
                        //Debug.Assert(false);
                        throw new ArgumentOutOfRangeException(nameof(typeCode));
                }
            }

            public virtual LocalVariable GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);

                string name = definition.Namespace.IsNil
                    ? reader.GetString(definition.Name)
                    : reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

                // TODO: Make support
                //if (definition.Attributes.IsNested())
                //{
                //    TypeDefinitionHandle declaringTypeHandle = definition.GetDeclaringType();
                //    return GetTypeFromDefinition(reader, declaringTypeHandle, 0) + "/" + name;
                //}

                return new LocalVariable { LocalType = Type.GetType(name) };
            }

            public virtual LocalVariable GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                TypeReference reference = reader.GetTypeReference(handle);
                Handle scope = reference.ResolutionScope;

                string name = reference.Namespace.IsNil
                    ? reader.GetString(reference.Name)
                    : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

                switch (scope.Kind)
                {
                    // TODO: Make support
                    //case HandleKind.ModuleReference:
                    //    return "[.module  " + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)scope).Name) + "]" + name;

                    //case HandleKind.AssemblyReference:
                    //    var assemblyReferenceHandle = (AssemblyReferenceHandle)scope;
                    //    var assemblyReference = reader.GetAssemblyReference(assemblyReferenceHandle);
                    //    return "[" + reader.GetString(assemblyReference.Name) + "]" + name;

                    //case HandleKind.TypeReference:
                    //    return GetTypeFromReference(reader, (TypeReferenceHandle)scope) + "/" + name;

                    default:
                        // rare cases:  ModuleDefinition means search within defs of current module (used by WinMDs for projections)
                        //              nil means search exported types of same module (haven't seen this in practice). For the test
                        //              purposes here, it's sufficient to format both like defs.
                        //Debug.Assert(scope == Handle.ModuleDefinition || scope.IsNil);
                        return new LocalVariable { LocalType = Type.GetType(name) };
                }
            }

            public virtual LocalVariable GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode rawTypeKind = 0)
            {
                return reader.GetTypeSpecification(handle).DecodeSignature(this);
            }

            public virtual LocalVariable GetSZArrayType(LocalVariable elementType)
            {
                elementType.LocalType = elementType.LocalType.MakeArrayType();
                return elementType;
            }

            public virtual LocalVariable GetPointerType(LocalVariable elementType)
            {
                elementType.LocalType = elementType.LocalType.MakePointerType();
                return elementType;
            }

            public virtual LocalVariable GetByReferenceType(LocalVariable elementType)
            {
                elementType.LocalType = elementType.LocalType.MakeByRefType();
                return elementType;
            }

            public virtual LocalVariable GetGenericMethodParameter(int index)
            {
                throw new NotSupportedException();
            }

            public virtual LocalVariable GetGenericTypeParameter(int index)
            {
                throw new NotSupportedException();
            }

            public virtual LocalVariable GetPinnedType(LocalVariable elementType)
            {
                elementType.IsPinned = true;
                return elementType;
            }

            public virtual LocalVariable GetGenericInstance(LocalVariable genericType, ImmutableArray<LocalVariable> typeArguments)
            {
                var genericArguments = new Type[typeArguments.Length];
                for (int i = 0; i < typeArguments.Length; i++)
                    genericArguments[i] = typeArguments[i].LocalType;
                return new LocalVariable { LocalType = genericType.LocalType.MakeGenericType(genericArguments) };
            }

            public virtual LocalVariable GetArrayType(LocalVariable elementType, ArrayShape shape)
            {
                throw new NotSupportedException();
                //var builder = new StringBuilder();

                //builder.Append(elementType);
                //builder.Append('[');

                //for (int i = 0; i < shape.Rank; i++)
                //{
                //    int lowerBound = 0;

                //    if (i < shape.LowerBounds.Length)
                //    {
                //        lowerBound = shape.LowerBounds[i];
                //        builder.Append(lowerBound);
                //    }

                //    builder.Append("...");

                //    if (i < shape.Sizes.Length)
                //    {
                //        builder.Append(lowerBound + shape.Sizes[i] - 1);
                //    }

                //    if (i < shape.Rank - 1)
                //    {
                //        builder.Append(',');
                //    }
                //}

                //builder.Append(']');
                //return builder.ToString();
            }

            public virtual LocalVariable GetTypeFromHandle(MetadataReader reader, EntityHandle handle)
            {
                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        return GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle);

                    case HandleKind.TypeReference:
                        return GetTypeFromReference(reader, (TypeReferenceHandle)handle);

                    case HandleKind.TypeSpecification:
                        return GetTypeFromSpecification(reader, (TypeSpecificationHandle)handle);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(handle));
                }
            }

            public virtual LocalVariable GetModifiedType(MetadataReader reader, bool isRequired, LocalVariable modifierType, LocalVariable unmodifiedType)
            {
                throw new NotSupportedException();
                //return unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";
            }

            public virtual LocalVariable GetFunctionPointerType(MethodSignature<LocalVariable> signature)
            {
                throw new NotSupportedException();
                //ImmutableArray<string> parameterTypes = signature.ParameterTypes;

                //int requiredParameterCount = signature.RequiredParameterCount;

                //var builder = new StringBuilder();
                //builder.Append("method ");
                //builder.Append(signature.ReturnType);
                //builder.Append(" *(");

                //int i;
                //for (i = 0; i < requiredParameterCount; i++)
                //{
                //    builder.Append(parameterTypes[i]);
                //    if (i < parameterTypes.Length - 1)
                //    {
                //        builder.Append(", ");
                //    }
                //}

                //if (i < parameterTypes.Length)
                //{
                //    builder.Append("..., ");
                //    for (; i < parameterTypes.Length; i++)
                //    {
                //        builder.Append(parameterTypes[i]);
                //        if (i < parameterTypes.Length - 1)
                //        {
                //            builder.Append(", ");
                //        }
                //    }
                //}

                //builder.Append(')');
                //return builder.ToString();
            }
        }

        public static byte[] GetILBytes(MethodInfo methodInfo)
        {
            var metadataToken = methodInfo.GetMetadataToken();

            using (var stream = File.OpenRead(methodInfo.DeclaringType.GetTypeInfo().Assembly.Location))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = MetadataTokens.MethodDefinitionHandle(metadataToken);
                var methodDefinition = metadataReader.GetMethodDefinition(methodHandle);
                var methodBody = peReader.GetMethodBody(methodDefinition.RelativeVirtualAddress);
                return methodBody.GetILBytes();
            }
        }

        private static Type EntityHandleToCatchType(MetadataReader reader, EntityHandle entityHandle)
        {
            StringHandle stringHandle;

            if (entityHandle.Kind == HandleKind.TypeDefinition)
            {
                stringHandle = reader.GetTypeDefinition((TypeDefinitionHandle) entityHandle).Name;
            }
            else if (entityHandle.Kind == HandleKind.TypeReference)
            {
                stringHandle = reader.GetTypeReference((TypeReferenceHandle)entityHandle).Name;
            }
            else if (entityHandle.Kind == HandleKind.TypeSpecification)
            {
                var decoded = reader.GetTypeSpecification((TypeSpecificationHandle)entityHandle).DecodeSignature(new DisassemblingTypeProvider());
                //Console.WriteLine(decoded);
                return Type.GetType(decoded);
            }
            else
            {
                return null;
            }

            return Type.GetType(reader.GetString(stringHandle));
        }

        public static IEnumerable<ExceptionClause> GetExceptionClauses(MethodInfo methodInfo)
        {
            var metadataToken = methodInfo.GetMetadataToken();

            using (var stream = File.OpenRead(methodInfo.DeclaringType.GetTypeInfo().Assembly.Location))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = MetadataTokens.MethodDefinitionHandle(metadataToken);
                var methodDefinition = metadataReader.GetMethodDefinition(methodHandle);

                var methodBody = peReader.GetMethodBody(methodDefinition.RelativeVirtualAddress);

                return methodBody.ExceptionRegions.Select(x => new ExceptionClause
                {
                    CatchType = EntityHandleToCatchType(metadataReader, x.CatchType),
                    FilterOffset = x.FilterOffset,
                    HandlerLength = x.HandlerLength,
                    HandlerOffset = x.HandlerOffset,
                    Kind = (ExceptionClauseKind)(ushort)x.Kind,
                    TryLength = x.TryLength,
                    TryOffset = x.TryOffset
                }).ToList();
            }
        }

        public static IEnumerable<LocalVariable> GetLocalVariables(MethodInfo methodInfo)
        {
            var metadataToken = methodInfo.GetMetadataToken();

            using (var stream = File.OpenRead(methodInfo.DeclaringType.GetTypeInfo().Assembly.Location))
            using (var peReader = new PEReader(stream))
            {
                var provider = new LocalVariableProvider();

                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = MetadataTokens.MethodDefinitionHandle(metadataToken);
                var methodDefinition = metadataReader.GetMethodDefinition(methodHandle);
                var methodBody = peReader.GetMethodBody(methodDefinition.RelativeVirtualAddress);

                if (methodBody.LocalVariablesInitialized)
                {
                    var standaloneSignature = metadataReader.GetStandaloneSignature(methodBody.LocalSignature);
                    var array = standaloneSignature.DecodeLocalSignature(provider);

                    return array.Select((x, ix) =>
                    {
                        x.LocalIndex = ix;
                        return x;
                    }).ToList();
                }
                else
                {
                    return Enumerable.Empty<LocalVariable>();
                }
            }
        }
#else
        public static byte[] GetILBytes(MethodInfo methodInfo)
        {
            return methodInfo.GetMethodBody().GetILAsByteArray();
        }

        private static Type GetCatchType(ExceptionHandlingClause clause)
        {
            try
            {
                return clause.CatchType;
            }
            catch
            {
                return null;
            }
        }

        private static int GetFilterOffset(ExceptionHandlingClause clause)
        {
            try
            {
                return clause.FilterOffset;
            }
            catch
            {
                return -1;
            }
        }

        public static IEnumerable<ExceptionClause> GetExceptionClauses(MethodInfo methodInfo)
        {
            return new LinqList<ExceptionHandlingClause>(methodInfo.GetMethodBody().ExceptionHandlingClauses).Select(x => new ExceptionClause()
            {
                CatchType = GetCatchType(x),
                FilterOffset = GetFilterOffset(x),
                HandlerLength = x.HandlerLength,
                HandlerOffset = x.HandlerOffset,
                Kind = (ExceptionClauseKind)(ushort)x.Flags,
                TryLength = x.TryLength,
                TryOffset = x.TryOffset
            }).ToList().AsEnumerable();
        }

        public static IEnumerable<LocalVariable> GetLocalVariables(MethodInfo methodInfo)
        {
            return new LinqList<LocalVariableInfo>(methodInfo.GetMethodBody().LocalVariables).Select(x => new LocalVariable
            {
                IsPinned = x.IsPinned,
                LocalIndex = x.LocalIndex,
                LocalType = x.LocalType
            }).ToList().AsEnumerable();
        }
#endif
    }
}
