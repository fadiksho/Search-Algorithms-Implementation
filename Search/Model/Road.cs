using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Model
{
    public class Road
    {
        public Node HeadNode { get; set; }
        public Node SourceNode { get; set; }
        public List<Node> Branches { get; set; }
        public List<Node> PassedRoad { get; set; }
        public Queue<Node> PossibleRoad { get; set; }
    }
}