using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLink;
using Tinyhand;
using Tinyhand.IO;

namespace Sandbox
{
    [TinyhandObject(ExplicitKeyOnly = true)]
    public partial class SentinelClass
    {
        public sealed class SentinelGoshujin : ITinyhandSerialize
        {
            public void Add(SentinelClass x)
            {
                if (x.GoshujinInstance != null && x.GoshujinInstance != this)
                {
                    x.GoshujinInstance.Remove(x);
                }

                this.IdChain.Add(x);
                this.NameChain.Add(x.Name, x);
                x.GoshujinInstance = this;
            }

            public bool Remove(SentinelClass x)
            {
                if (x.GoshujinInstance == this)
                {
                    this.IdChain.Remove(x);
                    this.NameChain.Remove(x);
                    x.GoshujinInstance = default!;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
            {
                int max = 0;

                // reset index
                max = max > this.IdChain.Count ? max : this.IdChain.Count;
                foreach (var x in this.IdChain)
                {
                    x.serializeIndex = -1;
                }

                max = max > this.NameChain.Count ? max : this.NameChain.Count;
                foreach (var x in this.NameChain)
                {
                    x.serializeIndex = -1;
                }

                // set index
                var array = new SentinelClass[max];
                int number = 0;
                foreach (var x in this.IdChain)
                {
                    if (x.serializeIndex == -1)
                    {
                        if (number >= max)
                        {
                            max <<= 1;
                            Array.Resize(ref array, max);
                        }

                        array[number] = x;
                        x.serializeIndex = number++;
                    }
                }

                foreach (var x in this.NameChain)
                {
                    if (x.serializeIndex == -1)
                    {
                        if (number >= max)
                        {
                            max <<= 1;
                            Array.Resize(ref array, max);
                        }

                        array[number] = x;
                        x.serializeIndex = number++;
                    }
                }

                // serialize
                writer.WriteArrayHeader(2);

                writer.WriteArrayHeader(number);
                var formatter = options.Resolver.GetFormatter<SentinelClass>();
                foreach (var x in array)
                {
                    formatter.Serialize(ref writer, x, options);
                }

                writer.WriteMapHeader(2);
                writer.WriteString(SentinelClass.__gen_utf8_key_0000);
                writer.WriteArrayHeader(this.IdChain.Count);
                foreach (var x in this.IdChain)
                {
                    writer.Write(x.serializeIndex);
                }

                writer.WriteString(SentinelClass.__gen_utf8_key_0001);
                writer.WriteArrayHeader(this.NameChain.Count);
                var list = this.NameChain.Select(x => x.serializeIndex).ToArray();
                Tinyhand.Formatters.Builtin.SerializeInt32Array(ref writer, list);
                // options.Resolver.GetFormatter<int[]>().Serialize(ref writer, list, options);
                foreach (var x in this.NameChain)
                {
                    writer.Write(x.serializeIndex);
                }
            }

            public void Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
            {
                var length = reader.ReadArrayHeader();
                if (length == 0)
                {
                    return;
                }

                // array
                length--;
                var max = reader.ReadArrayHeader();
                var array = new SentinelClass[max];
                var formatter = options.Resolver.GetFormatter<SentinelClass>();
                for (var n = 0; n < max; n++)
                {
                    array[n] = formatter.Deserialize(ref reader, options)!;
                }

                if (length == 0)
                {
                    return;
                }

                // map
                var chains = reader.ReadMapHeader2();

                {
                    var name = reader.ReadStringSpan();
                    var number = reader.ReadArrayHeader();
                    this.IdChain.Clear();
                    for (var n = 0; n < number; n++)
                    {
                        var i = reader.ReadInt32();
                        if (i >= max) throw new TinyhandException("Invalid index");
                        var x = array[i];
                        this.IdChain.Add(x);
                    }
                }

                {
                    var name = reader.ReadStringSpan();
                    var number = reader.ReadArrayHeader();
                    this.NameChain.Clear();
                    for (var n = 0; n < number; n++)
                    {
                        var i = reader.ReadInt32();
                        if (i >= max) throw new TinyhandException("Invalid index");
                        var x = array[i];
                        this.NameChain.Add(x.Name, x);
                    }
                }
            }

            public ListChain<SentinelClass> IdChain = new(static x => ref x.IdLink);

            public OrderedChain<string, SentinelClass> NameChain = new(static x => ref x.NameLink);

            private static ReadOnlySpan<byte> __gen_utf8_key_0000 => new byte[] { 73, 100, };

            private static ReadOnlySpan<byte> __gen_utf8_key_0001 => new byte[] { 78, 97, 109, 101, };
        }

        public SentinelClass()
        {
        }

        public SentinelClass(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public SentinelGoshujin Goshujin
        {
            get => this.GoshujinInstance;
            set
            {
                if (value != this.GoshujinInstance)
                {
                    if (value != null)
                    {
                        value.Add(this);
                    }
                    else if (this.GoshujinInstance != null)
                    {
                        this.GoshujinInstance.Remove(this);
                    }
                }
            }
        }

        private SentinelGoshujin GoshujinInstance = default!;

        private int serializeIndex;

        [KeyAsName]
        public int Id { get; set; }

        [KeyAsName]
        public string Name { get; set; } = string.Empty;

        public ListChain<SentinelClass>.Link IdLink;

        public OrderedChain<string, SentinelClass>.Link NameLink;
    }
}
