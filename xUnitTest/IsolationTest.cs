// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using ValueLink;
using Xunit;

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

public interface IRepeatableObject<TGoshujin, TWriter>
    where TWriter : class
{
    TWriter? TryLock();

    void AddToGoshujinInternal(TGoshujin g);
}

public enum TryLockMode
{
    Get,
    Create,
    GetOrCreate,
}

public interface IRepeatableGoshujin<TKey, TObject, TWriter, TGoshujin>
    where TObject : IRepeatableObject<TGoshujin, TWriter>
    where TWriter : class
    where TGoshujin : IRepeatableGoshujin<TKey, TObject, TWriter, TGoshujin>
{
    public object SyncObject { get; }

    protected TObject? FindFirst(TKey key);

    protected TObject NewObject(TKey key);

    public TObject? TryGet(TKey key)
    {
        lock (this.SyncObject)
        {
            var x = this.FindFirst(key);
            return x;
        }
    }

    public TWriter? TryLock(TKey key, TryLockMode mode = TryLockMode.Get)
    {
        while (true)
        {
            TObject? x = default;
            lock (this.SyncObject)
            {
                x = this.FindFirst(key);
                if (x is null)
                {// No object
                    if (mode == TryLockMode.Get)
                    {// Get
                        return default;
                    }
                    else
                    {// Create, GetOrCreate
                        x = this.NewObject(key);
                        x.AddToGoshujinInternal((TGoshujin)this);
                    }
                }
                else
                {// Exists
                    if (mode == TryLockMode.Create)
                    {// Create
                        return default;
                    }

                    // Get, GetOrCreate
                }
            }

            if (x.TryLock() is { } writer)
            {
                return writer;
            }
        }
    }

    /*public TWriter? TryLock(TKey key)
    {
        while (true)
        {
            TObject? x = default;
            lock (this.SyncObject)
            {
                x = this.FindFirst(key);
                if (x is null)
                {
                    return default;
                }
            }

            if (x.TryLock() is { } writer)
            {
                return writer;
            }
        }
    }

    public TWriter? CreateAndLock(TKey key)
    {
        TObject x;
        lock (this.SyncObject)
        {
            if (this.FindFirst(key) is not null)
            {
                return null;
            }

            x = this.NewObject(key);
            x.AddToGoshujinInternal((TGoshujin)this);
        }

        return x.TryLock();
    }*/
}

[ValueLinkObject(Isolation = IsolationLevel.RepeatablePrimitive)]
public partial record RepeatableRoom : IRepeatableObject<RepeatableRoom.GoshujinClass, RepeatableRoom.WriterClass>
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

    public partial class GoshujinClass : IRepeatableGoshujin<int, RepeatableRoom, WriterClass, GoshujinClass>
    {
        RepeatableRoom? IRepeatableGoshujin<int, RepeatableRoom, WriterClass, GoshujinClass>.FindFirst(int key) => this.RoomIdChain.FindFirst(key);

        RepeatableRoom IRepeatableGoshujin<int, RepeatableRoom, WriterClass, GoshujinClass>.NewObject(int key)
        {
            var obj = new RepeatableRoom();
            obj.RoomId = key;
            return obj;
        }

        // public override object SyncObject => throw new NotImplementedException();

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

        public RepeatableRoom? TryGet(int roomId)
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
        }
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
    }

    void IRepeatableObject<GoshujinClass, WriterClass>.AddToGoshujinInternal(GoshujinClass g)
    {
        this.__gen_cl_identifier__001 = g;
        if (g != null)
        {
            g.RoomIdChain.Add(this.RoomId, this);
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
