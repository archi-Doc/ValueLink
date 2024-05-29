using System;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Tinyhand;
using ValueLink.Integrality;

namespace Playground;

public class TestIntegralityEngine : IntegralityEngine<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegralityEngine> Pool = new(() => new()
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
        var h = ((IIntegrality)t).GetIntegralityHash();
        h = ((IIntegrality)g).GetIntegralityHash();
        h = ((IIntegrality)g).GetIntegralityHash();

        var g2 = new Message.GoshujinClass();
        var engine = TestIntegralityEngine.Pool.Get();
        try
        {
            var result = await engine.Integrate(g, (x, y) => Task.FromResult(((IIntegrality)g).Differentiate(x)));
        }
        finally
        {
            TestIntegralityEngine.Pool.Return(engine);
        }
    }
}
