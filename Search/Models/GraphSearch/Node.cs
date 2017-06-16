using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public List<Node> ConnectedNode { get; set; }
        public string NodeName { get; set; }
        public bool StartPoint { get; set; }
        public bool GoolPoint { get; set; }
    }
}
