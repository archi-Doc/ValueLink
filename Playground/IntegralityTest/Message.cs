﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

namespace Playground;

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
                    var hash = ((IExaltationOfIntegrality)this).GetIntegralityHash();
                    var writer = TinyhandWriter.CreateFromBytePool();
                    writer.WriteUInt8((byte)IntegralityState.ProbeResponse);
                    writer.WriteUInt64(hash);
                    if (hash != reader.ReadUInt64())
                    {
                        foreach (var x in this)
                        {
                            // var key = x.identifier;
                            writer.WriteUnsafe(x.identifier);
                            writer.WriteUnsafe(((IExaltationOfIntegrality)x).GetIntegralityHash());
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
                        var key = reader.ReadUInt64();
                        var hash = reader.ReadUInt64();
                        cache.TryAdd(key, hash);
                        if (this.IdentifierChain.FindFirst(key) is not IExaltationOfIntegrality obj || obj.GetIntegralityHash() != hash)
                        {
                            writer.WriteUInt64(key);
                        }
                    }
                }
                catch
                {
                }
            }
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
    private ulong identifier;

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
