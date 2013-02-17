﻿using Sigil.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        private IEnumerable<StackTransition> CheckConvertible(string method, TypeOnStack item, Type toType)
        {
            if (item != TypeOnStack.Get<int>() && item != TypeOnStack.Get<NativeIntType>() &&
                item != TypeOnStack.Get<long>() && item != TypeOnStack.Get<float>() &&
                item != TypeOnStack.Get<double>() && !item.IsPointer
               )
            {
                throw new SigilVerificationException(method + " expected an int, native int, long, float, double, or pointer on the stack; found " + item, IL.Instructions(LocalsByIndex));
            }

            return
                new[]
                {
                    new StackTransition(new [] { typeof(int) }, new [] { toType }),
                    new StackTransition(new [] { typeof(NativeIntType) }, new [] { toType }),
                    new StackTransition(new [] { typeof(long) }, new [] { toType }),
                    new StackTransition(new [] { typeof(float) }, new [] { toType }),
                    new StackTransition(new [] { typeof(double) }, new [] { toType }),
                };
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character primitive type.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> Convert<PrimitiveType>()
            where PrimitiveType : struct
        {
            return Convert(typeof(PrimitiveType));
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character primitive type.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> Convert(Type primitiveType)
        {
            if (primitiveType == null)
            {
                throw new ArgumentNullException("primitiveType");
            }

            if (!primitiveType.IsPrimitive || primitiveType == typeof(char))
            {
                throw new ArgumentException("Convert expects a non-character primitive type");
            }

            var top = Stack.Top();
            if (top == null)
            {
                FailStackUnderflow(1);
            }

            var transitions = CheckConvertible("Convert", top.Single(), primitiveType);

            if (primitiveType == typeof(byte))
            {
                ConvertToByte(transitions);
                return this;
            }

            if (primitiveType == typeof(sbyte))
            {
                ConvertToSByte(transitions);
                return this;
            }

            if (primitiveType == typeof(short))
            {
                ConvertToInt16(transitions);
                return this;
            }

            if (primitiveType == typeof(ushort))
            {
                ConvertToUInt16(transitions);
                return this;
            }

            if (primitiveType == typeof(int))
            {
                ConvertToInt32(transitions);
                return this;
            }

            if (primitiveType == typeof(uint))
            {
                ConvertToUInt32(transitions);
                return this;
            }

            if (primitiveType == typeof(long))
            {
                ConvertToInt64(transitions);
                return this;
            }

            if (primitiveType == typeof(ulong))
            {
                ConvertToUInt64(transitions);
                return this;
            }

            if (primitiveType == typeof(IntPtr))
            {
                ConvertToNativeInt(transitions);
                return this;
            }

            if (primitiveType == typeof(UIntPtr))
            {
                ConvertToUnsignedNativeInt(transitions);
                return this;
            }

            if (primitiveType == typeof(float))
            {
                ConvertToFloat(transitions);
                return this;
            }

            if (primitiveType == typeof(double))
            {
                ConvertToDouble(transitions);
                return this;
            }

            throw new Exception("Shouldn't be possible");
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character, non-float, non-double primitive type.
        /// If the conversion would overflow at runtime, an OverflowException is thrown.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> ConvertOverflow<PrimitiveType>()
        {
            return ConvertOverflow(typeof(PrimitiveType));
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character, non-float, non-double primitive type.
        /// If the conversion would overflow at runtime, an OverflowException is thrown.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> ConvertOverflow(Type primitiveType)
        {
            if (primitiveType == null)
            {
                throw new ArgumentNullException("primitiveType");
            }

            if (!primitiveType.IsPrimitive || primitiveType == typeof(char))
            {
                throw new ArgumentException("ConvertOverflow expects a non-character primitive type");
            }

            if (primitiveType == typeof(float))
            {
                throw new InvalidOperationException("There is no operation for converting to a float with overflow checking");
            }

            if (primitiveType == typeof(double))
            {
                throw new InvalidOperationException("There is no operation for converting to a double with overflow checking");
            }

            var top = Stack.Top();
            if (top == null)
            {
                FailStackUnderflow(1);
            }

            var transitions = CheckConvertible("ConvertOverflow", top.Single(), primitiveType);

            if (primitiveType == typeof(byte))
            {
                ConvertToByteOverflow(transitions);
                return this;
            }

            if (primitiveType == typeof(sbyte))
            {
                ConvertToSByteOverflow(transitions);
                return this;
            }

            if (primitiveType == typeof(short))
            {
                ConvertToInt16Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(ushort))
            {
                ConvertToUInt16Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(int))
            {
                ConvertToInt32Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(uint))
            {
                ConvertToUInt32Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(long))
            {
                ConvertToInt64Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(ulong))
            {
                ConvertToUInt64Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(IntPtr))
            {
                ConvertToNativeIntOverflow(transitions);
                return this;
            }

            if (primitiveType == typeof(UIntPtr))
            {
                ConvertToUnsignedNativeIntOverflow(transitions);
                return this;
            }

            throw new Exception("Shouldn't be possible");
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character, non-float, non-double primitive type as if it were unsigned.
        /// If the conversion would overflow at runtime, an OverflowException is thrown.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> UnsignedConvertOverflow<PrimitiveType>()
        {
            return UnsignedConvertOverflow(typeof(PrimitiveType));
        }

        /// <summary>
        /// Convert a value on the stack to the given non-character, non-float, non-double primitive type as if it were unsigned.
        /// If the conversion would overflow at runtime, an OverflowException is thrown.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr). 
        /// </summary>
        public Emit<DelegateType> UnsignedConvertOverflow(Type primitiveType)
        {
            if (primitiveType == null)
            {
                throw new ArgumentNullException("primitiveType");
            }

            if (!primitiveType.IsPrimitive || primitiveType == typeof(char))
            {
                throw new ArgumentException("UnsignedConvertOverflow expects a non-character primitive type");
            }

            if (primitiveType == typeof(float))
            {
                throw new InvalidOperationException("There is no operation for converting to a float with overflow checking");
            }

            if (primitiveType == typeof(double))
            {
                throw new InvalidOperationException("There is no operation for converting to a double with overflow checking");
            }

            if (primitiveType == typeof(UIntPtr) || primitiveType == typeof(IntPtr))
            {
                throw new InvalidOperationException("There is no operation for converting to a pointer with overflow checking");
            }

            var top = Stack.Top();
            if (top == null)
            {
                FailStackUnderflow(1);
            }

            var transitions = CheckConvertible("UnsignedConvertOverflow", top.Single(), primitiveType);

            if (primitiveType == typeof(byte))
            {
                UnsignedConvertToByteOverflow(transitions);
                return this;
            }

            if (primitiveType == typeof(sbyte))
            {
                UnsignedConvertToSByteOverflow(transitions);
                return this;
            }

            if (primitiveType == typeof(short))
            {
                UnsignedConvertToInt16Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(ushort))
            {
                UnsignedConvertToUInt16Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(int))
            {
                UnsignedConvertToInt32Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(uint))
            {
                UnsignedConvertToUInt32Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(long))
            {
                UnsignedConvertToInt64Overflow(transitions);
                return this;
            }

            if (primitiveType == typeof(ulong))
            {
                UnsignedConvertToUInt64Overflow(transitions);
                return this;
            }

            throw new Exception("Shouldn't be possible");
        }

        /// <summary>
        /// Converts a primitive type on the stack to a float, as if it were unsigned.
        /// 
        /// Primitives are int8, uint8, int16, uint16, int32, uint32, int64, uint64, float, double, native int (IntPtr), and unsigned native int (UIntPtr).
        /// </summary>
        public Emit<DelegateType> UnsignedConvertToFloat()
        {
            var top = Stack.Top();

            if (top == null)
            {
                FailStackUnderflow(1);
            }

            var transitions = CheckConvertible("UnsignedConvertToFloat", top.Single(), typeof(float));

            UpdateState(OpCodes.Conv_R_Un, transitions, TypeOnStack.Get<float>(), pop: 1);

            return this;
        }

        private void ConvertToNativeInt(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_I, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void ConvertToNativeIntOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void UnsignedConvertToNativeIntOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I_Un, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void ConvertToUnsignedNativeInt(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_U, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void ConvertToUnsignedNativeIntOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void UnsignedConvertToUnsignedNativeIntOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U_Un, transitions, TypeOnStack.Get<NativeIntType>(), pop: 1);
        }

        private void ConvertToSByte(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_I1, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToSByteOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I1, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToSByteOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I1_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToInt16(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_I2, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToInt16Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I2, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToInt16Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I2_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToInt32(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_I4, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToInt32Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I4, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToInt32Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I4_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToInt64(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_I8, transitions, TypeOnStack.Get<long>(), pop: 1);
        }

        private void ConvertToInt64Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I8, transitions, TypeOnStack.Get<long>(), pop: 1);
        }

        private void UnsignedConvertToInt64Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_I8_Un, transitions, TypeOnStack.Get<long>(), pop: 1);
        }

        private void ConvertToFloat(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_R4, transitions, TypeOnStack.Get<float>(), pop: 1);
        }

        private void ConvertToDouble(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_R8, transitions, TypeOnStack.Get<double>(), pop: 1);
        }

        private void ConvertToByte(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_U1, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToByteOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U1, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToByteOverflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U1_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToUInt16(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_U2, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToUInt16Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U2, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToUInt16Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U2_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToUInt32(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_U4, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToUInt32Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U4, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void UnsignedConvertToUInt32Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U4_Un, transitions, TypeOnStack.Get<int>(), pop: 1);
        }

        private void ConvertToUInt64(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_U8, transitions, TypeOnStack.Get<long>(), pop: 1);
        }

        private void ConvertToUInt64Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U8, transitions, TypeOnStack.Get<long>(), pop: 1);
        }

        private void UnsignedConvertToUInt64Overflow(IEnumerable<StackTransition> transitions)
        {
            UpdateState(OpCodes.Conv_Ovf_U8_Un, transitions, TypeOnStack.Get<long>(), pop: 1);
        }
    }
}
