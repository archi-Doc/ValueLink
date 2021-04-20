using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLink;

namespace Benchmark.Draft
{
    public class TestClassGeneric<T>
    {
        public sealed class TestGoshujinGeneric : IGoshujin
        {
            public TestGoshujinGeneric()
            {
                this.IdChain = new(this, static x => x.Goshujin, static x => ref x.IdLink);
            }

            public void Add(TestClassGeneric<T> x)
            {
                this.IdChain.AddLast(x);
            }

            public void Remove(TestClassGeneric<T> x)
            {
                this.IdChain.Remove(x);
            }

            // public LinkedListChain6<TestClass6> IdChain = new(static x => x.IdLink, static (x, y) => { x.IdLink = y; });
            public LinkedListChain<TestClassGeneric<T>> IdChain { get; }
        }

        public TestClassGeneric(int id)
        {
            this.Id = id;
            this.value = default(T)!;
        }

        public TestGoshujinGeneric Goshujin
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

        private TestGoshujinGeneric GoshujinInstance = default!;

        public int Id { get; set; }

        public LinkedListChain<TestClassGeneric<T>>.Link IdLink;

        public OrderedChain<T, TestClassGeneric<T>>.Link TLink;

        public T value;

        public string Name { get; set; } = string.Empty;
    }
}
