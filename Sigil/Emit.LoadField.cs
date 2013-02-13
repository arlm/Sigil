﻿using Sigil.Impl;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        /// <summary>
        /// Loads a field onto the stack.
        /// 
        /// Instance fields expect a reference on the stack, which is popped.
        /// </summary>
        public Emit<DelegateType> LoadField(FieldInfo field, bool? isVolatile = null, int? unaligned = null)
        {
            if (field == null)
            {
                throw new ArgumentNullException("field");
            }

            if (unaligned.HasValue && (unaligned != 1 && unaligned != 2 && unaligned != 4))
            {
                throw new ArgumentException("unaligned must be null, 1, 2, or 4");
            }

            if (unaligned.HasValue && field.IsStatic)
            {
                throw new ArgumentException("unaligned cannot be used with static fields");
            }

            var useVolatile = isVolatile ?? field.IsVolatile();

            if (!field.IsStatic)
            {
                var onStack = Stack.Top();

                if (onStack == null)
                {
                    FailStackUnderflow(1);
                }
                var type = onStack[0];

                if (!field.DeclaringType.IsAssignableFrom(type))
                {
                    throw new SigilVerificationException("LoadField expected a type on the stack assignable to " + field.DeclaringType + ", found " + type, IL.Instructions(Locals), Stack, 0);
                }

                if (useVolatile)
                {
                    UpdateState(OpCodes.Volatile);
                }

                if (unaligned.HasValue)
                {
                    UpdateState(OpCodes.Unaligned, unaligned.Value);
                }

                UpdateState(OpCodes.Ldfld, field, TypeOnStack.Get(field.FieldType), pop: 1);
            }
            else
            {
                if (useVolatile)
                {
                    UpdateState(OpCodes.Volatile);
                }

                UpdateState(OpCodes.Ldsfld, field, TypeOnStack.Get(field.FieldType));
            }

            return this;
        }
    }
}
