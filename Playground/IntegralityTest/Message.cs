// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc.Collections;
using System.Threading.Tasks;
using System.Threading;
using Tinyhand;
using ValueLink;
using ValueLink.Integrality;
using Tinyhand.IO;

namespace Playground;



[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class Message
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

    public partial class GoshujinClass
    {
        void ProcessProbeResponse(ref TinyhandReader reader, ref TinyhandWriter writer)
        {
            lock (this.syncObject)
            {
                while (!reader.End)
                {
                    
                    var key = reader.ReadUInt64();
                    key = TinyhandSerializer.Deserialize<ulong>(ref reader);
                    var hash = reader.ReadUInt64();
                    if (this.IdentifierChain.FindFirst(key) is IIntegrality obj)
                    {
                        if (obj.GetIntegralityHash() != hash)
                        {
                            writer.WriteUInt64(key);
                        }
                    }
                }
            }
        }
    }

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
