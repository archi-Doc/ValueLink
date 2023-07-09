// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using ValueLink;
using Xunit;

namespace xUnitTest;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial record RoomClass
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; set; }

    public RoomClass(int roomId)
    {
    }
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitives)]
public partial record Room
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; private set; }

    public Booking.GoshujinClass Bookings { get; private set; } = new();

    public Room(int roomId)
    {
        this.RoomId = roomId;
    }

    [ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitives)]
    public partial record Booking
    {
        public partial class GoshujinClass
        {
            public Reader[] GetArray()
            {
                Reader[] array;
                lock (this.SyncObject)
                {
                    array = this.Select(x => x.GetReader()).ToArray();
                }

                return array;
            }
        }

        [Link(Primary = true, Type = ChainType.Ordered)]
        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public int UserId { get; private set; }

        [Link(Type = ChainType.Ordered)]
        private string name = string.Empty;

        public Booking()
        {
        }

        public partial record Writer2 : Booking, IDisposable
        {
            public Writer2(Booking parent)
                : base(parent)
            {

            }

            public string Name
            {
                get => this.name;
                set { this.name = value; }
            }

            public new int UserId
            {
                get => base.UserId;
                set { base.UserId = value; }
            }

            public void Test()
            {
                this.Name = string.Empty;
            }

            public void Dispose()
            {
            }
        }
    }
}

public class IsolationTest
{
    [Fact]
    public void Test1()
    {// Serializable
        var g = new RoomClass.GoshujinClass();
        lock (g.SyncObject)
        {
            g.Add(new RoomClass(1));

            var room2 = new RoomClass(2);
            room2.Goshujin = g;
        }
    }

    [Fact]
    public void Test2()
    {// RepeatablePrimitive
        var g = new Room.GoshujinClass();
        lock (g.SyncObject)
        {
            g.Add(new Room(1));

            var room2 = new Room(2);
            room2.Goshujin = g;
            using (var w = room2.Lock())
            {
                w.Goshujin = g;
                w.Commit();
            }

            var booking = room2.Bookings.GetArray();
            if (booking.Length > 0)
            {
                var b = booking[0];
                using (var w = b.Lock())
                {
                    w.Name = "test";
                    w.Commit();
                }
            }
        }
    }
}
