// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using ValueLink;
using Xunit;

namespace xUnitTest;

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

    [Link(Primary = true, Type = ChainType.Ordered)]
    public int Id { private set; get; }

    public int Max { private set; get; }

    public int Sold { private set; get; }

    public List<string> CustomerList { private set; get; } = new();
}

public class RepeatableCustomer
{
    public RepeatableCustomer(int name)
    {
        this.Name = $"Customer{name.ToString()}";
    }

    public string Name { private set; get; } = string.Empty;

    public List<int> ItemList { private set; get; } = new();

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
                    Thread.Sleep(10 + id);

                    w.Sold++;
                    w.CustomerList.Add(this.Name);
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
            g.TryGet(3, )
            item2.IsNotNull();
            item2!.State.IsValid().IsTrue();
            item.State.IsInvalid().IsTrue();
        }

        lock (g.SyncObject)
        {
            g.Count.Is(numberOfItems);
        }

        // Task
        var customers = new RepeatableCustomer[numberOfCustomers];
        Parallel.For(0, numberOfCustomers, i =>
        {
            customers[i] = new(i);
            customers[i].Task(g, numberOfItems, numberToBuy);
        });

        // Check
    }
}
