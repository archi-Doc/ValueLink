// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Crypto;
using Tinyhand;
using Tinyhand.Integrality;
using Tinyhand.IO;
using ValueLink;

namespace Playground;



[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class Message : IIntegrality
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

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

    private ulong integralityHash;

    #endregion

    public bool Validate()
    {
        return true;
    }

    void IIntegrality.ClearIntegralityHash()
        => this.integralityHash = 0;

    ulong IIntegrality.GetIntegralityHash()
        => this.integralityHash != 0 ? this.integralityHash : this.integralityHash = Arc.Crypto.XxHash3.Hash64([]);

    public override string ToString()
        => $"{this.messageBoardIdentifier}-{this.identifier}({this.signedMics}) {this.name} {this.content}";
}
