// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using ValueLink;
using Tinyhand;
using Xunit;
using System.Linq;

namespace xUnitTest;

[ValueLinkObject]
[TinyhandObject]
public partial class AdditionalMethodClass
{
    public static int TotalAge;

    [Link(Primary = true, Type = ChainType.Ordered, AddValue = true)]
    [MemberNameAsKey]
    private int Id;

    [Link(Type = ChainType.Ordered)]
    [MemberNameAsKey]
    public int Age { get; set; }

    [MemberNameAsKey]
    public string Name { get; set; } = string.Empty;

    // [Link<DelegateTestClass>(Name = "StartingWithA", Type = ChainType.LinkedList, PredicateLink = Predicate)]
    // Unfortunately, the support for generic attributes is insufficient, so...

    [Link(Name = "StartingWithA", Type = ChainType.LinkedList)]
    public AdditionalMethodClass()
    {
    }

    public AdditionalMethodClass(int id, int age, string name)
    {
        this.Id = id;
        this.Age = age;
        this.Name = name;
    }

    protected bool StartingWithALinkPredicate()
    {
        return this.Name.StartsWith('A');
    }

    protected bool AgeLinkPredicate()
    {
        return this.Age >= 20;
    }

    protected void AgeLinkAdded()
    {
        TotalAge += this.Age;
    }

    protected void AgeLinkRemoved()
    {
        TotalAge -= this.Age;
    }

    /*private static bool Predicate(DelegateTestClass x)
        => x.Age >= 20;*/
}

public class AdditionalMethodTest
{
    [Fact]
    public void Test1()
    {
        var g = new AdditionalMethodClass.GoshujinClass();
        var c1 = new AdditionalMethodClass(3, 20, "ABC");
        var c2 = new AdditionalMethodClass(1, 3, "D");
        var c3 = new AdditionalMethodClass(2, 10, "E");
        var c4 = new AdditionalMethodClass(4, 40, "F");

        g.Add(c1);
        g.Add(c2);
        g.Add(c3);
        g.Add(c4);

        g.IdChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 1, 2, 3, 4, });
        g.StartingWithAChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 3, });
        g.AgeChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 3, 4, });

        AdditionalMethodClass.TotalAge.Is(60);

        c1.Goshujin = null;
        c2.Goshujin = null;
        c3.Goshujin = null;
        c4.Goshujin = null;

        AdditionalMethodClass.TotalAge.Is(0);

        g.Add(c1);
        g.Add(c2);
        g.Add(c3);
        g.Add(c4);

        var bytes = TinyhandSerializer.Serialize(g);
        var g2 = TinyhandSerializer.Deserialize<AdditionalMethodClass.GoshujinClass>(bytes)!;

        g2.IdChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 1, 2, 3, 4, });
        g2.StartingWithAChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 3, });
        g2.AgeChain.Select(x => x.IdValue).ToArray().SequenceEqual(new int[] { 3, 4, });

        AdditionalMethodClass.TotalAge.Is(120);
    }
}
