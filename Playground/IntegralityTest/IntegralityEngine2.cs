using System;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Playground;
using ValueLink;

#pragma warning disable CS1998

namespace Tinyhand.Integrality;

public class TestIntegralityEngine : IntegralityEngine2<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegralityEngine> Pool = new(() => new());

    public override bool Validate(Message obj)
        => true;

    public override void Prume(Message.GoshujinClass goshujin)
    {
    }
}

public class IntegralityEngine2<TGoshujin, TObject>
    where TGoshujin : IGoshujin
    where TObject : ITinyhandSerialize<TObject>
{// Integrate/Differentiate
    public delegate Task<(IntegralityResult Result, ByteArrayPool.MemoryOwner Difference)> DifferentiateDelegate(ByteArrayPool.MemoryOwner integration);

    static public async Task<(IntegralityResult Result, ByteArrayPool.MemoryOwner Difference)> Differentiate(TGoshujin obj, ByteArrayPool.MemoryOwner integration)
    {
        return (IntegralityResult.Integrated, default);
    }

    public IntegralityEngine2()
    {
    }

    public async Task<IntegralityResult> Integrate(TGoshujin obj, DifferentiateDelegate differentiateDelegate)
    {
        return IntegralityResult.Integrated;
    }


    public virtual bool Validate(TObject obj)
        => true;

    public virtual void Prume(TGoshujin goshujin)
    {
    }
}
