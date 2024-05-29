// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
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

    private object? keyHashCache;

    #endregion

    public Dictionary<TKey, ulong> GetKeyHashCache<TKey>(bool clear)
        where TKey : struct
    {
        if (this.keyHashCache is not Dictionary<TKey, ulong> dictionary)
        {
            dictionary = new Dictionary<TKey, ulong>();
            this.keyHashCache = dictionary;
        }
        else if (clear)
        {
            dictionary.Clear();
        }

        return dictionary;
    }
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

        // Integrate: resultMemory2
        while (resultMemory2.Result == IntegralityResult.Incomplete &&
            resultMemory2.RentMemory.Length > 1)
        {
            // Get: resultMemory2
            try
            {
                resultMemory = await brokerDelegate(resultMemory2.RentMemory, cancellationToken).ConfigureAwait(false);
                if (resultMemory.Result != IntegralityResult.Success)
                {
                    resultMemory.Return();
                    return resultMemory.Result;
                }
            }
            finally
            {
                resultMemory2.Return();
            }

            // GetResponse: resultMemory
            try
            {
                resultMemory2 = this.ProcessGetResponsePacket(obj, resultMemory);
            }
            finally
            {
                resultMemory.Return();
            }
        }

        resultMemory2.Return();

        // Prune

        if (obj.GetIntegralityHash() == this.TargetHash)
        {// Integrated
            return IntegralityResult.Success;
        }
        else
        {
            return IntegralityResult.Incomplete;
        }
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
            var state = (IntegralityState)reader.ReadRaw<byte>();
            if (state != IntegralityState.ProbeResponse)
            {
                return new(IntegralityResult.InvalidData);
            }

            this.TargetHash = reader.ReadRaw<ulong>();
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
            writer.RawWriteUInt8((byte)IntegralityState.Get);
            obj.Compare(this, ref reader, ref writer);
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

    private IntegralityResultMemory ProcessGetResponsePacket(TGoshujin obj, IntegralityResultMemory resultMemory)
    {
        var reader = new TinyhandReader(resultMemory.RentMemory.Span);
        try
        {
            var state = (IntegralityState)reader.ReadRaw<byte>();
            if (state != IntegralityState.GetResponse)
            {
                return new(IntegralityResult.InvalidData);
            }
        }
        catch
        {
            return new(IntegralityResult.InvalidData);
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.RawWriteUInt8((byte)IntegralityState.Get);
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
