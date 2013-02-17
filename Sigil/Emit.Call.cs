﻿using Sigil.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        private void InjectTailCall()
        {
            if (InstructionStream.Count < 2) return;

            var last = InstructionStream[InstructionStream.Count - 1];
            var nextToLast = InstructionStream[InstructionStream.Count - 2];

            if (last.Item1 != OpCodes.Ret) return;

            if (!new[] { OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt }.Contains(nextToLast.Item1)) return;

            InsertInstruction(IL.Index - 2, OpCodes.Tailcall);
        }

        /// <summary>
        /// Calls the given method.  Pops its arguments in reverse order (left-most deepest in the stack), and pushes the return value if it is non-void.
        /// 
        /// If the given method is an instance method, the `this` reference should appear before any parameters.
        /// 
        /// Call does not respect overrides, the implementation defined by the given MethodInfo is what will be called at runtime.
        /// 
        /// To call overrides of instance methods, use CallVirtual.
        /// </summary>
        public Emit<DelegateType> Call(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            var expectedParams = method.GetParameters().Select(s => TypeOnStack.Get(s.ParameterType)).ToList();

            // Instance methods expect this to preceed parameters
            if (HasFlag(method.CallingConvention, CallingConventions.HasThis))
            {
                expectedParams.Insert(0, TypeOnStack.Get(method.DeclaringType));
            }

            var resultType = method.ReturnType == typeof(void) ? null : TypeOnStack.Get(method.ReturnType);

            var firstParamIsThis =
                HasFlag(method.CallingConvention, CallingConventions.HasThis) ||
                HasFlag(method.CallingConvention, CallingConventions.ExplicitThis);

            IEnumerable<StackTransition> transitions;
            if (resultType != null)
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.AsEnumerable().Reverse(), new [] { resultType })
                    };
            }
            else
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.AsEnumerable().Reverse(), new TypeOnStack[0])
                    };
            }

            UpdateState(OpCodes.Call, method, transitions.Wrap("Call"), resultType, pop: expectedParams.Count, firstParamIsThis: firstParamIsThis);

            return this;
        }
    }
}
