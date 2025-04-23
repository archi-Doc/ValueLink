using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest.Tests;

[ValueLinkObject]
public partial class PartialPropertyTestClass
{
    [Link(Primary = true, Type = ChainType.Ordered)]
    public required partial int Id { get; set; }

    [Link(Type = ChainType.Ordered)]
    public required partial int Id2 { get; init; }
}

public class PartialPropertyTest
{
    [Fact]
    public void Test1()
    {
        var g = new PartialPropertyTestClass.GoshujinClass();
        g.Add(new() { Id = 1, Id2 = 1,});
        g.Add(new() { Id = 2, Id2 = 0,});
        g.Add(new() { Id = 0, Id2 = 10, });
        var array = g.IdChain.Select(x => x.Id).ToArray();
        array.SequenceEqual([0, 1, 2]);
        array = g.Select(x => x.Id2).ToArray();
        array.SequenceEqual([10, 1, 0]);
        array = g.Id2Chain.Select(x => x.Id2).ToArray();
        array.SequenceEqual([0, 1, 10]);
    }
}
