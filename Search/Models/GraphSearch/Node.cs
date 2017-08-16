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
        public List<Node> ConnectedNodes { get; set; }
        public string NodeName { get; set; }
        public bool StartPoint { get; set; }
        public bool GoolPoint { get; set; }

        public Node GetCopy()
        {
            Node newNode = new Node();
            newNode.X = this.X;
            newNode.Y = this.Y;
            newNode.ConnectedNodes = new List<Node>(this.ConnectedNodes);
            newNode.NodeName = this.NodeName;
            newNode.StartPoint = this.StartPoint;
            newNode.GoolPoint = this.GoolPoint;
            return newNode;
        }

        public Node ShallowCopy()
        {
            return (Node)this.MemberwiseClone();
        }
        
    }
}
