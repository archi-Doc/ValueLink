using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Playground;
using Tinyhand.IO;
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
    where TGoshujin : IGoshujin, IIntegrality
    where TObject : ITinyhandSerialize<TObject>, IIntegrality
{// Integrate/Differentiate
    public delegate Task<(IntegralityResult Result, ByteArrayPool.MemoryOwner Difference)> DifferentiateDelegate(ByteArrayPool.MemoryOwner integration, CancellationToken cancellationToken);

    static public async Task<(IntegralityResult Result, ByteArrayPool.MemoryOwner Difference)> Differentiate(TGoshujin obj, ByteArrayPool.MemoryOwner integration)
    {
        return (IntegralityResult.Integrated, default);
    }

    public IntegralityEngine2()
    {
    }

    #region FieldAndProperty

    private IntegralityState state = IntegralityState.Probe;
    private ulong targetHash;

    #endregion

    public async Task<IntegralityResult> Integrate(TGoshujin obj, DifferentiateDelegate differentiateDelegate, CancellationToken cancellationToken = default)
    {
        if (this.state == IntegralityState.Probe)
        {// Probe
            var primaryHash = obj.GetIntegralityHash();
            var owner = ByteArrayPool.Default.Rent(sizeof(ulong));
            var memoryOwner = owner.ToMemoryOwner(0, sizeof(ulong));
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(owner.ByteArray.AsSpan()), primaryHash);
            var r = await differentiateDelegate(memoryOwner, cancellationToken).ConfigureAwait(false);
            if (r.Result != IntegralityResult.Success)
            {
                return r.Result;
            }

            var memory = r.Difference.Memory;
            if (memory.Length < sizeof(ulong))
            {
                return IntegralityResult.InvalidData;
            }

            this.targetHash = BitConverter.ToUInt64(memory.Span);
            if (primaryHash == this.targetHash)
            {
                return IntegralityResult.Success;
            }

            this.state = IntegralityState.Request;
        }

        if (this.state == IntegralityState.Request)
        {
        }

        void Internal()
        {
            var writer = new TinyhandWriter(owner.ByteArray);
            writer.WriteUInt8((byte)state);

        }


        return IntegralityResult.Integrated;
    }

    public virtual bool Validate(TObject obj)
        => true;

    public virtual void Prume(TGoshujin goshujin)
    {
    }
}
