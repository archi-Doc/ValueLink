using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueLink;

namespace Benchmark.Draft
{
    public class LinkedListChain5<T>
    {
        public LinkedListChain5(Func<T, LinkedListNode<T>?> getter, Action<T, LinkedListNode<T>?> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public void AddLast(T obj)
        {
            var node = this.getter(obj);
            if (node != null)
            {
                this.chain.Remove(node);
            }

            this.setter(obj, this.chain.AddLast(obj));
        }

        public void Remove(T obj)
        {
            var node = this.getter(obj);
            if (node != null)
            {
                this.chain.Remove(node);
                this.setter(obj, null);
            }
        }

        public int Count => this.chain.Count;

        public T? First => this.chain.First == null ? default(T) : this.chain.First.Value;

        private Func<T, LinkedListNode<T>?> getter;
        private Action<T, LinkedListNode<T>?> setter;
        private LinkedList<T> chain = new();
    }

    public sealed class TestGoshujin5
    {
        public TestGoshujin5()
        {
        }

        public void Add(TestClass5 x)
        {
            this.IdChain.AddLast(x);
        }

        public void Remove(TestClass5 x)
        {
            this.IdChain.Remove(x);
        }

        public LinkedListChain5<TestClass5> IdChain = new(static x => x.IdNode, static (x, y) => { x.IdNode = y; });
    }

    public class TestClass5
    {
        public TestClass5(int id)
        {
            this.Id = id;
        }

        public TestGoshujin5 Goshujin
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

        private TestGoshujin5 GoshujinInstance = default!;

        public int Id { get; set; }

        public LinkedListNode<TestClass5>? IdNode { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
