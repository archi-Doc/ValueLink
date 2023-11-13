// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Tinyhand.IO;
using ValueLink;
using Xunit;

namespace xUnitTest.CrystalDataTest;

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record CreditData
{
    public CreditData()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0, AddProperty = "Credit", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    private int credit;

    // [Key(3, AddProperty = "Borrowers", PropertyAccessibility = PropertyAccessibility.GetterOnly)]
    // private StorageData<Borrower.GoshujinClass> borrowers = new();
}

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public sealed partial record Borrower // : ITinyhandCustomJournal
{
    public Borrower()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private long id;
}
