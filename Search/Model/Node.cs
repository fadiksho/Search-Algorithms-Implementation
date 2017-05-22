using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Model
{
    public class Node
    {
        public float X { get; set; }
        public float Y { get; set; }
        public List<Node> ConnectedNode { get; set; }
        public string NodeName { get; set; }
        public bool StartPoint { get; set; }
        public bool GoolPoint { get; set; }
    }
}
