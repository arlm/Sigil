﻿using Sigil.Impl;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        /// <summary>
        /// Removes the top value on the stack.
        /// </summary>
        public Emit<DelegateType> Pop()
        {
            UpdateState(OpCodes.Pop, StackTransition.Pop<WildcardType>().Wrap("Pop"));

            return this;
        }
    }
}
