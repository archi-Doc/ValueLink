using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Tinyhand;
using ValueLink;

namespace Benchmark;

[TinyhandObject]
public partial record CloneClass
{
    public CloneClass()
    {
    }

    public CloneClass(int id, string name,
        int id2, string name2,
        int id3, string name3,
        int id4, string name4,
        int id5, string name5)
    {
        this.Id = id;
        this.Name = name;
        this.Id2 = id2;
        this.Name2 = name2;
        this.Id3 = id3;
        this.Name3 = name3;
        this.Id4 = id4;
        this.Name4 = name4;
        this.Id5 = id5;
        this.Name5 = name5;

        this.IntArray = [0, 100, 10000, 10, 2, 3, 4, 5, 6,];
    }

    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; } = default!;

    [Key(2)]
    public int Id2 { get; set; }

    [Key(3)]
    public string Name2 { get; set; } = default!;

    [Key(4)]
    public int Id3 { get; set; }

    [Key(5)]
    public string Name3 { get; set; } = default!;

    [Key(6)]
    public int Id4 { get; set; }

    [Key(7)]
    public string Name4 { get; set; } = default!;

    [Key(8)]
    public int Id5 { get; set; }

    [Key(9)]
    public string Name5 { get; set; } = default!;

    [Key(10)]
    public int[] IntArray { get; set; } = Array.Empty<int>();
}

[Config(typeof(BenchmarkConfig))]
public class CloneClassBenchmark
{
    private CloneClass class1;

    public CloneClassBenchmark()
    {
        this.class1 = new(1, "1", 10, "10", 100, "1000", 10000, "10000", 1000000, "1000000");

        var c1 = TinyhandSerializer.Clone(this.class1);
        var c2 = this.class1 with { };
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    /*[Benchmark]
    public CloneClass CloneTinyhand()
        => TinyhandSerializer.Clone(this.class1);*/

    [Benchmark]
    public CloneClass CloneTinyhandObject()
        => TinyhandSerializer.CloneObject(this.class1)!;

    [Benchmark]
    public CloneClass CloneRecord()
        => this.class1 with { };
}
