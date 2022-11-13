using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Benchmark.Serializer;
using ValueLink;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;

namespace Sandbox
{
    public static class ValueLinkModule
    {
        private static bool Initialized;

        [ModuleInitializer]
        public static void Initialize()
        {
            if (ValueLinkModule.Initialized) return;
            ValueLinkModule.Initialized = true;

            Generated.__gen__load();
        }
    }

    static class Generated
    {
        internal static void __gen__load()
        {
            GeneratedResolver.Instance.SetFormatter<Benchmark.Serializer.SerializerBaseClass.GoshujinClass>(new __gen__tf__0000());
            // GeneratedResolver.Instance.SetFormatterExtra<Benchmark.Serializer.SerializerBaseClass.GoshujinClass>(new __gen__tf__0001());
        }

        class __gen__tf__0000 : ITinyhandFormatter<Benchmark.Serializer.SerializerBaseClass.GoshujinClass>
        {
            public void Serialize(ref TinyhandWriter writer, Benchmark.Serializer.SerializerBaseClass.GoshujinClass? v, TinyhandSerializerOptions options)
            {
                if (v == null) { writer.WriteNil(); return; }
                v.Serialize(ref writer, options);
            }
            public Benchmark.Serializer.SerializerBaseClass.GoshujinClass? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
            {
                if (reader.TryReadNil()) return default;
                var v = new Benchmark.Serializer.SerializerBaseClass.GoshujinClass();
                v.Deserialize(ref reader, options);
                return v;
            }
            public Benchmark.Serializer.SerializerBaseClass.GoshujinClass Reconstruct(TinyhandSerializerOptions options)
            {
                var v = new Benchmark.Serializer.SerializerBaseClass.GoshujinClass();
                return v;
            }

            public SerializerBaseClass.GoshujinClass? Clone(SerializerBaseClass.GoshujinClass? value, TinyhandSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
