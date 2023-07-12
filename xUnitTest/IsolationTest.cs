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

[ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitive)]
public partial record Room
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; private set; }

    public Booking.GoshujinClass Bookings { get; private set; } = new();

    public bool TestFlag => true;

    public Room(int roomId)
    {
        this.RoomId = roomId;
    }

    [ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitive)]
    public partial record Booking
    {
        [Link(Primary = true, Type = ChainType.Ordered)]
        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public int UserId { get; private set; }

        [Link(Type = ChainType.Ordered)]
        private string name = string.Empty;

        public Booking()
        {
        }

        public partial class GoshujinClass
        {
            public Booking[] GetArray()
            {
                Booking[] array;
                lock (this.SyncObject)
                {
                    array = this.ToArray();
                }

                return array;
            }
        }

        public partial record WriterClass2 : Booking, IDisposable
        {
            public WriterClass2(Booking parent)
                : base(parent)
            {
            }

            public new GoshujinClass? Goshujin { get; set; }

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
            g.Add(room2);
            using (var w = room2.TryLock())
            {
                if (w is not null)
                {
                    w.Goshujin = g;
                    w.Commit();
                }
            }

            var booking = room2.Bookings.GetArray();
            if (booking.Length > 0)
            {
                var b = booking[0];
                using (var w = b.TryLock())
                {
                    if (w is not null)
                    {
                        w.Name = "test";
                        w.Commit();
                    }
                }
            }
        }
    }
}
