// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable CS1998
#pragma warning disable SA1124

namespace ValueLink.Integrality;

public abstract class IntegralityEngine
{
    #region FieldAndProperty

    public ulong TargetHash { get; protected set; }

    #endregion
}

public class IntegralityEngine<TGoshujin, TObject> : IntegralityEngine
    where TGoshujin : IGoshujin, IIntegrality
    where TObject : ITinyhandSerialize<TObject>, IIntegrality
{// Integrate/Differentiate
    public IntegralityEngine()
    {
    }

    public async Task<IntegralityResult> Integrate(TGoshujin obj, IntegralityBrokerDelegate brokerDelegate, CancellationToken cancellationToken = default)
    {
        // Probe
        var rentMemory = this.CreateProbePacket(obj);
        IntegralityResultMemory resultMemory;
        try
        {
            resultMemory = await brokerDelegate(rentMemory, cancellationToken).ConfigureAwait(false);
            if (resultMemory.Result != IntegralityResult.Success)
            {
                resultMemory.Return();
                return resultMemory.Result;
            }
        }
        finally
        {
            rentMemory.Return();
        }

        /*var resultMemory = await brokerDelegate(IntegralityHelper.ProbePacket, cancellationToken).ConfigureAwait(false);
        if (resultMemory.Result != IntegralityResult.Success)
        {
            resultMemory.Return();
            return resultMemory.Result;
        }*/

        // ProbeResponse: resultMemory
        IntegralityResultMemory resultMemory2;
        try
        {
            resultMemory2 = this.ProcessProbeResponsePacket(obj, resultMemory);
            if (resultMemory2.Result != IntegralityResult.Incomplete)
            {
                resultMemory2.Return();
                return resultMemory2.Result;
            }
        }
        finally
        {
            resultMemory.Return();
        }

        // Get: resultMemory2
        try
        {
            resultMemory = await brokerDelegate(resultMemory2.RentMemory, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            resultMemory2.Return();
        }

        // GetResponse: resultMemory

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
            writer.RawWriteUInt8((byte)IntegralityState.Probe);
            writer.RawWriteUInt64(obj.GetIntegralityHash());
            return writer.FlushAndGetRentMemory();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private IntegralityResultMemory ProcessProbeResponsePacket(TGoshujin obj, IntegralityResultMemory resultMemory)
    {
        var reader = new TinyhandReader(resultMemory.RentMemory.Span);
        try
        {
            var span = reader.ReadRaw(sizeof(byte) + sizeof(ulong));
            var state = (IntegralityState)span[0];
            if (state != IntegralityState.ProbeResponse)
            {
                return new(IntegralityResult.InvalidData);
            }

            span = span.Slice(sizeof(byte));
            this.TargetHash = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(span));

            /*var state = (IntegralityState)reader.ReadUInt8();
            if (state != IntegralityState.Probe)
            {
                return new(IntegralityResult.InvalidData);
            }

            this.TargetHash = reader.ReadUInt64();*/
        }
        catch
        {
            return new(IntegralityResult.InvalidData);
        }

        if (obj.GetIntegralityHash() == this.TargetHash)
        {// Identical
            return new(IntegralityResult.Success);
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteUInt8((byte)IntegralityState.Get);
            obj.Integrate(this, ref reader, ref writer);
            return new(IntegralityResult.Incomplete, writer.FlushAndGetRentMemory());
        }
        catch
        {
            return new(IntegralityResult.InvalidData);
        }
        finally
        {
            writer.Dispose();
        }
    }
}
