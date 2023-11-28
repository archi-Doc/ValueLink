using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueLink;

namespace Benchmark.Draft;

public class LinkedListChain6<T>
{

    public delegate ref Link ObjectToLinkDelegete(T obj);

    // public LinkedListChain6(Func<T, Link> getter, Action<T, Link> setter)
    public LinkedListChain6(ObjectToLinkDelegete objectToLink)
    {
        this.objectToLink = objectToLink;
    }

        public void AddLast(T obj)
    {
        ref LinkedListChain6<T>.Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
        }

        link.Node = this.chain.AddLast(obj);
    }

    public void Remove(T obj)
    {
        ref LinkedListChain6<T>.Link link = ref this.objectToLink(obj);
        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
            link.Node = null;
        }
    }

    /*public void AddLast(T obj)
    {
        var link = this.getter(obj);
        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
        }

        link.Node = this.chain.AddLast(obj);
        this.setter(obj, link);
    }*/

    /*public void Remove(T obj)
    {
        var link = this.getter(obj);
        if (link.Node != null)
        {
            this.chain.Remove(link.Node);
            link.Node = null;
            this.setter(obj, link);
        }
    }*/

    public int Count => this.chain.Count;

    public T? First => this.chain.First == null ? default(T) : this.chain.First.Value;

    public T? Last => this.chain.Last == null ? default(T) : this.chain.Last.Value;

    // private Func<T, Link> getter;
    // private Action<T, Link> setter;
    private ObjectToLinkDelegete objectToLink;
    private LinkedList<T> chain = new();

    public struct Link : ILink<T>
    {
        public bool IsLinked => this.Node != null;

        public T? Next => this.Node == null || this.Node.Next == null ? default(T) : this.Node.Next.Value;

        internal LinkedListNode<T>? Node { get; set; }
    }
}

public sealed class TestGoshujin6
{
    public TestGoshujin6()
    {
    }

    public void Add(TestClass6 x)
    {
        this.IdChain.AddLast(x);
    }

    public void Remove(TestClass6 x)
    {
        this.IdChain.Remove(x);
    }

    // public LinkedListChain6<TestClass6> IdChain = new(static x => x.IdLink, static (x, y) => { x.IdLink = y; });
    public LinkedListChain6<TestClass6> IdChain = new(static x => ref x.IdLink);
}

public class TestClass6
{
    public TestClass6(int id)
    {
        this.Id = id;
    }

    public TestGoshujin6 Goshujin
    {
        get => this.GoshujinInstance;
        set
        {
            if (this.GoshujinInstance != null)
            {
                this.GoshujinInstance.Remove(this);
            }

            this.GoshujinInstance = value;
            this.GoshujinInstance.Add(this);
        }
    }

    private TestGoshujin6 GoshujinInstance = default!;

    public int Id { get; set; }

    public LinkedListChain6<TestClass6>.Link IdLink;

    public string Name { get; set; } = string.Empty;
}
