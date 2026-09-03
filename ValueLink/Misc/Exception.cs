// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// Reports an attempt to acquire a writer lock while holding its owner lock.
/// </summary>
public class LockOrderException : System.Exception
{
    private const string ExceptionMessage = "To prevent deadlock, it is not possible to acquire a Writer lock from within a Goshujin lock.";

    public LockOrderException()
        : base(ExceptionMessage)
    {
    }
}

/// <summary>
/// Reports a chain operation on an object belonging to a different owner.
/// </summary>
public class UnmatchedGoshujinException : System.Exception
{
    private const string ExceptionMessage = "This object is the property of different Goshujin-sama.";

    public UnmatchedGoshujinException()
        : base(ExceptionMessage)
    {
    }
}
