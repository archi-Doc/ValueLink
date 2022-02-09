// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest
{
    [ValueLinkObject]
    [TinyhandObject]
    public partial class TestClass3
    {
        [Link(Primary = true, Type = ChainType.Ordered, NoValue = true, Accessibility = ValueLinkAccessibility.Public)]
        [KeyAsName]
        private int Id;

        public TestClass3()
        {
        }

        public TestClass3(int id, string name, byte age)
        {
            this.Id = id;
        }
    }

    public class BasicTest2
    {
        [Fact]
        public void Test1()
        {
            var tc = new TestClass3();
        }
    }
}
