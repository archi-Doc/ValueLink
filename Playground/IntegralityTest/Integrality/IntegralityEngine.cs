using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Playground;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable CS1998

namespace ValueLink.Integrality;

public class TestIntegralityEngine : IntegralityEngine<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegralityEngine> Pool = new(() => new());

    public override bool Validate(Message obj)
        => true;

    public override void Prune(Message.GoshujinClass goshujin)
    {
    }
}



public class IntegralityEngine<TGoshujin, TObject>
    where TGoshujin : IGoshujin, IIntegrality
    where TObject : ITinyhandSerialize<TObject>, IIntegrality
{// Integrate/Differentiate


    static public async Task<(IntegralityResult Result, BytePool.RentMemory Difference)> Differentiate(TGoshujin obj, BytePool.RentMemory integration)
    {
        return (IntegralityResult.Success, default);
    }

    public IntegralityEngine()
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
            var rentMemory = this.CreateProbePacket(obj);

            DifferentiateResult dif;
            try
            {
                dif = await differentiateDelegate(rentMemory, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                rentMemory.Return();
            }

            try
            {
                var result = this.ProcessProbeResponsePacket(obj, dif);
                if (result != IntegralityResult.Success)
                {
                    return result;
                }
            }
            finally
            {
                dif.Return();
            }

            this.state = IntegralityState.Get;
        }

        if (this.state == IntegralityState.Get)
        {
        }

        return IntegralityResult.Success;
    }

    public virtual bool Validate(TObject obj)
        => true;

    public virtual void Prune(TGoshujin goshujin)
    {
    }

    private BytePool.RentMemory CreateProbePacket(TGoshujin obj)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteUInt8((byte)IntegralityState.Probe);
            writer.WriteUInt64(obj.GetIntegralityHash());
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private IntegralityResult ProcessProbeResponsePacket(TGoshujin obj, DifferentiateResult dif)
    {
        if (dif.Result != IntegralityResult.Success)
        {
            return dif.Result;
        }

        var reader = new TinyhandReader(dif.RentMemory.Span);
        ulong hash;
        try
        {
            var state = (IntegralityState)reader.ReadUInt8();
            if (state != IntegralityState.Probe)
            {
                return IntegralityResult.InvalidData;
            }

            var hash = reader.ReadUInt64();
        }
        catch
        {
            return IntegralityResult.InvalidData;
        }

        return IntegralityResult.Success;
    }
}
