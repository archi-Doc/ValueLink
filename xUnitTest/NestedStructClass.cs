// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ValueLink;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public partial class NestedStructClass<T, U>
    where T : struct
    where U : class
{
    [TinyhandObject]
    [ValueLinkObject]
    private sealed partial class Item
    {
        [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
        public Item(int key, T value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Item()
        {
        }

        [Key(0)]
        internal T Value = default!;

        [Key(1)]
        [Link(Type = ChainType.Unordered)]
        internal int Key;
    }

    public NestedStructClass()
    {
    }

    private Item.GoshujinClass goshujin = new();
}
