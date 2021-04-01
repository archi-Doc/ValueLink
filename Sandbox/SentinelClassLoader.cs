using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CrossLink;
using Tinyhand;
using Tinyhand.IO;
using Tinyhand.Resolvers;

namespace Sandbox
{
    public static class CrossLinkModule
    {
        private static bool Initialized;

        [ModuleInitializer]
        public static void Initialize()
        {
            if (CrossLinkModule.Initialized) return;
            CrossLinkModule.Initialized = true;

            Generated.__gen__load();
        }
    }

    static class Generated
    {
        internal static void __gen__load()
        {
            GeneratedResolver.Instance.SetFormatter<Sandbox.SentinelClass.SentinelGoshujin>(new __gen__tf__0000());
            GeneratedResolver.Instance.SetFormatterExtra<Sandbox.SentinelClass.SentinelGoshujin>(new __gen__tf__0001());
        }

        class __gen__tf__0000 : ITinyhandFormatter<Sandbox.SentinelClass.SentinelGoshujin>
        {
            public void Serialize(ref TinyhandWriter writer, Sandbox.SentinelClass.SentinelGoshujin? v, TinyhandSerializerOptions options)
            {
                if (v == null) { writer.WriteNil(); return; }
                v.Serialize(ref writer, options);
            }
            public Sandbox.SentinelClass.SentinelGoshujin? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
            {
                if (reader.TryReadNil()) return default;
                var v = new Sandbox.SentinelClass.SentinelGoshujin();
                v.Deserialize(ref reader, options);
                return v;
            }
            public Sandbox.SentinelClass.SentinelGoshujin Reconstruct(TinyhandSerializerOptions options)
            {
                var v = new Sandbox.SentinelClass.SentinelGoshujin();
                return v;
            }
        }
        class __gen__tf__0001 : ITinyhandFormatterExtra<Sandbox.SentinelClass.SentinelGoshujin>
        {
            public Sandbox.SentinelClass.SentinelGoshujin? Deserialize(Sandbox.SentinelClass.SentinelGoshujin reuse, ref TinyhandReader reader, TinyhandSerializerOptions options)
            {
                reuse = reuse ?? new Sandbox.SentinelClass.SentinelGoshujin();
                reuse.Deserialize(ref reader, options);
                return reuse;
            }
        }
    }
}
