﻿using NUnit.Framework;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigilTests
{
    [TestFixture, System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class LoadObject
    {
        [Test]
        public void Simple()
        {
            var e1 = Emit<Func<DateTime, DateTime>>.NewDynamicMethod();
            e1.LoadArgumentAddress(0);
            e1.LoadObject<DateTime>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            var now = DateTime.UtcNow;

            Assert.AreEqual(now, d1(now));
        }
    }
}
