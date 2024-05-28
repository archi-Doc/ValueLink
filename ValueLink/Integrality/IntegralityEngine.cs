// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable CS1998
#pragma warning disable SA1124

namespace ValueLink.Integrality;

public class IntegralityEngine<TGoshujin, TObject>
    where TGoshujin : IGoshujin, IIntegrality
    where TObject : ITinyhandSerialize<TObject>, IIntegrality
{// Integrate/Differentiate
    public static async Task<IntegralityResultMemory> Differentiate(TGoshujin obj, BytePool.RentMemory integration)
    {
        return default;
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

            IntegralityResultMemory resultMemory;
            try
            {
                resultMemory = await differentiateDelegate(rentMemory, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                rentMemory.Return();
            }

            try
            {
                var result = this.ProcessProbeResponsePacket(obj, resultMemory);
                if (result != IntegralityResult.Continue)
                {
                    return result;
                }
            }
            finally
            {
                resultMemory.Return();
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

    private IntegralityResult ProcessProbeResponsePacket(TGoshujin obj, IntegralityResultMemory resultMemory)
    {
        if (resultMemory.Result != IntegralityResult.Success)
        {
            return resultMemory.Result;
        }

        var reader = new TinyhandReader(resultMemory.RentMemory.Span);
        try
        {
            var state = (IntegralityState)reader.ReadUInt8();
            if (state != IntegralityState.Probe)
            {
                return IntegralityResult.InvalidData;
            }

            this.targetHash = reader.ReadUInt64();
        }
        catch
        {
            return IntegralityResult.InvalidData;
        }

        if (obj.GetIntegralityHash() == this.targetHash)
        {// Identical
            return IntegralityResult.Success;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteUInt8((byte)IntegralityState.Get);
            obj.ProcessProbeResponse(ref reader, ref writer);
            var getPacket = writer.FlushAndGetRentMemory();
        }
        catch
        {
            return IntegralityResult.InvalidData;
        }
        finally
        {
            writer.Dispose();
        }

        return IntegralityResult.Continue;
    }
}
