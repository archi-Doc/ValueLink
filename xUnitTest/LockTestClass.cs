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

        public Task<Writer?> TryLock(TimeSpan timeout) => this.Instance.TryLock(timeout);

        public int Id => this.Instance.id;

        public string Name => this.Instance.name;
    }

    public class Writer : IDisposable
    {
        public Writer(IsolationTestClass instance)
        {
            this.original = instance;
        }

        public IsolationTestClass Instance => this.instance ?? this.original with { };

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
            var instance = this.Instance;
            var lockObject = instance.Goshujin?.LockObject;
            lockObject?.Enter();
            try
            {
                if (this.idChanged)
                {
                    instance.__gen_cl_identifier__001?.IdChain.Add(instance.id, instance);
                }

                // Replace instance
                instance.LinkedListLink.UnsafeChangeInstance(instance);
            }
            finally
            {
                lockObject?.Exit();
            }
        }

        public void Dispose() => this.original.writerSemaphore.Exit();
    }

    public Reader GetReader() => new Reader(this);

    public Writer Lock()
    {
        this.writerSemaphore.Enter();
        return new Writer(this);
    }

    public async Task<Writer?> TryLock(TimeSpan timeout)
    {
        var entered = await this.writerSemaphore.EnterAsync().ConfigureAwait(false);
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
    }
}
