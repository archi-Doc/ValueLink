// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ValueLink.Generator;

namespace ValueLink.Generator;

public static class GeneratorHelper
{
    public static string ChainTypeToName(this ChainType type) => type switch
    {
        ChainType.None => string.Empty,
        ChainType.List => "ListChain",
        ChainType.LinkedList => "LinkedListChain",
        ChainType.StackList => "StackListChain",
        ChainType.QueueList => "QueueListChain",
        ChainType.Ordered => "OrderedChain",
        ChainType.ReverseOrdered => "OrderedChain",
        ChainType.Unordered => "UnorderedChain",
        ChainType.Observable => "ObservableChain",
        ChainType.SlidingList => "SlidingListChain",
        _ => string.Empty,
    };
}
