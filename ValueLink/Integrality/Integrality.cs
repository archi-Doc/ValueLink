// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable CS1998
#pragma warning disable SA1124

namespace ValueLink.Integrality;

/// <summary>
/// Represents the class used for object integration.
/// </summary>
/// <typeparam name="TGoshujin">The type of the Goshujin.</typeparam>
/// <typeparam name="TObject">The type of the Object.</typeparam>
public class Integrality<TGoshujin, TObject> : IIntegralityInternal
    where TGoshujin : class, IGoshujin, IIntegralityObject
    where TObject : class, ITinyhandSerialize<TObject>, IIntegralityObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Integrality{TGoshujin, TObject}"/> class.
    /// </summary>
    public Integrality()
    {
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets the maximum number of items.
    /// </summary>
    public required int MaxItems { get; init; }

    /// <summary>
    /// Gets a value indicating whether to remove the item if it is not found.
    /// </summary>
    public required bool RemoveIfItemNotFound { get; init; }

    /// <summary>
    /// Gets the maximum memory length.
    /// </summary>
    public int MaxMemoryLength { get; init; } = (1024 * 1024 * 4) - 1024; // ConnectionAgreement.MaxBlockSize

    /// <summary>
    /// Gets the maximum integration count.
    /// </summary>
    public int MaxIntegrationCount { get; init; } = 3;

    /// <summary>
    /// Gets or sets the target hash.
    /// </summary>
    public ulong TargetHash { get; protected set; }

    private object? keyHashCache;

    #endregion

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    bool IIntegralityInternal.Validate(object goshujin, object newItem, object? oldItem)
        => this.Validate((TGoshujin)goshujin, (TObject)newItem, oldItem as TObject);

    /// <summary>
    /// Integrates the specified object into the Goshujin.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="obj">The object to integrate.</param>
    /// <returns>The integration result.</returns>
    public IntegralityResult IntegrateObject(TGoshujin goshujin, TObject obj)
        => goshujin.IntegrateObject(this, obj);

    /*public IntegralityResult IntegrateForTest(TGoshujin goshujin, TGoshujin target)
        => this.Integrate(goshujin, (x, y) => Task.FromResult(target.Differentiate(this, x))).Result;*/

    /// <summary>
    /// Integrates the Goshujin using the specified broker delegate.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="brokerDelegate">The broker delegate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The integration result.</returns>
    public async Task<IntegralityResult> Integrate(TGoshujin goshujin, IntegralityBrokerDelegate brokerDelegate, CancellationToken cancellationToken = default)
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
        if (goshujin is ISyncObject g)
        {
            lock (g.SyncObject)
            {
                this.Prune(goshujin);
            }
        }
        else
        {
            this.Prune(goshujin);
        }

        if (goshujin.GetIntegralityHash() == this.TargetHash)
        {// Integrated
            return IntegralityResult.Success;
        }
        else
        {
            return IntegralityResult.Incomplete;
        }
    }

    /// <summary>
    /// Validates the specified new item in the Goshujin.<br/>
    /// If Goshujin's isolation level is set to <see cref="IsolationLevel.Serializable"/>, this function will be executed within a lock(goshujin.syncObject) statement.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="newItem">The new item to validate.</param>
    /// <param name="oldItem">The old item to compare against.</param>
    /// <returns><c>true</c> if the new item is valid; otherwise, <c>false</c>.</returns>
    public virtual bool Validate(TGoshujin goshujin, TObject newItem, TObject? oldItem)
        => true;

    /// <summary>
    /// Prunes the Goshujin.<br/>
    /// If Goshujin's isolation level is set to <see cref="IsolationLevel.Serializable"/>, this function will be executed within a lock(goshujin.syncObject) statement.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    public virtual void Prune(TGoshujin goshujin)
    {
    }

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
