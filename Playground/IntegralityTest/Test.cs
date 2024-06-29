using System;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Netsphere;
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

    public override bool Validate(Message.GoshujinClass goshujin, Message newItem, Message? oldItem)
        => true;

    public override void Prune(Message.GoshujinClass goshujin)
    {
    }
}


public class Test
{
    public async void Test1()
    {
        var g = new Message.GoshujinClass();
        g.Add(new(1, 1, "A", "aaa", 1));
        g.Add(new(2, 2, "B", "bbb", 2));

        var t = new Message(1, 1, "A", "aaa", 1);
        var h = ((IIntegralityObject)t).GetIntegralityHash();
        h = ((IIntegralityObject)g).GetIntegralityHash();
        h = ((IIntegralityObject)g).GetIntegralityHash();

        var g2 = new Message.GoshujinClass();
        var engine = TestIntegrality.Pool.Get();
        try
        {
            var result = await engine.Integrate(g2, (x, y) => Task.FromResult(((IIntegralityObject)g).Differentiate(engine, x)));
            result = await engine.Integrate(g2, (x, y) => Task.FromResult(((IIntegralityObject)g).Differentiate(engine, x)));
        }
        finally
        {
            TestIntegrality.Pool.Return(engine);
        }
    }

    /*public async void Test2()
    {
        var g = new ActiveNode.GoshujinClass();

        var g2 = new ActiveNode.GoshujinClass();
        var engine = ActiveNode.Integrality.Instance;
        var result = await engine.Integrate(g2, (x, y) => Task.FromResult(((IIntegralityObject)g).Differentiate(engine, x)));
        result = await engine.Integrate(g2, (x, y) => Task.FromResult(((IIntegralityObject)g).Differentiate(engine, x)));
    }*/
}
