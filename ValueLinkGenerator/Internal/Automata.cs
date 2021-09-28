// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral
{
    public enum AutomataAddNodeResult
    {
        Success,
        KeyCollision,
        NullKey,
    }

    internal class Automata<TObj, TMember>
    {
        public const int MaxStringKeySizeInBytes = 512;

        public Automata(TObj obj, Action<TObj, ScopingStringBuilder, object?, TMember> generateMethod)
        {
            this.Object = obj;
            this.GenerateMethod = generateMethod;
            this.root = new Node(this, 0);
        }

        public TObj Object { get; }

        public Action<TObj, ScopingStringBuilder, object?, TMember> GenerateMethod { get; }

        public (Node? node, AutomataAddNodeResult result, bool keyResized) AddNode(string name, TMember member)
        {
            var keyResized = false;

            var utf8 = Encoding.UTF8.GetBytes(name);
            if (utf8.Length > MaxStringKeySizeInBytes)
            {// String key size limit.
                keyResized = true;
                Array.Resize(ref utf8, MaxStringKeySizeInBytes);
            }

            if (this.NameToNode.TryGetValue(utf8, out var node))
            {// Key collision
                return (node, AutomataAddNodeResult.KeyCollision, keyResized);
            }

            if (utf8.Length == 0 || utf8.Any(x => x == 0))
            {// Null key
                return (null, AutomataAddNodeResult.NullKey, keyResized);
            }

            node = this.root;
            ReadOnlySpan<byte> bytes = utf8;
            while (bytes.Length > 0)
            {
                var key = AutomataKey.GetKey(ref bytes);

                if (key == 0)
                {
                }
                else if (bytes.Length == 0)
                {// leaf node
                    node = node.Add(key, this.NodeList.Count, member, utf8);
                    this.NodeList.Add(node);
                }
                else
                {// branch node
                    node = node.Add(key);
                }
            }

            this.NameToNode[utf8] = node;

            return (node, AutomataAddNodeResult.Success, keyResized);
        }

        public void Generate(ScopingStringBuilder ssb, object? info)
        {
            ssb.AppendLine("ulong key;");
            ssb.AppendLine("var utf8 = reader.ReadStringSpan();");
            using (var c = ssb.ScopeBrace("if (utf8.Length == 0)"))
            {
                ssb.AppendLine("goto SkipLabel;");
            }

            ssb.AppendLine("key = global::Arc.Visceral.AutomataKey.GetKey(ref utf8);");

            this.GenerateCore(ssb, info);

            ssb.AppendLine("continue;");
            ssb.AppendLine("SkipLabel:", false);
            ssb.AppendLine("reader.Skip();");
        }

        private void GenerateCore(ScopingStringBuilder ssb, object? info, Node? node = null)
        {
            if (node == null)
            {
                node = this.root;
            }

            if (node.Nexts == null)
            {
                ssb.AppendLine("goto SkipLabel;");
                return;
            }

            this.GenerateNode(ssb, info, node.Nexts.Values.ToArray());
        }

        private void GenerateChildrenNexts(ScopingStringBuilder ssb, object? info, Node[] childrenNexts)
        {// childrenNexts.Length > 0
            if (childrenNexts.Length == 1)
            {
                var x = childrenNexts[0];
                ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
                ssb.AppendLine("key = global::Arc.Visceral.AutomataKey.GetKey(ref utf8);");
                this.GenerateCore(ssb, info, x);
                return;
            }

            var firstFlag = true;
            foreach (var x in childrenNexts)
            {
                var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
                firstFlag = false;
                using (var c = ssb.ScopeBrace(condition))
                {
                    ssb.AppendLine("key = global::Arc.Visceral.AutomataKey.GetKey(ref utf8);");
                    this.GenerateCore(ssb, info, x);
                }
            }

            // ssb.GotoSkipLabel();
        }

        private void GenerateValueNexts(ScopingStringBuilder ssb, object? info, Node[] valueNexts)
        {// valueNexts.Length > 0
            if (valueNexts.Length == 1)
            {
                var x = valueNexts[0];
                ssb.AppendLine($"if (key != 0x{x.Key:X}) goto SkipLabel;");
                this.GenerateMethod(this.Object, ssb, info, x.Member!);
                return;
            }

            var firstFlag = true;
            foreach (var x in valueNexts)
            {
                var condition = firstFlag ? string.Format("if (key == 0x{0:X})", x.Key) : string.Format("else if (key == 0x{0:X})", x.Key);
                firstFlag = false;
                using (var c = ssb.ScopeBrace(condition))
                {
                    this.GenerateMethod(this.Object, ssb, info, x.Member!);
                }
            }

            // ssb.GotoSkipLabel();
        }

        private void GenerateNode(ScopingStringBuilder ssb, object? info, Node[] nexts)
        {// ReadOnlySpan<byte> utf8, ulong key (assgined)
            if (nexts.Length < 4)
            {// linear-search
                var valueNexts = nexts.Where(x => x.HasValue).ToArray();
                var childrenNexts = nexts.Where(x => x.HasChildren).ToArray();

                if (valueNexts.Length == 0)
                {
                    if (childrenNexts.Length == 0)
                    {
                        ssb.AppendLine("goto SkipLabel;");
                    }
                    else
                    {// valueNexts = 0, childrenNexts > 0
                        ssb.AppendLine("if (utf8.Length == 0) goto SkipLabel;");
                        this.GenerateChildrenNexts(ssb, info, childrenNexts);
                    }
                }
                else
                {
                    if (childrenNexts.Length == 0)
                    {// valueNexts > 0, childrenNexts = 0
                        ssb.AppendLine("if (utf8.Length != 0) goto SkipLabel;");
                        this.GenerateValueNexts(ssb, info, valueNexts);
                    }
                    else
                    {// valueNexts > 0, childrenNexts > 0
                        using (var scopeLeaf = ssb.ScopeBrace("if (utf8.Length == 0)"))
                        {// Should be leaf node.
                            this.GenerateValueNexts(ssb, info, valueNexts);
                        }

                        using (var scopeBranch = ssb.ScopeBrace("else"))
                        {// Should be branch node.
                            this.GenerateChildrenNexts(ssb, info, childrenNexts);
                        }
                    }
                }
            }
            else
            {// binary-search
                var midline = nexts.Length / 2;
                var mid = nexts[midline].Key;
                var left = nexts.Take(midline).ToArray();
                var right = nexts.Skip(midline).ToArray();

                using (var scopeLeft = ssb.ScopeBrace($"if (key < 0x{mid:X})"))
                {// left
                    this.GenerateNode(ssb, info, left);
                }

                using (var scopeRight = ssb.ScopeBrace("else"))
                {// right
                    this.GenerateNode(ssb, info, right);
                }
            }
        }

        public List<Node> NodeList { get; } = new();

        public Dictionary<byte[], Node> NameToNode { get; } = new(new ByteArrayComparer());

        private readonly Node root;

        internal class Node
        {
            public Node(Automata<TObj, TMember> automata, ulong key)
            {
                this.Automata = automata;
                this.Key = key;
            }

            public Automata<TObj, TMember> Automata { get; }

            public ulong Key { get; }

            public int Index { get; private set; } = -1;

            public TMember? Member { get; private set; }

            public byte[]? Utf8Name { get; private set; }

            public SortedDictionary<ulong, Node>? Nexts { get; private set; }

            public bool HasValue => this.Index != -1;

            public bool HasChildren => this.Nexts != null;

            public Node Add(ulong key)
            {
                if (this.Nexts != null && this.Nexts.TryGetValue(key, out var node))
                {// Found
                    return node;
                }
                else
                {// Not found
                    node = new Node(this.Automata, key);
                    if (this.Nexts == null)
                    {
                        this.Nexts = new();
                    }

                    this.Nexts.Add(key, node);
                    return node;
                }
            }

            public Node Add(ulong key, int index, TMember member, byte[] utf8)
            {
                var node = this.Add(key);
                node.Index = index;
                node.Member = member;
                node.Utf8Name = utf8;

                return node;
            }

            public override string ToString()
            {
                return Encoding.UTF8.GetString(this.Utf8Name);
            }
        }
    }

    internal class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public override bool Equals(byte[] first, byte[] second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }
            else if (ReferenceEquals(first, second))
            {
                return true;
            }
            else if (first.Length != second.Length)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }

        public override int GetHashCode(byte[] obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.Length;
        }
    }
}
