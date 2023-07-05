// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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

    public RoomBooking.GoshujinClass Bookings { get; set; } = new();

    public RoomClass2(int roomId)
    {
    }

    [ValueLinkObject]
    public partial record RoomBooking
    {
        [Link(Primary = true, Type = ChainType.Ordered)]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int UserId { get; set; }

        public RoomBooking()
        {
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
        }
    }
}
