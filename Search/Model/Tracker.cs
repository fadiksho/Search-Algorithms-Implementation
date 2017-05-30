using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Model
{
    public class Tracker
    {
        public Node Node { get; set; }

        public Stack<Node> PossibleRoad { get; set; } = new Stack<Node>();

        public List<Node> PassedRoad { get; set; } = new List<Node>();

    }
}
