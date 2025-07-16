// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record class NoDefaultConstructorClass
{
    public NoDefaultConstructorClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public int Id { get; private set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial record SerializableRoom
{
    [Link(Primary = true, Type = ChainType.Ordered, AddValue = false)]
    public int RoomId { get; set; }

    public SerializableRoom(int roomId)
    {
    }
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record RepeatableRoom
{
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, AddValue = false)]
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

    [ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
    public partial record Booking
    {
        [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
        public DateTime StartTime { get; private set; }

        [Link(Unique = true, Type = ChainType.Unordered)]
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
        using (g.LockObject.EnterScope())
        {
            g.Add(new SerializableRoom(1));

            var room2 = new SerializableRoom(2);
            room2.Goshujin = g;
        }
    }

    [Fact]
    public void TestRepeatable()
    {// RepeatableRead
        var g = new RepeatableRoom.GoshujinClass();
        var room1 = g.Add(new RepeatableRoom(1));
        ((IGoshujinSemaphore)g).State.Is(GoshujinState.Valid);
        ((IGoshujinSemaphore)g).SemaphoreCount.Is(0);

        using (var a = g.TryLock(0))
        {
            ((IGoshujinSemaphore)g).State.Is(GoshujinState.Valid);
            ((IGoshujinSemaphore)g).SemaphoreCount.Is(0);
        }

        var b = g.TryGet(1);
        using (var a = g.TryLock(1))
        {
            if (a is not null)
            {
                ((IGoshujinSemaphore)g).State.Is(GoshujinState.Valid);
                ((IGoshujinSemaphore)g).SemaphoreCount.Is(1);

                a.RoomId = 100;
                a.Commit();

                ((IGoshujinSemaphore)g).SemaphoreCount.Is(1);
            }
        }

        ((IGoshujinSemaphore)g).SemaphoreCount.Is(0);

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

            var booking = room2.Bookings.GetArray();
            if (booking.Length > 0)
            {
                var c = booking[0];
                using (var w = c.TryLock())
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

    [Fact]
    public void TestNew()
    {
        var g1 = new RepeatableRoom.GoshujinClass();
        var r1 = g1.Add(new RepeatableRoom(1));
        r1.IsNotNull();

        using (var r = g1.TryLock(1))
        {
            r.IsNotNull();
        }

        using (var r = g1.TryLock(1))
        {// Empty commit
            r!.Commit().Is(r1);
        }

        using (var r = g1.TryLock(1, TryLockMode.GetOrCreate))
        {
            r.IsNotNull();
        }

        using (var r = g1.TryLock(1, TryLockMode.Create))
        {
            r.IsNull();
        }

        var rr = new RepeatableRoom(2);
        var r2 = g1.Add(rr)!;
        r2.IsNotNull();

        // Id 2 -> 1
        using (var w = r2.TryLock())
        {
            w.IsNotNull();
            if (w is not null)
            {

                w.RoomId = 1;
                rr = w.Commit();
                rr.IsNull();
            }
        }

        // G1 -> G2
        var g2 = new RepeatableRoom.GoshujinClass();
        rr = g2.Add(r1!);
        rr.IsNotNull();
        g1.Count.Is(1);
        g2.Count.Is(1);

        r1 = g1.Add(new RepeatableRoom(1));
        g1.Count.Is(2);
        using (var w = g2.TryLock(1))
        {
            w.IsNotNull();
            if (w is not null)
            {
                w.Goshujin = g1;
                rr = w.Commit();
                rr.IsNull();
            }
        }

        using (var w = g2.TryLock(10, TryLockMode.Create))
        {
            w!.Commit();
        }

        rr = g2.TryGet(10);
        rr.IsNotNull();

        using (var w = g2.TryLock(11, TryLockMode.Create))
        {
        }

        rr = g2.TryGet(11);
        rr.IsNull();

        using (var w = g2.TryLock(10, TryLockMode.Get)!)
        {
            w.RemoveAndErase();
            w.Commit();
        }

        rr = g2.TryGet(10);
        rr.IsNull();

        using (var w = g2.TryLock(10, TryLockMode.Create)!)
        {
            w.RemoveAndErase();
            w.Commit();
        }

        rr = g2.TryGet(10);
        rr.IsNull();
    }
}
