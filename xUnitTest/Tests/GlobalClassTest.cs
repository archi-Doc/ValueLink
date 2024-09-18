using Tinyhand;
using ValueLink;

[ValueLinkObject]
[TinyhandObject]
public partial class GlobalClassTest
{
    [Link(Type = ChainType.Ordered, Primary = true)]
    [Key(0)]
    private int id;

    [Link(Type = ChainType.Ordered)]
    [Key(1)]
    private string name = default!;

    [Link(Type = ChainType.Ordered, AddValue = false)]
    public int Length => this.name.Length;

    public GlobalClassTest()
    {
    }
}
