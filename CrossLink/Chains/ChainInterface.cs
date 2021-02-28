// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1615 // Element return value should be documented

namespace CrossLink
{
    public interface ILink
    {
        bool IsLinked { get; }
    }
}
