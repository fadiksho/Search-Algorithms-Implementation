using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class Path
    {
        public Node Node { get; set; }
        public List<Node> Paths { get; set; } = new List<Node>();
        public int ChildLength { get; set; }
        public bool CurrectPath { get; set; }
    }
}
