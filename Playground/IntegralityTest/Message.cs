﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tinyhand;
using ValueLink;

namespace Playground;



[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class Message
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

    /*public partial class GoshujinClass : IIntegrality
    {
        private ulong integralityHash;
        void IIntegrality.ClearIntegralityHash() => this.integralityHash = 0;
        ulong IIntegrality.GetIntegralityHash()
        {
            if (this.integralityHash != 0) return this.integralityHash;

            byte[]? rent = null;
            lock (this.syncObject)
            {
                var keyLength = Unsafe.SizeOf<ulong>();
                var length = (keyLength + sizeof(ulong)) * this.Count;
                Span<byte> span = length <= 4096 ?
                stackalloc byte[length] : (rent = ArrayPool<byte>.Shared.Rent(length));
                var s = span;
                foreach (var x in this.IdentifierChain)
                {
                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(s), x.identifier);
                    s = s.Slice(keyLength);
                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(s), ((IIntegrality)x).GetIntegralityHash());
                }

                this.integralityHash = Arc.Crypto.XxHash3.Hash64(span);
            }

            if (rent is not null) ArrayPool<byte>.Shared.Return(rent);
            return this.integralityHash;
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
