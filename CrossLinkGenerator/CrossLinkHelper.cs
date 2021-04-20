﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CrossLink.Generator;

namespace CrossLink.Generator
{
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
            _ => string.Empty,
        };
    }
}
