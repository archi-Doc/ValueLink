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
public partial record RoomClass2
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; set; }

    public Booking.GoshujinClass Bookings { get; set; } = new();

    public RoomClass2(int roomId)
    {
        this.RoomId = roomId;
    }

    [ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitives)]
    public partial record Booking
    {

        [Link(Primary = true, Type = ChainType.Ordered)]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int UserId { get; set; }

        public Booking()
        {
        }
    }
}

public static class IsolationExtension
{
    public static RoomClass2.Booking[] GetArray(this RoomClass2.Booking.GoshujinClass g)
    {
        RoomClass2.Booking[] array;
        lock (g.SyncObject)
        {
            array = g.ToArray();
        }

        return array;
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
        var g = new RoomClass2.GoshujinClass();
        lock (g.SyncObject)
        {
            g.Add(new RoomClass2(1));

            var room2 = new RoomClass2(2);
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

                }
            }
        }
    }
}
