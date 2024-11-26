using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueLink;
using Tinyhand;
using Tinyhand.IO;

namespace Benchmark.Serializer;

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class SerializerBaseClass
{
    public sealed partial class GoshujinClass : IGoshujin, ITinyhandSerialize
    {
        public GoshujinClass()
        {
            this.StackChain = new(this, static x => x.GoshujinInstance, x => ref x.StackLink);
            this.IdChain = new(this, static x => x.GoshujinInstance, x => ref x.IdLink);
            this.NameChain = new(this, static x => x.GoshujinInstance, x => ref x.NameLink);
        }

        public void Serialize(ref TinyhandWriter writer, TinyhandSerializerOptions options)
        {
            int max = 0;

            // reset index
            max = max > this.StackChain.Count ? max : this.StackChain.Count;
            foreach (var x in this.StackChain)
            {
                x.serializeIndex = -1;
            }

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
            var array = new SerializerBaseClass[max];
            int number = 0;
            foreach (var x in this.StackChain)
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
            var formatter = options.Resolver.GetFormatter<SerializerBaseClass>();
            foreach (var x in array)
            {
                formatter.Serialize(ref writer, x, options);
            }

            writer.WriteMapHeader(3);

            writer.WriteString(GoshujinClass.__gen_utf8_key_0000);
            writer.WriteArrayHeader(this.StackChain.Count);
            foreach (var x in this.StackChain)
            {
                writer.Write(x.serializeIndex);
            }

            writer.WriteString(GoshujinClass.__gen_utf8_key_0001);
            writer.WriteArrayHeader(this.IdChain.Count);
            foreach (var x in this.IdChain)
            {
                writer.Write(x.serializeIndex);
            }

            writer.WriteString(GoshujinClass.__gen_utf8_key_0002);
            writer.WriteArrayHeader(this.NameChain.Count);
            foreach (var x in this.NameChain)
            {
                writer.Write(x.serializeIndex);
            }

            /*var list = this.NameChain.Select(x => x.serializeIndex).ToArray();
            Tinyhand.Formatters.Builtin.SerializeInt32Array(ref writer, list); // options.Resolver.GetFormatter<int[]>().Serialize(ref writer, list, options);*/
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
            var array = new SerializerBaseClass[max];
            var formatter = options.Resolver.GetFormatter<SerializerBaseClass>();
            for (var n = 0; n < max; n++)
            {
                array[n] = formatter.Deserialize(ref reader, options)!;
                array[n].GoshujinInstance = this;
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
                this.StackChain.Clear();
                for (var n = 0; n < number; n++)
                {
                    var i = reader.ReadInt32();
                    if (i >= max) throw new TinyhandException("Invalid index");
                    var x = array[i];
                    this.StackChain.Push(x);
                }
            }

            {
                var name = reader.ReadStringSpan();
                var number = reader.ReadArrayHeader();
                this.IdChain.Clear();
                for (var n = 0; n < number; n++)
                {
                    var i = reader.ReadInt32();
                    if (i >= max) throw new TinyhandException("Invalid index");
                    var x = array[i];
                    this.IdChain.Add(x.Id, x);
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

        public StackListChain<SerializerBaseClass> StackChain { get; }

        public OrderedChain<int, SerializerBaseClass> IdChain { get; }

        public OrderedChain<string, SerializerBaseClass> NameChain { get; }

        private static ReadOnlySpan<byte> __gen_utf8_key_0000 => new byte[] { 83, 116, 97, 99, 107, 67, 104, 97, 105, 110, };
        private static ReadOnlySpan<byte> __gen_utf8_key_0001 => new byte[] { 73, 100, 67, 104, 97, 105, 110, };
        private static ReadOnlySpan<byte> __gen_utf8_key_0002 => new byte[] { 78, 97, 109, 101, 67, 104, 97, 105, 110, };
    }

    public SerializerBaseClass()
    {
    }

    public SerializerBaseClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public GoshujinClass Goshujin
    {
        get => this.GoshujinInstance;
        set
        {
            if (this.GoshujinInstance != null)
            {
                this.GoshujinInstance.StackChain.Remove(this);
                this.GoshujinInstance.IdChain.Remove(this);
                this.GoshujinInstance.NameChain.Remove(this);
            }

            this.GoshujinInstance = value;
            this.GoshujinInstance.StackChain.Push(this);
            this.GoshujinInstance.IdChain.Add(this.Id, this);
            this.GoshujinInstance.NameChain.Add(this.Name, this);
        }
    }

    private GoshujinClass GoshujinInstance = default!;

    private int serializeIndex;

    [KeyAsName]
    public int Id { get; set; }

    [KeyAsName]
    public string Name { get; set; } = string.Empty;

    public StackListChain<SerializerBaseClass>.Link StackLink;

    public OrderedChain<int, SerializerBaseClass>.Link IdLink;

    public OrderedChain<string, SerializerBaseClass>.Link NameLink;
}
