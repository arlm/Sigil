﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sigil.NonGeneric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SigilTests
{
    public partial class LoadElementAddress
    {
        [TestMethod]
        public void SimpleNonGeneric()
        {
            var e1 = Emit.NewDynamicMethod(typeof(int), new [] { typeof(int[]), typeof(int) }, "E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.LoadElementAddress<int>();
            e1.LoadIndirect<int>();
            e1.Return();

            string instrs;
            var d1 = e1.CreateDelegate<Func<int[], int, int>>(out instrs);

            Assert.AreEqual(2, d1(new[] { 1, 2, 3 }, 1));
            Assert.IsTrue(instrs.Contains("readonly."));
        }

        [TestMethod]
        public void NotReadonlyNonGeneric()
        {
            var e1 = Emit.NewDynamicMethod(typeof(void), new[] { typeof(int[]), typeof(int) }, "E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.LoadElementAddress<int>();
            e1.Duplicate();
            e1.LoadIndirect<int>();
            e1.LoadConstant(1);
            e1.Add();
            e1.StoreIndirect<int>();
            e1.Return();

            string instrs;
            var d1 = e1.CreateDelegate<Action<int[], int>>(out instrs);

            var x = new[] { 1, 2, 3 };
            d1(x, 1);

            Assert.AreEqual(1, x[0]);
            Assert.AreEqual(3, x[1]);
            Assert.AreEqual(3, x[2]);

            Assert.IsFalse(instrs.Contains("readonly."));
        }

        [TestMethod]
        public void ReadOnlyCallNonGeneric()
        {
            var toString = typeof(object).GetMethod("ToString");

            var e1 = Emit.NewDynamicMethod(typeof(string), new [] { typeof(int), typeof(object[]) }, "E1");
            e1.LoadArgument(1);
            e1.LoadArgument(0);
            e1.LoadElementAddress<object>();
            e1.LoadIndirect<object>();
            e1.CallVirtual(toString);
            e1.Return();

            string instrs;
            var d1 = e1.CreateDelegate<Func<int, object[], string>>(out instrs);

            Assert.AreEqual("123", d1(0, new object[] { 123 }));
            Assert.IsTrue(instrs.Contains("readonly."));
        }

        [TestMethod]
        public void NotReadOnlyCallNonGeneric()
        {
            var toString = typeof(object).GetMethod("ToString");

            var e1 = Emit.NewDynamicMethod(typeof(string), new [] { typeof(int), typeof(object[]) }, "E1");
            e1.LoadArgument(1);
            e1.LoadArgument(0);
            e1.LoadElementAddress<object>();
            e1.Duplicate();
            e1.Pop();
            e1.LoadIndirect<object>();
            e1.CallVirtual(toString);
            e1.Return();

            string instrs;
            var d1 = e1.CreateDelegate<Func<int, object[], string>>(out instrs);

            Assert.AreEqual("123", d1(0, new object[] { 123 }));
            Assert.IsFalse(instrs.Contains("readonly."));
        }
    }
}
