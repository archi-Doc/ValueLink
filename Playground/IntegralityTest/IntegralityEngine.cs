﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Playground;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable CS1998

namespace Tinyhand.Integrality;

public class TestIntegralityEngine : IntegralityEngine<Message.GoshujinClass, Message>
{
    public static readonly ObjectPool<TestIntegralityEngine> Pool = new(() => new());

    public override bool Validate(Message obj)
        => true;

    public override void Prune(Message.GoshujinClass goshujin)
    {
    }
}

/*[TinyhandObject]
internal partial class ProbePacket
{
    [Key(0)]
    public IntegralityState State { get; set; } = IntegralityState.Probe;

    [Key(1)]
    public ulong TargetHash { get; set; }
}*/

public readonly struct DifferentiateResult
{
    public DifferentiateResult(IntegralityResult result, BytePool.RentMemory difference)
    {
        this.Result = result;
        this.RentMemory = difference;
    }

    public readonly IntegralityResult Result;

    public readonly BytePool.RentMemory RentMemory;

    public void Return()
        => this.RentMemory.Return();
}

public class IntegralityEngine<TGoshujin, TObject>
    where TGoshujin : IGoshujin, IIntegrality
    where TObject : ITinyhandSerialize<TObject>, IIntegrality
{// Integrate/Differentiate
    public delegate Task<DifferentiateResult> DifferentiateDelegate(BytePool.RentMemory integration, CancellationToken cancellationToken);

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

            this.state = IntegralityState.Request;
        }

        if (this.state == IntegralityState.Request)
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
