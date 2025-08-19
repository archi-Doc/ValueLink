// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

/*[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableList
{
    [Link(Type = ChainType.LinkedList, Name = "Main")]
    public RepeatableList()
    {
    }
}

public class IsolationTestList
{
    [Fact]
    public void Test1()
    {// RepeatableRead
        var g = new RepeatableList.GoshujinClass();
        var c = g.Add(new RepeatableList());
        var c2 = g.Add(new RepeatableList());
        var c3 = g.Add(new RepeatableList());

        RepeatableList[] array;
        using (g.LockObject.EnterScope())
        {
            g.MainChain.Count.Is(3);
            array = g.MainChain.ToArray();
        }

        array.Contains(c).IsTrue();
        array.Contains(c2).IsTrue();
        array.Contains(c3).IsTrue();

        using (var w = c2!.TryLock())
        {
            if (w is not null)
            {
                w.Goshujin = null;
                w.Commit();
            }
        }

        using (g.LockObject.EnterScope())
        {
            g.MainChain.Count.Is(2);
            array = g.MainChain.ToArray();
        }

        array.Contains(c).IsTrue();
        array.Contains(c2).IsFalse();
        c2.State.Is(RepeatableReadObjectState.Obsolete);
        array.Contains(c3).IsTrue();
    }
}*/
