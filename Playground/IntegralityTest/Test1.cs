using Tinyhand;
using Tinyhand.Integrality;

namespace Playground;

public class Test1
{
    public void Test()
    {
        var g = new Message.GoshujinClass();
        g.Add(new(1, 1, "A", "aaa", 1));
        g.Add(new(2, 2, "B", "bbb", 2));

        var t = new Message(1, 1, "A", "aaa", 1);
        var h = ((IIntegrality)t).GetIntegralityHash();
        h = ((IIntegrality)g).GetIntegralityHash();
        h = ((IIntegrality)g).GetIntegralityHash();

        // var engine = new IntegralityEngine2<Message.GoshujinClass>();

        var engine = TestIntegralityEngine.Pool.Get();
        try
        {
        }
        finally
        {
            TestIntegralityEngine.Pool.Return(engine);
        }

        /*var identity = engine.GetIdentity(g);
        var difference = engine.GetDifference();
        engine.Integrate(difference);*/
    }
}
