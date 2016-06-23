using System;
using System.Collections.Generic;
using System.Reflection;
#if COREFX
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
#endif

namespace Sigil.Impl
{
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

        public static IList<ExceptionClause> GetExceptionClauses(MethodInfo methodInfo)
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
                    CatchType = Type.GetType(metadataReader.GetString(metadataReader.GetTypeReference((TypeReferenceHandle)x.CatchType).Name)),
                    FilterOffset = x.FilterOffset,
                    HandlerLength = x.HandlerLength,
                    HandlerOffset = x.HandlerOffset,
                    Kind = (ExceptionClauseKind) (ushort) x.Kind,
                    TryLength = x.TryLength,
                    TryOffset = x.TryOffset
                }).ToList();
            }
        }

        public static IList<LocalVariable> GetLocalVariables(MethodInfo methodInfo)
        {
            using (var stream = File.OpenRead(methodInfo.DeclaringType.GetTypeInfo().Assembly.Location))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                return metadataReader.LocalVariables.Select(x =>
                {
                    var localVariable = metadataReader.GetLocalVariable(x);

                    return new LocalVariable
                    {
                        LocalIndex = localVariable.Index,
                        //LocalType = Type.GetType(metadataReader.GetString(localVariable.Name))
                        // TODO: Get over data
                    };
                }).ToList();
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
