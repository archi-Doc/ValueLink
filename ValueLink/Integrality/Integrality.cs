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

public interface IIntegralityInternal
{
    int MaxItems { get; }

    bool RemoveIfItemNotFound { get; }

    int MaxMemoryLength { get; }

    int MaxIntegrationCount { get; }

    public ulong TargetHash { get; }

    Dictionary<TKey, ulong> GetKeyHashCache<TKey>(bool clear)
        where TKey : struct;

    bool ValidateInternal(object goshujin, object newItem, object? oldItem);
}

public abstract class Integrality : IIntegralityInternal
{
    public delegate Task<IntegralityResultMemory> BrokerDelegate(BytePool.RentMemory integration, CancellationToken cancellationToken);

    public Integrality()
    {
    }

    #region FieldAndProperty

    public required int MaxItems { get; init; }

    public required bool RemoveIfItemNotFound { get; init; }

    public int MaxMemoryLength { get; init; } = (1024 * 1024 * 4) - 1024; // ConnectionAgreement.MaxBlockSize

    public int MaxIntegrationCount { get; init; } = 3;

    public ulong TargetHash { get; protected set; }

    private object? keyHashCache;

    #endregion

    Dictionary<TKey, ulong> IIntegralityInternal.GetKeyHashCache<TKey>(bool clear)
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

    public virtual bool ValidateInternal(object goshujin, object newItem, object? oldItem)
        => true;
}

public class Integrality<TGoshujin, TObject> : Integrality
    where TGoshujin : class, IGoshujin, IIntegralityObject
    where TObject : class, ITinyhandSerialize<TObject>, IIntegralityObject
{// Integrate/Differentiate
    public Integrality()
    {
    }

    public IntegralityResult Integrate(TGoshujin goshujin, TObject obj)
    {
        return goshujin.Integrate(this, obj);
    }

    public async Task<IntegralityResult> Integrate(TGoshujin goshujin, Integrality.BrokerDelegate brokerDelegate, CancellationToken cancellationToken = default)
    {
        // Probe
        var rentMemory = this.CreateProbePacket(goshujin);
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
            resultMemory2 = this.ProcessProbeResponsePacket(goshujin, resultMemory);
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
        var integrationCount = 0;
        while (resultMemory2.Result == IntegralityResult.Incomplete)
        {
            if (integrationCount++ >= this.MaxIntegrationCount)
            {
                break;
            }

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
                resultMemory2 = this.ProcessGetResponsePacket(goshujin, resultMemory);
            }
            finally
            {
                resultMemory.Return();
            }
        }

        resultMemory2.Return();

        // Prune
        this.Prune(goshujin);

        if (goshujin.GetIntegralityHash() == this.TargetHash)
        {// Integrated
            return IntegralityResult.Success;
        }
        else
        {
            return IntegralityResult.Incomplete;
        }
    }

    public virtual bool Validate(TGoshujin goshujin, TObject newItem, TObject? oldItem)
        => true;

    public virtual void Prune(TGoshujin goshujin)
    {
    }

    public override bool ValidateInternal(object goshujin, object newItem, object? oldItem)
        => this.Validate((TGoshujin)goshujin, (TObject)newItem, oldItem as TObject);

    private BytePool.RentMemory CreateProbePacket(TGoshujin goshujin)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteRawUInt8((byte)IntegralityState.Probe);
            writer.WriteRawUInt64(goshujin.GetIntegralityHash());
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
            var state = (IntegralityState)reader.ReadUnsafe<byte>();
            if (state != IntegralityState.ProbeResponse)
            {
                return new(IntegralityResult.InvalidData);
            }

            this.TargetHash = reader.ReadUnsafe<ulong>();
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
            writer.WriteRawUInt8((byte)IntegralityState.Get);
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
            var state = (IntegralityState)reader.ReadUnsafe<byte>();
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
            writer.WriteRawUInt8((byte)IntegralityState.Get);
            obj.Integrate(this, ref reader, ref writer);
            if (writer.Written <= 1)
            {
                return new(IntegralityResult.Success, default);
            }
            else
            {
                return new(IntegralityResult.Incomplete, writer.FlushAndGetRentMemory());
            }
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
