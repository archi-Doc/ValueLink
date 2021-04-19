// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        public static string LinkTypeToChain(this LinkType type) => type switch
        {
            LinkType.None => string.Empty,
            LinkType.List => "ListChain",
            LinkType.LinkedList => "LinkedListChain",
            LinkType.StackList => "StackListChain",
            LinkType.QueueList => "QueueListChain",
            LinkType.Ordered => "OrderedChain",
            LinkType.ReverseOrdered => "OrderedChain",
            LinkType.Unordered => "UnorderedChain",
            _ => string.Empty,
        };
    }
}
