// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink;

/// <summary>
/// An interface for comparing objects.<br/>
/// When added to an object, <see cref="IEquatableGoshujin{TGoshujin}"/> will be automatically implemented.
/// </summary>
/// <typeparam name="TObject">The type of the object.</typeparam>
public interface IEquatableObject<TObject>
{
    bool ObjectEquals(TObject other);
}
