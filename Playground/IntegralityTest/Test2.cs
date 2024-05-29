﻿using System;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Tinyhand;
using ValueLink.Integrality;

namespace Playground;

public class TestIntegrality : Integrality<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegrality> Pool = new(() => new()
    {
        MaxItems = 1000,
        RemoveIfItemNotFound = true,
    });

    public override bool Validate(Message obj)
        => true;

    public override void Prune(Message.GoshujinClass goshujin)
    {
    }
}


public class Test1
{
    public async void Test()
    {
        var g = new Message.GoshujinClass();
        g.Add(new(1, 1, "A", "aaa", 1));
        g.Add(new(2, 2, "B", "bbb", 2));

        var t = new Message(1, 1, "A", "aaa", 1);
        var h = ((IExaltationOfIntegrality)t).GetIntegralityHash();
        h = ((IExaltationOfIntegrality)g).GetIntegralityHash();
        h = ((IExaltationOfIntegrality)g).GetIntegralityHash();

        var g2 = new Message.GoshujinClass();
        var engine = TestIntegrality.Pool.Get();
        try
        {
            var result = await engine.Integrate(g, (x, y) => Task.FromResult(((IExaltationOfIntegrality)g).Differentiate(engine, x)));
            result = await engine.Integrate(g, (x, y) => Task.FromResult(((IExaltationOfIntegrality)g).Differentiate(engine, x)));
        }
        finally
        {
            TestIntegrality.Pool.Return(engine);
        }
    }
}
