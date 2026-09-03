// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral;

public enum VisceralGenericsKind
{
    NotSet = 0,
    NotGeneric = 1,
    // UnboundGeneric = 2, // Currently not supported.
    OpenGeneric = 3,
    ClosedGeneric = 4,
}
