// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using ValueLink.Integrality;
using Tinyhand.IO;
using Arc.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System;
using System.Net.Http.Headers;
using Arc.Crypto;
using System.Collections.Generic;

namespace Playground;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class SimpleIntegralityClass
{
    public SimpleIntegralityClass()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }
}

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class GenericIntegralityClass<T>
    where T : ITinyhandSerialize<T>
{
    public GenericIntegralityClass()
    {
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; set; }

    [Key(1)]
    public T Value { get; set; } = default!;
}

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class Message
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

    /*public partial class GoshujinClass
    {
        IntegralityResultMemory Differentiate(Integrality engine, BytePool.RentMemory integration)
        {
            try
            {
                var reader = new TinyhandReader(integration.Span);
                var state = (IntegralityState)reader.ReadUInt8();
                if (state == IntegralityState.Probe)
                {
                    var hash = ((IIntegralityObject)this).GetIntegralityHash();
                    var writer = TinyhandWriter.CreateFromBytePool();
                    writer.WriteUInt8((byte)IntegralityState.ProbeResponse);
                    writer.WriteUInt64(hash);
                    if (hash != reader.ReadUInt64())
                    {
                        var count = 0;
                        foreach (var x in this)
                        {
                            if (count >= engine.MaxItems) break;
                            // var key = x.identifier;
                            writer.WriteUnsafe(x.identifier);
                            writer.WriteUnsafe(((IIntegralityObject)x).GetIntegralityHash());
                        }
                    }

                    return new(IntegralityResult.Success, writer.FlushAndGetRentMemory());
                }
                else if (state == IntegralityState.Get)
                {
                    var writer = TinyhandWriter.CreateFromBytePool();
                    writer.WriteUInt8((byte)IntegralityState.GetResponse);
                    int written = 0;
                    while (!reader.End)
                    {
                        var key = reader.ReadUnsafe<ulong>();
                        writer.WriteUnsafe(key);
                        if (this.IdentifierChain.FindFirst(key) is { } obj)
                        {
                            TinyhandSerializer.SerializeObject(ref writer, obj);
                        }
                        else
                        {
                            writer.WriteNil();
                        }

                        if (writer.Written < engine.MaxMemoryLength) written = (int)writer.Written;
                        else break;
                    }

                    return new(IntegralityResult.Success, writer.FlushAndGetRentMemory().Slice(0, written));
                }
            }
            catch
            {
            }

            return new(IntegralityResult.InvalidData);
        }


        void Compare(Integrality engine, ref TinyhandReader reader, ref TinyhandWriter writer)
        {
            lock (this.syncObject)
            {
                var cache = engine.GetKeyHashCache<ulong>(true);
                try
                {
                    while (!reader.End)
                    {
                        var key = reader.ReadUnsafe<ulong>();
                        var hash = reader.ReadUnsafe<ulong>();
                        if (this.IdentifierChain.FindFirst(key) is not IIntegralityObject obj || obj.GetIntegralityHash() != hash)
                        {
                            if (cache.Count >= engine.MaxItems) break;
                            cache.TryAdd(key, hash);
                            writer.WriteUInt64(key);
                        }
                    }

                    if (engine.RemoveIfItemNotFound)
                    {
                        List<Message>? list = default;
                        foreach (var x in this.IdentifierChain)
                        {
                            if (!cache.ContainsKey(x.identifier))
                            {
                                list ??= new();
                                list.Add(x);
                            }
                        }
                        if (list is not null) foreach (var x in list) x.Goshujin = default;
                    }
                }
                catch
                {
                }
            }
        }

        void Integrate(Integrality engine, ref TinyhandReader reader, ref TinyhandWriter writer)
        {
            lock (this.syncObject)
            {
                try
                {
                    var cache = engine.GetKeyHashCache<ulong>(true);
                    while (!reader.End)
                    {
                        var key = reader.ReadUnsafe<ulong>();
                        if (reader.TryReadNil()) continue;
                        var obj = TinyhandSerializer.DeserializeObject<Message>(ref reader);
                        cache.Remove(key);
                        this.Integrate(engine, obj);
                    }

                    foreach (var x in cache.Keys) writer.WriteUnsafe(x);
                }
                catch
                {
                }
            }
        }

        IntegralityResult Integrate(Integrality engine, object obj)
        {
            if (obj is not Message newObj || !engine.Validate(newObj))
            {
                return IntegralityResult.InvalidData;
            }

            if (this.IdentifierChain.FindFirst(newObj.identifier) is { } oldObj)
            {
                oldObj.Goshujin = default;
                newObj.Goshujin = this;
            }

            return IntegralityResult.Success;
        }
    }*/

    public Message()
    {
    }

    public Message(uint messageBoardIdentifier, uint identifier, string name, string content, long signedMics)
    {
        this.messageBoardIdentifier = messageBoardIdentifier;
        this.identifier = identifier;
        this.name = name;
        this.content = content;
        this.signedMics = signedMics;
    }

    #region FieldAndProperty

    [Key(0, AddProperty = "Identifier")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    private uint identifier;

    [Key(1, AddProperty = "MessageBoardIdentifier")]
    private ulong messageBoardIdentifier;

    [Key(2)]
    private long signedMics;

    [Key(5, AddProperty = "Name")]
    [MaxLength(MaxNameLength)]
    private string name = default!;

    [Key(7, AddProperty = "Content")]
    [MaxLength(MaxContentLength)]
    private string content = default!;

    [Link(Type = ChainType.Ordered, AddValue = false)]
    public long SignedMics => signedMics;

    #endregion

    public bool Validate()
    {
        return true;
    }

    public override string ToString()
        => $"{this.messageBoardIdentifier}-{this.identifier}({this.signedMics}) {this.name} {this.content}";
}
