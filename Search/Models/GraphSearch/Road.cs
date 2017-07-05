using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class Road
    {
        public Node HeadNode { get; set; }
        public List<Node> PassedRoad { get; set; } = new List<Node>();
        public Queue<Node> PossibleNodes { get; set; } = new Queue<Node>();
        public List<Road> PossibleRoads { get; set; } = new List<Road>();
    }
}