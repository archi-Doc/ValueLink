// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossLink;

namespace ConsoleApp1
{
    [CrossLinkObject]
    public partial class TinyClass
    {// Tiny class to demonstrate how CrossLink works.
        [Link(Type = LinkType.Ordered)]
        private int id;
    }
}
