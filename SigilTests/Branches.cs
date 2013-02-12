﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SigilTests
{
    [TestClass, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Branches
    {
        [TestMethod]
        public void BranchingOverExceptions()
        {
            {
                var hasNormalBranch = new Regex("^br ", RegexOptions.Multiline);

                for (var i = 0; i < 127 - 10; i++)
                {
                    var e1 = Emit<Action>.NewDynamicMethod("E1");
                    var end = e1.DefineLabel("end");

                    e1.Branch(end);

                    var t = e1.BeginExceptionBlock();
                    var c = e1.BeginCatchBlock<Exception>(t);
                    e1.Pop();
                    e1.EndCatchBlock(c);
                    e1.EndExceptionBlock(t);

                    for (var j = 0; j < i; j++)
                    {
                        e1.Nop();
                    }

                    e1.MarkLabel(end);
                    e1.Return();

                    string instrs;
                    var d1 = e1.CreateDelegate(out instrs);

                    d1();

                    var shouldFail = hasNormalBranch.IsMatch(instrs);
                    if (shouldFail)
                    {
                        Assert.Fail();
                    }
                }
            }

            {
                var hasNormalBranch = new Regex("^br ", RegexOptions.Multiline);

                for (var i = 0; i < 127 - 16; i++)
                {
                    var e1 = Emit<Action>.NewDynamicMethod("E1");
                    var end = e1.DefineLabel("end");

                    e1.Branch(end);

                    var t = e1.BeginExceptionBlock();
                    var c = e1.BeginCatchBlock<Exception>(t);
                    e1.Pop();
                    e1.EndCatchBlock(c);
                    var f = e1.BeginFinallyBlock(t);
                    e1.EndFinallyBlock(f);
                    e1.EndExceptionBlock(t);

                    for (var j = 0; j < i; j++)
                    {
                        e1.Nop();
                    }

                    e1.MarkLabel(end);
                    e1.Return();

                    string instrs;
                    var d1 = e1.CreateDelegate(out instrs);

                    d1();

                    var shouldFail = hasNormalBranch.IsMatch(instrs);
                    if (shouldFail)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        [TestMethod]
        public void ShortForm()
        {
            {
                var hasNormalBranch = new Regex("^br ", RegexOptions.Multiline);

                for (var i = 0; i < 127; i++)
                {
                    var e1 = Emit<Action>.NewDynamicMethod("E1");
                    var end = e1.DefineLabel("end");

                    e1.Branch(end);

                    for (var j = 0; j < i; j++)
                    {
                        e1.Nop();
                    }

                    e1.MarkLabel(end);
                    e1.Return();

                    string instrs;
                    var d1 = e1.CreateDelegate(out instrs);

                    d1();

                    var shouldFail = hasNormalBranch.IsMatch(instrs);
                    if (shouldFail)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        [TestMethod]
        public void BinaryInput()
        {
            var emit = typeof(Emit<Action>);
            var branches =
                new[]
                {
                    emit.GetMethod("BranchIfEqual"),
                    emit.GetMethod("BranchIfGreater"),
                    emit.GetMethod("BranchIfGreaterOrEqual"),
                    emit.GetMethod("BranchIfLess"),
                    emit.GetMethod("BranchIfLessOrEqual"),
                    emit.GetMethod("UnsignedBranchIfNotEqual"),
                    emit.GetMethod("UnsignedBranchIfGreater"),
                    emit.GetMethod("UnsignedBranchIfGreaterOrEqual"),
                    emit.GetMethod("UnsignedBranchIfLess"),
                    emit.GetMethod("UnsignedBranchIfLessOrEqual")
                };

            foreach (var branch in branches)
            {
                var e1 = Emit<Action>.NewDynamicMethod("E1");
                var l = e1.DefineLabel();
                e1.LoadConstant(0);
                e1.LoadConstant(1);
                branch.Invoke(e1, new object[] { l });
                e1.MarkLabel(l);
                e1.Return();

                var d1 = e1.CreateDelegate();
                d1();
            }
        }

        [TestMethod]
        public void UnaryInput()
        {
            {
                var e1 = Emit<Action>.NewDynamicMethod("E1");
                var l = e1.DefineLabel();
                e1.LoadConstant(0);
                e1.BranchIfFalse(l);
                e1.MarkLabel(l);
                e1.Return();

                var d1 = e1.CreateDelegate();

                d1();
            }

            {
                var e2 = Emit<Action>.NewDynamicMethod("E1");
                var l = e2.DefineLabel();
                e2.LoadConstant(0);
                e2.BranchIfTrue(l);
                e2.MarkLabel(l);
                e2.Return();

                var d2 = e2.CreateDelegate();

                d2();
            }
        }

        [TestMethod]
        public void MultiLabel()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");

            e1.LoadConstant(1);
            var one = e1.DefineLabel("one");
            e1.Branch(one);
            
            e1.LoadConstant(2);
            e1.Add();

            e1.MarkLabel(one);
            var two = e1.DefineLabel("two");
            e1.Branch(two);

            e1.LoadConstant(3);
            e1.Add();

            e1.MarkLabel(two);
            e1.Return();

            var del = e1.CreateDelegate();

            Assert.AreEqual(1, del());
        }

        [TestMethod]
        public void BrS()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");

            var after = e1.DefineLabel("after");
            e1.LoadConstant(456);
            e1.Branch(after);
            
            e1.LoadConstant(111);
            e1.Add();

            e1.MarkLabel(after);
            e1.Return();

            var del = e1.CreateDelegate();

            Assert.AreEqual(456, del());
        }

        [TestMethod]
        public void Br()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");

            var after = e1.DefineLabel("after");
            e1.LoadConstant(111);
            e1.Branch(after);

            for (var i = 0; i < 1000; i++)
            {
                e1.Nop();
            }

            e1.MarkLabel(after);
            e1.Return();

            var del = e1.CreateDelegate();

            Assert.AreEqual(111, del());
        }

        [TestMethod]
        public void BeqS()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");

            var after = e1.DefineLabel("after");
            e1.LoadConstant(314);
            e1.LoadConstant(456);
            e1.LoadConstant(456);
            e1.BranchIfEqual(after);

            e1.LoadConstant(111);
            e1.Add();

            e1.MarkLabel(after);
            e1.Return();

            var del = e1.CreateDelegate();

            Assert.AreEqual(314, del());
        }

        [TestMethod]
        public void Beq()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");

            var after = e1.DefineLabel("after");
            e1.LoadConstant(314);
            e1.LoadConstant(456);
            e1.LoadConstant(456);
            e1.BranchIfEqual(after);

            for (var i = 0; i < 1234; i++)
            {
                e1.Nop();
            }

            e1.LoadConstant(111);
            e1.Add();

            e1.MarkLabel(after);
            e1.Return();

            var del = e1.CreateDelegate();

            Assert.AreEqual(314, del());
        }
    }
}
