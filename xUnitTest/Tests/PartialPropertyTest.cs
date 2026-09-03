using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest.Tests;

/// <summary>
/// Provides a fixture for tests of generated partial property accessors.
/// </summary>
[ValueLinkObject]
public partial class PartialPropertyTestClass
{
    [Link(Primary = true, Type = ChainType.Ordered)]
    public required partial int Id { get; set; }

    [Link(Type = ChainType.Ordered)]
    public required partial int Id2 { get; init; }
}

/// <summary>
/// Tests generated partial property accessors.
/// </summary>
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
        array.SequenceEqual([0, 1, 2]).IsTrue();
        array = g.Select(x => x.Id2).ToArray();
        array.SequenceEqual([10, 1, 0]).IsTrue();
        array = g.Id2Chain.Select(x => x.Id2).ToArray();
        array.SequenceEqual([0, 1, 10]).IsTrue();
    }
}
