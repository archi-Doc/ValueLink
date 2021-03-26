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
    {
        [Link(Type = LinkType.Ordered)]
        private int id;
    }
}
