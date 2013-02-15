﻿using Sigil.Impl;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        /// <summary>
        /// Pops a reference to a rank 1 array off the stack, and pushes it's length onto the stack.
        /// </summary>
        public Emit<DelegateType> LoadLength()
        {
            var onStack = Stack.Top();

            if (onStack == null)
            {
                FailStackUnderflow(1);
            }

            var arr = onStack[0];

            if (arr.IsReference || arr.IsPointer || !arr.Type.IsArray || arr.Type.GetArrayRank() != 1)
            {
                throw new SigilVerificationException("LoadLength expects a rank 1 array, found " + arr, IL.Instructions(LocalsByIndex), Stack, 0);
            }

            UpdateState(OpCodes.Ldlen, TypeOnStack.Get<int>(), pop: 1);

            return this;
        }
    }
}
