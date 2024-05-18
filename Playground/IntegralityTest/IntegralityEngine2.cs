using Arc.Collections;
using Arc.Unit;
using Playground;
using ValueLink;

namespace Tinyhand.Integrality;

public class TestIntegralityEngine : IntegralityEngine2<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegralityEngine> Pool = new(() => new());
}

public class IntegralityEngine2<TGoshujin, TObject>
    where TGoshujin : IGoshujin
    where TObject : ITinyhandSerialize<TObject>
{// Integrate/Differentiate
    public IntegralityEngine2()
    {
    }

    public void Integrate(TGoshujin obj, ByteArrayPool.MemoryOwner difference, out ByteArrayPool.MemoryOwner integration)
    {
        integration = default;
    }

    public void Differentiate(TGoshujin obj, ByteArrayPool.MemoryOwner integration, out ByteArrayPool.MemoryOwner difference)
    {
        difference = default;
    }

    public virtual bool Validate(TObject obj)
        => true;

    public virtual void Prume(TGoshujin goshujin)
    {

    }
}
