// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace ValueLink.Integrality;

public interface IIntegralityInternal
{
    int MaxItems { get; }

    bool RemoveIfItemNotFound { get; }

    int MaxMemoryLength { get; }

    int MaxIntegrationCount { get; }

    public ulong TargetHash { get; }

    Dictionary<TKey, ulong> GetKeyHashCache<TKey>(bool clear)
        where TKey : struct;

    bool ValidateInternal(object goshujin, object newItem, object? oldItem);
}
