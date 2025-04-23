using ValueLink;
using Xunit;

namespace xUnitTest.Tests;

[ValueLinkObject]
public partial class PartialPropertyTestClass
{
    [Link(Type = ChainType.Ordered)]
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
    }
}
