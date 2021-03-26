``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]    : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  MediumRun : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                        Method | Length |       Mean |    Error |   StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------------ |------- |-----------:|---------:|---------:|-------:|-------:|------:|----------:|
|    NewAndAdd_SortedDictionary |    100 | 7,209.8 ns | 53.98 ns | 77.42 ns | 1.9379 |      - |     - |    8112 B |
|           NewAndAdd_CrossLink |    100 | 4,942.6 ns | 12.28 ns | 17.99 ns | 2.7084 | 0.0076 |     - |   11328 B |
| RemoveAndAdd_SortedDictionary |    100 | 1,491.1 ns | 13.01 ns | 18.24 ns | 0.1335 |      - |     - |     560 B |
|        RemoveAndAdd_CrossLink |    100 |   524.1 ns |  3.76 ns |  5.63 ns | 0.1717 |      - |     - |     720 B |
