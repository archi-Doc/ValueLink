using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueLink;

namespace Sandbox.Design;

[ValueLinkObject]
public partial class SimpleClass
{
    [Link(Type = ChainType.Ordered)]
    private int x;

    [Link(Type = ChainType.Unordered)]
    private string name = string.Empty;

    private static void Test()
    {
        var c = new SimpleClass();
        c.xValue = 19;
    }
}

[ValueLinkObject]
public partial class SimpleClass2
{
    [Link(Type = ChainType.Ordered, Name = "X")]
    private int x;

    [Link(Type = ChainType.Unordered, Name = "Name")]
    private string name = string.Empty;

    private static void Test()
    {
        var c = new SimpleClass2();
        c.XValue = 19;
    }
}

[ValueLinkObject]
public partial class SimpleClass3
{
    [Link(Type = ChainType.Ordered, Name = "X")]
    public int X { get; private set; }

    [Link(Type = ChainType.Unordered, Name = "Name")]
    public string Name { get; private set; } = string.Empty;

    private static void Test()
    {
        var c = new SimpleClass2();
        c.XValue = 19;
    }
}

public class ProtectedBaseClass
{
    public int X { get; protected set; }

    public string Name { get; protected set; } = string.Empty;
}

[ValueLinkObject]
public partial class ProtectedDerivedClass : ProtectedBaseClass
{
    [Link(TargetMember = nameof(X), Type = ChainType.Ordered)]
    public ProtectedDerivedClass()
    {
    }
}

public static class DesignProgram
{
    public static void Test()
    {
        var bc = new ProtectedBaseClass();

        var dc = new ProtectedDerivedClass();
        dc.XValue = 10;
    }
}
