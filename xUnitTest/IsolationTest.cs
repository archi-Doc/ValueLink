// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ValueLink;
using Xunit;
using static xUnitTest.RepeatableRoom;

namespace xUnitTest;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial record SerializableRoom
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; set; }

    public SerializableRoom(int roomId)
    {
    }
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitive)]
public partial record RepeatableRoom
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; private set; }

    public Booking.GoshujinClass Bookings { get; private set; } = new();

    public bool TestFlag => true;

    public RepeatableRoom()
    {
    }

    public RepeatableRoom(int roomId)
    {
        this.RoomId = roomId;
    }

    public partial class GoshujinClass
    {
        /*public WriterClass? TryLock(int roomId)
        {
            while (true)
            {
                RepeatableRoom? x = null;
                lock (this.SyncObject)
                {
                    x = this.RoomIdChain.FindFirst(roomId);
                    if (x is null)
                    {
                        return null;
                    }
                }

                if (x.TryLock() is { } writer)
                {
                    return writer;
                }
            }
        }*/

        /*public RepeatableRoom? TryGet(int roomId)
        {
            lock (this.SyncObject)
            {
                var x = this.RoomIdChain.FindFirst(roomId);
                return x;
            }
        }

        public WriterClass? CreateAndLock(int roomId)
        {
            RepeatableRoom x;
            lock (this.SyncObject)
            {
                if (this.RoomIdChain.FindFirst(roomId) is not null)
                {
                    return null;
                }

                x = new();
                x.RoomId = roomId;
                x.AddToGoshujinInternal(this);
            }

            return x.TryLock();
        }

        public WriterClass GetOrCreate(int roomId)
        {
            while (true)
            {
                RepeatableRoom? x;
                lock (this.SyncObject)
                {
                    x = this.RoomIdChain.FindFirst(roomId);
                    if (x is null)
                    {
                        x = new(roomId);
                        x.RoomId = roomId;
                        x.AddToGoshujinInternal(this);
                    }
                }

                if (x.TryLock() is { } writer)
                {
                    return writer;
                }
            }
        }*/
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
    }
}

public class IsolationTest
{
    [Fact]
    public void TestSerializable()
    {// Serializable
        var g = new SerializableRoom.GoshujinClass();
        lock (g.SyncObject)
        {
            g.Add(new SerializableRoom(1));

            var room2 = new SerializableRoom(2);
            room2.Goshujin = g;
        }
    }

    [Fact]
    public void TestRepeatable()
    {// RepeatablePrimitive
        var g = new RepeatableRoom.GoshujinClass();
        var room1 = g.Add(new RepeatableRoom(1));

        var r = new RepeatableRoom(2);
        var room2 = g.Add(r);
        if (room2 is not null)
        {
            using (var w = room2.TryLock())
            {
                if (w is not null)
                {
                    w.Goshujin = g;
                    w.Commit();
                }
            }

            var booking = ((IRepeatableGoshujin<System.DateTime, Booking, GoshujinClass, WriterClass>)room2.Bookings).GetArray();
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
