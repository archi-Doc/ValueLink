// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.ComponentModel;
using CrossLink;
using Xunit;

namespace xUnitTest
{
    [CrossLinkObject]
    public partial class TestNotifyPropertyChanged : INotifyPropertyChanged
    {
        [Link(AutoNotify = true)]
        private int id;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [CrossLinkObject(ExplicitPropertyChanged = "propertyChanged")]
    public partial class TestNotifyPropertyChanged2 : INotifyPropertyChanged
    {
        [Link(AutoNotify = true)]
        private int id;

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

    [CrossLinkObject]
    public partial class TestNotifyPropertyChanged3
    {
        [Link(AutoNotify = true)]
        private int id;

        public void Test()
        {
            this.SetProperty(ref id, 3);
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
            t1.PropertyChanged += (s, e) => { if (e.PropertyName == "Id") count++; };
            t1.Id = 1;
            count.Is(1);

            var t2 = new TestNotifyPropertyChanged2();
            ((INotifyPropertyChanged)t2).PropertyChanged += (s, e) => { if (e.PropertyName == "Id") count++; };
            t2.Id = 1;
            count.Is(2);

            var t3 = new TestNotifyPropertyChanged3();
            t3.PropertyChanged += (s, e) => { if (e.PropertyName == "Id") count++; };
            t3.Id = 1;
            count.Is(3);
        }
    }
}
