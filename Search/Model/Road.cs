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
        public List<Node> PassedRoad { get; set; } = new List<Node>();
        public Queue<Node> PossibleRoad { get; set; } = new Queue<Node>();
        public List<Road> mypaths { get; set; } = new List<Road>();
    }
}