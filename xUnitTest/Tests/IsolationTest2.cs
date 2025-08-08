// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest;

[TinyhandObject(ExplicitKeyOnly = true, Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableItem
{
    public RepeatableItem()
    {
    }

    public RepeatableItem(int id, int max)
    {
        this.Id = id;
        this.Max = max;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public int Id { private set; get; }

    public int Max { private set; get; }

    public int Sold { private set; get; }

    public List<int> CustomerList { private set; get; } = new();

    public int[] NumberArray { set; get; } = Array.Empty<int>();

    [Key(1)]
    [MaxLength(100)]
    public partial string Name { get; private set; } = string.Empty;
}

public class RepeatableCustomer
{
    public RepeatableCustomer(int id)
    {
        this.Id = id;
        this.Name = $"Customer{this.Id.ToString()}";
    }

    public int Id { get; private set; }

    public string Name { private set; get; } = string.Empty;

    public List<int> ItemList { private set; get; } = new();

    public int[] NumberArray { set; get; } = Array.Empty<int>();

    public void Task(RepeatableItem.GoshujinClass g, int numberOfItems, int numberToBuy)
    {
        while (numberToBuy > 0)
        {
            var id = Random.Shared.Next(0, numberOfItems);
            using (var w = g.TryLock(id))
            {
                if (w is null)
                {
                    continue;
                }

                if (w.Sold < w.Max)
                {
                    // Thread.Sleep(10 + id);

                    w.Sold++;
                    w.CustomerList.Add(this.Id);
                    w.Commit();

                    numberToBuy--;
                    this.ItemList.Add(id);
                }
            }
        }
    }
}

public class IsolationTest2
{
    [Fact]
    public void Test1()
    {
        this.Test2(10, 20, 1, 10);
        this.Test2(20, 20, 10, 10);
        this.Test2(100, 100, 100, 80);
    }

    private void Test2(int numberOfItems, int max, int numberOfCustomers, int numberToBuy)
    {
        if ((numberOfItems * max) < (numberOfCustomers * numberToBuy))
        {
            return;
        }

        // Prepare
        var g = new RepeatableItem.GoshujinClass();
        for (var i = 0; i < numberOfItems; i++)
        {
            var item = new RepeatableItem(i, max);
            var item2 = g.Add(item);
            item2.IsNotNull();
            item2!.State.IsValid().IsTrue();
            item.State.IsInvalid().IsTrue();
        }

        using (g.LockObject.EnterScope())
        {
            g.Count.Is(numberOfItems);
            ((IRepeatableSemaphore)g).State.Is(GoshujinState.Valid);
            ((IRepeatableSemaphore)g).SemaphoreCount.Is(0);
        }

        // Task
        var customers = new RepeatableCustomer[numberOfCustomers];
        Parallel.For(0, numberOfCustomers, i =>
        {
            customers[i] = new(i);
            customers[i].Task(g, numberOfItems, numberToBuy);
        });

        // Check
        foreach (var c in customers)
        {
            c.ItemList.Count.Is(numberToBuy);
            c.NumberArray = new int[numberOfItems];
            var n = 0;
            foreach (var i in c.ItemList)
            {
                c.NumberArray[i]++;
                n++;
            }

            n.Is(numberToBuy);
        }

        var items = g.GetArray();
        items.Count().Is(numberOfItems);
        foreach (var item in items)
        {
            item.Sold.Is(item.CustomerList.Count);
            item.NumberArray = new int[numberOfCustomers];

            var n = 0;
            foreach (var i in item.CustomerList)
            {
                item.NumberArray[i]++;
                n++;
            }

            n.Is(item.Sold);

            for (var i = 0; i < numberOfCustomers; i++)
            {
                item.NumberArray[i].Is(customers[i].NumberArray[item.Id]);
            }
        }
    }
}
