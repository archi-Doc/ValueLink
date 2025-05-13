// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
[TinyhandObject]
public partial class EmptyClass
{
}

[TinyhandObject]
public partial class EmptyClass2
{
    [Key(0)]
    public EmptyClass.GoshujinClass Goshujin { get; set; } = new();
}

public class EmptyTest
{
    [Fact]
    public void Test1()
    {
        var c = new EmptyClass();
        var b = TinyhandSerializer.Serialize(c);

        var c2 = new EmptyClass2();
        b = TinyhandSerializer.Serialize(c2);
        var c3 = TinyhandSerializer.Deserialize<EmptyClass2>(b)!;
    }
}
