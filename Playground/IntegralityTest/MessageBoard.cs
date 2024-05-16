// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;

namespace Playground;

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record MessageBoard
{
    public const int MaxMessages = 1_000;

    public MessageBoard()
    {
    }

    [Key(0, AddProperty = "Identifier")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    private ulong identifier;

    [Key(1, AddProperty = "Description")]
    private Message description = default!;

    [Key(2, AddProperty = "Messages", Selection = false)]
    private Message.GoshujinClass messages = default!;
}
