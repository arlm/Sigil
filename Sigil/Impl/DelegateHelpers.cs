using System;
using System.Reflection;

namespace Sigil.Impl
{
    /// <summary>
    /// Contains helper methods to shim over the difference between different Type APIs in
    /// different frameworks
    /// </summary>
    internal static class DelegateHelpers
    {
#if COREFX
        public static MethodInfo GetMethodInfo(Delegate @delegate)
        {
            return @delegate.GetMethodInfo();
        }
#else
        public static MethodInfo GetMethodInfo(Delegate @delegate)
        {
            return @delegate.Method;
        }
#endif
    }
}