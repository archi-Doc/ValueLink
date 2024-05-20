using System;
using System.Threading.Tasks;
using Arc.Unit;
using Tinyhand;
using Tinyhand.Integrality;

namespace Playground;

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
            var result = await engine.Integrate(g, (x, y) => TestIntegralityEngine.Differentiate(g2, x));
        }
        finally
        {
            TestIntegralityEngine.Pool.Return(engine);
        }
    }
}
