// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
/// Represents a class for object integration.<br/>
/// Since this class is immutable, it can be used by multiple threads.<br/>
/// Set the properties according to the use case, and override <see cref="Validate(TGoshujin, TObject, TObject?)"/> and <see cref="Trim(TGoshujin, int)"/> as needed.
/// </summary>
/// <typeparam name="TGoshujin">The type of the Goshujin.</typeparam>
/// <typeparam name="TObject">The type of the Object.</typeparam>
public class Integrality<TGoshujin, TObject> : IIntegralityInternal
    where TGoshujin : class, IGoshujin, IIntegralityGoshujin
    where TObject : class, ITinyhandSerializable<TObject>, IIntegralityObject
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
    public int MaxMemoryLength { get; init; } = IntegralityConstants.DefaultMaxMemoryLength;

    /// <summary>
    /// Gets the maximum integration count.
    /// </summary>
    public int MaxIntegrationCount { get; init; } = IntegralityConstants.DefaultMaxIntegrationCount;

    #endregion

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

    /// <summary>
    /// Integrates the Goshujin using the specified broker delegate.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="brokerDelegate">The broker delegate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The integration result.</returns>
    public async Task<IntegralityResultAndCount> Integrate(TGoshujin goshujin, IntegralityBrokerDelegate brokerDelegate, CancellationToken cancellationToken = default)
    {
        // Probe
        var rentMemory = this.CreateProbePacket(goshujin);
        BytePool.RentMemory resultMemory;
        try
        {
            resultMemory = await brokerDelegate(rentMemory.Memory, cancellationToken).ConfigureAwait(false);
            IntegralityResultHelper.ParseMemoryAndResult(resultMemory, out var result);
            if (result != IntegralityResult.Success)
            {
                resultMemory.Return();
                return new(result);
            }
        }
        finally
        {
            rentMemory.Return();
        }

        // ProbeResponse: resultMemory
        (IntegralityResult Result, BytePool.RentMemory RentMemory) resultMemory2;
        ulong targetHash;
        try
        {
            resultMemory2 = this.ProcessProbeResponsePacket(goshujin, resultMemory.Memory, out targetHash);
            if (resultMemory2.Result != IntegralityResult.Incomplete)
            {
                resultMemory2.RentMemory.Return();
                return new(resultMemory2.Result);
            }
        }
        finally
        {
            resultMemory.Return();
        }

        // Integrate: resultMemory2
        var iterationCount = 0;
        var integratedCount = 0;
        var trimmedCount = 0;
        while (resultMemory2.Result == IntegralityResult.Incomplete)
        {
            if (iterationCount++ >= this.MaxIntegrationCount)
            {
                break;
            }

            // Get: resultMemory2
            try
            {
                resultMemory = await brokerDelegate(resultMemory2.RentMemory.Memory, cancellationToken).ConfigureAwait(false);
                IntegralityResultHelper.ParseMemoryAndResult(resultMemory, out var result);
                if (result != IntegralityResult.Success)
                {
                    resultMemory.Return();
                    return new(result);
                }
            }
            finally
            {
                resultMemory2.RentMemory.Return();
            }

            // GetResponse: resultMemory
            try
            {
                resultMemory2 = this.ProcessGetResponsePacket(goshujin, resultMemory.Memory, ref integratedCount);
            }
            finally
            {
                resultMemory.Return();
            }
        }

        resultMemory2.RentMemory.Return();

        // Trim
        if (goshujin is ILockObject g)
        {
            using (g.LockObject.EnterScope())
            {
                trimmedCount = this.Trim(goshujin, integratedCount);
            }
        }
        else
        {
            trimmedCount = this.Trim(goshujin, integratedCount);
        }

        return new(
            goshujin.GetIntegralityHash() == targetHash ? IntegralityResult.Success : IntegralityResult.Incomplete,
            iterationCount,
            integratedCount,
            trimmedCount);
    }

    /// <summary>
    /// Retrieve the difference data between the source and the target of the integration.
    /// </summary>
    /// <param name="target">The target Goshujin.</param>
    /// <param name="integration">The data sent from the source to the target when calculating the difference.</param>
    /// <returns>The data sent from the target to the source for integration.</returns>
    public BytePool.RentMemory Differentiate(TGoshujin target, ReadOnlyMemory<byte> integration)
        => target.Differentiate(this, integration);

    /// <summary>
    /// Validates the specified new item in the Goshujin.<br/>
    /// If Goshujin's isolation level is set to <see cref="IsolationLevel.Serializable"/>, this function will be executed within a lock (goshujin.LockObject) statement.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="newItem">The new item to validate.</param>
    /// <param name="oldItem">The old item with the same key to compare against.</param>
    /// <returns><c>true</c> if the new item is valid; otherwise, <c>false</c>.</returns>
    public virtual bool Validate(TGoshujin goshujin, TObject newItem, TObject? oldItem)
        => true;

    /// <summary>
    /// Called in the final stage of <see cref="Integrate(TGoshujin, IntegralityBrokerDelegate, CancellationToken)" />() to remove any objects from Goshujin that exceed the limit or are invalid.<br/>
    /// If Goshujin's isolation level is set to <see cref="IsolationLevel.Serializable"/>, this function will be executed within a lock (goshujin.LockObject) statement.
    /// </summary>
    /// <param name="goshujin">The Goshujin.</param>
    /// <param name="integratedCount">The number of successfully integrated objects.</param>
    /// <returns>The number of objects trimmed.</returns>
    public virtual int Trim(TGoshujin goshujin, int integratedCount)
    {
        return 0;
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

    private (IntegralityResult Result, BytePool.RentMemory RentMemory) ProcessProbeResponsePacket(TGoshujin goshujin, Memory<byte> memory, out ulong targetHash)
    {
        var reader = new TinyhandReader(memory.Span);
        try
        {
            var state = (IntegralityState)reader.ReadUnsafe<byte>();
            if (state != IntegralityState.ProbeResponse)
            {
                targetHash = 0;
                return (IntegralityResult.InvalidData, default);
            }

            targetHash = reader.ReadUnsafe<ulong>();
        }
        catch
        {
            targetHash = 0;
            return (IntegralityResult.InvalidData, default);
        }

        if (goshujin.GetIntegralityHash() == targetHash)
        {// Identical
            return (IntegralityResult.Success, default);
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteRawUInt8((byte)IntegralityState.Get);
            goshujin.Compare(this, ref reader, ref writer);
            return (IntegralityResult.Incomplete, writer.FlushAndGetRentMemory());
        }
        catch
        {
            return (IntegralityResult.InvalidData, default);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private (IntegralityResult Result, BytePool.RentMemory RentMemory) ProcessGetResponsePacket(TGoshujin obj, Memory<byte> memory, ref int integratedObjects)
    {
        var reader = new TinyhandReader(memory.Span);
        try
        {
            var state = (IntegralityState)reader.ReadUnsafe<byte>();
            if (state != IntegralityState.GetResponse)
            {
                return (IntegralityResult.InvalidData, default);
            }
        }
        catch
        {
            return (IntegralityResult.InvalidData, default);
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteRawUInt8((byte)IntegralityState.Get);
            obj.Integrate(this, ref reader, ref writer, ref integratedObjects);
            if (writer.Written <= 1)
            {
                return (IntegralityResult.Success, default);
            }
            else
            {
                return (IntegralityResult.Incomplete, writer.FlushAndGetRentMemory());
            }
        }
        catch
        {
            return (IntegralityResult.InvalidData, default);
        }
        finally
        {
            writer.Dispose();
        }
    }
}
