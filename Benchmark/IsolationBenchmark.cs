using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using ValueLink;

namespace Benchmark;

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record IsolationClass
{
    public IsolationClass()
    {
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}

[Config(typeof(BenchmarkConfig))]
public class IsolationBenchmark
{
    private IsolationClass.GoshujinClass goshujin;

    public IsolationBenchmark()
    {
        this.goshujin = new();
        for (var i = 0; i < 10; i++)
        {
            using (var w = this.goshujin.TryLock(i, TryLockMode.Create))
            {
                if (w is not null)
                {
                    w.Name = i.ToString();
                    w.Commit();
                }
            }
        }
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public IsolationClass? TryGetObject()
    {
        var w = this.goshujin.TryGet(0);
        return w;
    }

    [Benchmark]
    public IsolationClass? TryLockObject()
    {
        using (var w = this.goshujin.TryLock(0))
        {
            return w?.Instance;
        }
    }

    [Benchmark]
    public IsolationClass? LockAndCommitObject()
    {
        using (var w = this.goshujin.TryLock(0))
        {
            if (w is not null)
            {
                w.Name = "test";
                return w.Commit();
            }
            else
            {
                return null;
            }
        }
    }

    [Benchmark]
    public async Task<IsolationClass?> LockAsyncAndCommitObject()
    {
        using (var w = await this.goshujin.TryLockAsync(0, 1000).ConfigureAwait(false))
        {
            if (w is not null)
            {
                w.Name = "test";
                return w.Commit();
            }
            else
            {
                return null;
            }
        }
    }
}
