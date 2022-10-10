// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using ValueLink;

namespace xUnitTest;

public partial class ObjectCache<TKey, TObject> : IDisposable
    where TKey : IEquatable<TKey>
{
    [TinyhandObject]
    public partial class TestClass
    {
    }

    [ValueLinkObject]
    private partial class Item
    {
        [Link(Primary = true, Name = "Queue", Type = ChainType.QueueList)]
        public Item(TKey key, TObject obj)
        {
            this.Key = key;
            this.Object = obj;
        }

#pragma warning disable SA1401 // Fields should be private
        [Link(Type = ChainType.Unordered)]
        internal TKey Key;
        internal TObject Object;
#pragma warning restore SA1401 // Fields should be private
    }

    public ObjectCache(uint cacheSize)
    {
        this.CacheSize = cacheSize;
    }

    public TObject? TryGet(TKey key)
    {
        Item? item;
        lock (this.goshujin)
        {
            this.goshujin.KeyChain.TryGetValue(key, out item);
            this.goshujin.Remove(item);
        }

        return item == null ? default : item.Object;
    }

    public bool Cache(TKey key, TObject obj)
    {
        lock (this.goshujin)
        {
            while (this.goshujin.QueueChain.Count > this.CacheSize)
            {
                var item = this.goshujin.QueueChain.Dequeue();
                this.DisposeItem(item);
            }

            if (!this.goshujin.KeyChain.ContainsKey(key))
            {
                var item = new Item(key, obj);
                item.Goshujin = this.goshujin;
                return true;
            }
        }

        return false;
    }

    public uint CacheSize { get; }

    private void DisposeItem(Item item)
    {
        item.Goshujin = null;
        if (item.Object is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private Item.GoshujinClass goshujin = new();

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectCache{TObject, TKey}"/> class.
    /// </summary>
    ~ObjectCache()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                lock (this.goshujin)
                {
                    while (this.goshujin.QueueChain.TryDequeue(out var item))
                    {
                        this.DisposeItem(item);
                    }
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
