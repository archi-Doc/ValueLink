using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLink;

namespace Benchmark.Draft
{
    public class ListChain3<T>
    {
        public ListChain3(Func<T, Link> objectToLink)
        {
            this.ObjectToLink = objectToLink;
        }

        public void Add(T obj)
        {
            var link = this.ObjectToLink(obj);
            if (link.IsLinked)
            {
                this.chain.Remove(obj);
            }

            this.chain.Add(obj);
            link.IsLinked = true;
        }

        public void Remove(T obj)
        {
            var link = this.ObjectToLink(obj);
            if (link.IsLinked)
            {
                this.chain.Remove(obj);
                link.IsLinked = false;
            }
        }

        public T this[int index]
        {
            get => this.chain[index];
            set
            {
                this.chain[index] = value;
            }
        }

        public int Count => this.chain.Count;

        private Func<T, Link> ObjectToLink;
        private List<T> chain = new();

        public sealed class Link : ILink<T>
        {
            public Link()
            {
            }

            public bool IsLinked { get; internal set; }
        }
    }

    public sealed class TestGoshujin3
    {
        public TestGoshujin3()
        {
        }

        public void Add(TestClass3 x)
        {
            this.IdChain.Add(x);
        }

        public void Remove(TestClass3 x)
        {
            this.IdChain.Remove(x);
        }

        public ListChain3<TestClass3> IdChain = new(static x => x.IdLink);
    }

    public class TestClass3
    {
        public TestClass3(int id)
        {
            this.Id = id;
        }

        public TestGoshujin3 Goshujin
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

        private TestGoshujin3 GoshujinInstance = default!;

        public int Id { get; set; }

        public ListChain3<TestClass3>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new());

        private ListChain3<TestClass3>.Link? IdLinkInstance;

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestGoshujin4
    {
        public TestGoshujin4()
        {
        }

        public void Add(TestClass4 x)
        {
            this.IdChain.AddLast(x);
        }

        public void Remove(TestClass4 x)
        {
            this.IdChain.Remove(x);
        }

        public LinkedListChain<TestClass4> IdChain = new(static x => x.IdLink);
    }

    public class TestClass4
    {
        public TestClass4(int id)
        {
            this.Id = id;
        }

        public TestGoshujin4 Goshujin
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

        private TestGoshujin4 GoshujinInstance = default!;

        public int Id { get; set; }

        public LinkedListChain<TestClass4>.Link IdLink => this.IdLinkInstance != null ? this.IdLinkInstance : (this.IdLinkInstance = new());

        private LinkedListChain<TestClass4>.Link? IdLinkInstance;

        public string Name { get; set; } = string.Empty;
    }
}
