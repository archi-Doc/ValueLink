// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial record LockTestClass
{
    [Link(Primary = true, Type = ChainType.Ordered)]
    private int id;

    public LockTestClass()
    {
    }
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record IsolationTestClass
{
    public readonly struct Reader
    {
        public Reader(IsolationTestClass instance)
        {
            this.Instance = instance;
        }

        public readonly IsolationTestClass Instance;

        public Writer Lock() => this.Instance.Lock();

        public Task<Writer?> TryLock(int millisecondsTimeout, CancellationToken cancellationToken) => this.Instance.TryLock(millisecondsTimeout, cancellationToken);

        public Task<Writer?> TryLock(int millisecondsTimeout) => this.Instance.TryLock(millisecondsTimeout);

        public int Id => this.Instance.id;

        public string Name => this.Instance.name;
    }

    public class Writer : IDisposable
    {
        public Writer(IsolationTestClass instance)
        {
            this.original = instance;
        }

        public IsolationTestClass Instance => this.instance ??= this.original with { };

        private IsolationTestClass original;
        private IsolationTestClass? instance;
        private bool idChanged;

        public int Id
        {
            get => this.Instance.id;
            set
            {
                this.Instance.id = value;
                this.idChanged = true;
            }
        }

        public string Name
        {
            get => this.Instance.name;
            set => this.Instance.name = value;
        }

        public void Commit()
        {
            var goshujin = this.original.__gen_cl_identifier__001;
            if (goshujin is not null)
            {
                using (goshujin.Lock())
                {
                    // Replace instance
                    goshujin.IdChain.UnsafeReplaceInstance(this.original, this.Instance);
                    // this.original.IdLink = default;

                    // Set chains
                    if (this.idChanged)
                    {
                        goshujin.IdChain.Add(this.Instance.id, this.Instance);
                    }

                    // Add journal
                }
            }
        }

        public void Rollback()
        {
            this.instance = null;
            this.idChanged = false;
        }

        public void Dispose() => this.original.writerSemaphore.Exit();
    }

    public Reader GetReader() => new Reader(this);

    public Writer Lock()
    {
        this.writerSemaphore.Enter();
        return new Writer(this);
    }

    public Task<Writer?> TryLock(int millisecondsTimeout) => this.TryLock(millisecondsTimeout, default);

    public async Task<Writer?> TryLock(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        var entered = await this.writerSemaphore.EnterAsync(millisecondsTimeout, cancellationToken).ConfigureAwait(false);
        if (entered)
        {
            return new Writer(this);
        }
        else
        {
            return null;
        }
    }

    private SemaphoreLock writerSemaphore = new();

    [Link(Type = ChainType.LinkedList, Name = "LinkedList")]
    public IsolationTestClass()
    {
    }

    [Link(Primary = true, Type = ChainType.Ordered)]
    private int id;

    private string name = string.Empty;
}

public class LockTest
{
    [Fact]
    public void Test1()
    {
        var tc = new LockTestClass();

        var tc2 = new IsolationTestClass();
        using (var writer = tc2.Lock())
        {
        }

        TestAsync(tc2).Wait();

        async Task TestAsync(IsolationTestClass t)
        {
            using (var writer = await t.TryLock(100))
            {
                if (writer is not null)
                {
                    writer.Id = 19;
                    writer.Commit();
                }
            }
        }
    }
}
