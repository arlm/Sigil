﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sigil.NonGeneric;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SigilTests 
{
    public partial class TypeInitializer 
    {
        [TestMethod]
        public void SimpleNonGeneric() 
        {
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Foo"), AssemblyBuilderAccess.Run);
            var mod = asm.DefineDynamicModule("Bar");
            var t = mod.DefineType("T");

            var foo = t.DefineField("Foo", typeof(int), FieldAttributes.Public | FieldAttributes.Static);

            var c = Emit.BuildTypeInitializer(t);
            c.LoadConstant(123);
            c.StoreField(foo);
            c.Return();

            c.CreateTypeInitializer();

            var type = t.CreateType();

            var fooGet = type.GetField("Foo");

            Assert.AreEqual(123, (int)fooGet.GetValue(null));
        }
    }
}
