// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject]
public partial class TestNotifyPropertyChanged : INotifyPropertyChanged
{
    [Link(AutoNotify = true)]
    private int Id;

    public event PropertyChangedEventHandler? PropertyChanged;
}

[ValueLinkObject(ExplicitPropertyChanged = "propertyChanged")]
public partial class TestNotifyPropertyChanged2 : INotifyPropertyChanged
{
    [Link(AutoNotify = true)]
    private int Id;

    public event PropertyChangedEventHandler? propertyChanged;

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            this.propertyChanged += value;
        }

        remove
        {
            this.propertyChanged -= value;
        }
    }
}

[ValueLinkObject]
public partial class TestNotifyPropertyChanged3
{
    [Link(AutoNotify = true)]
    private int Id;

    public void Test()
    {
        this.SetProperty(ref Id, 3);
    }

    public void SetProperty()
    {
        return;
    }

    public void SetProperty<T>(T a, T b, int name)
    {
        return;
    }

    public void SetProperty<U>(int a, U b, string name)
    {
        return;
    }
}

public class NotifyPropertyChangedTest
{
    [Fact]
    public void Test1()
    {
        var count = 0;

        var t1 = new TestNotifyPropertyChanged();
        t1.PropertyChanged += (s, e) => { if (e.PropertyName == "IdValue") count++; };
        t1.IdValue = 1;
        count.Is(1);

        var t2 = new TestNotifyPropertyChanged2();
        ((INotifyPropertyChanged)t2).PropertyChanged += (s, e) => { if (e.PropertyName == "IdValue") count++; };
        t2.IdValue = 1;
        count.Is(2);

        var t3 = new TestNotifyPropertyChanged3();
        t3.PropertyChanged += (s, e) => { if (e.PropertyName == "IdValue") count++; };
        t3.IdValue = 1;
        count.Is(3);
    }
}
