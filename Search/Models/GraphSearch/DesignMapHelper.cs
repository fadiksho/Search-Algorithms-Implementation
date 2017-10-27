using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class DesignMapHelper
    {
        private bool stop;
        public List<string> Letters { get; set; } = new List<string>();
        public List<string> RemovedLetters { get; set; } = new List<string>();
        public List<Line> Lines { get; set; } = new List<Line>();
        public HashSet<Node> BusyLocations { get; set; } = new HashSet<Node>();

        HashSet<Node> passedNodes = new HashSet<Node>();
        
        public DesignMapHelper()
        {
            Letters = AlphabeticallyLetters;
        }

        public void RemoveLine(Node node)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Point2 == node || Lines[i].Point1 == node)
                {
                    Lines.RemoveAt(i);
                    i--;
                }
            }
        }

        public bool HaveSameSlop(Node node1, Node node2, Node node3)
        {
            //(y1 - y2) * (x1 - x3) == (y1 - y3) * (x1 - x2)
            return (node1.Y - node2.Y) * (node1.X - node3.X) == (node1.Y - node3.Y) * (node1.X - node2.X);
        }

        public bool CheckIfTwoLineIntersect(Line line1, Line line2)
        {
            float m1 = line1.M;
            float m2 = line2.M;

            if (m1 == m2 || double.IsNaN(m1) && double.IsNaN(m2))
                return false; // Parallel segments
            var b1 = line1.B;
            var b2 = line2.B;


            float X, Y;
            if (float.IsInfinity(m1))
            {
                X = line1.Point1.X;
                Y = m2 * X + b2;

            }

            else if (float.IsInfinity(m2))
            {
                X = line2.Point1.X;
                Y = m1 * X + b1;
            }
            else
            {
                X = (b2 - b1) / (m1 - m2);
                Y = m1 * X + b1;
            }

            int minX1 = Math.Min(line1.Point1.X, line1.Point2.X);
            int minX2 = Math.Min(line2.Point1.X, line2.Point2.X);
            int minX = Math.Min(minX1, minX2);

            int maxX1 = Math.Max(line1.Point1.X, line1.Point2.X);
            int maxX2 = Math.Max(line2.Point1.X, line2.Point2.X);
            int maxX = Math.Max(maxX1, maxX2);

            int minY1 = Math.Min(line1.Point1.Y, line1.Point2.Y);
            int minY2 = Math.Min(line2.Point1.Y, line2.Point2.Y);
            int minY = Math.Min(minY1, minY2);

            int maxY1 = Math.Max(line1.Point2.Y, line1.Point2.Y);
            int maxY2 = Math.Max(line2.Point2.Y, line2.Point2.Y);
            int maxY = Math.Max(maxY1, maxY2);

            if (X > minX && X < maxX && Y > minY && Y < maxY)
            {
                return true;
            }
            return false;
        }

        public Node GetNodeIfExistOnMap(int x, int y, List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.X == x && node.Y == y)
                    return node;
            }
            return null;
        }

        public List<Node> GetNodesOnLine(Line line, List<Node> nodes, 
            float[] xAxis, float[] yAxis, float circleRadius)
        {
            var onLine = new List<Node>();
            foreach (var node in nodes)
            {
                if (line.IfPointOnTheLine(xAxis[node.X], yAxis[node.Y], circleRadius / 2))
                {
                    onLine.Add(node);
                }
            }
            onLine = onLine.OrderBy(s => s.X).OrderBy(s => s.Y).ToList();
            return onLine;
        }

        public bool DepthFirstSearchWithFiltering(Node sNode, Node gNode)
        {
            if (sNode == gNode)
                return true;
            else
            {
                passedNodes.Add(sNode);
                foreach (var node in sNode.ConnectedNodes)
                {
                    var passed = false;
                    foreach (var passedNode in passedNodes)
                    {
                        if (passedNode == node)
                        {
                            passed = true;
                            break;
                        }
                    }
                    if (!passed)
                        stop = DepthFirstSearchWithFiltering(node, gNode);
                    if (stop)
                        break;
                }
            }
            return stop;
        }

        public void FillAvailableNodeToConnect(Node sNode)
        {
            BusyLocations.Clear();
            if (sNode != null)
            {
                var connectedLines = Lines.Where(l => l.Point1 == sNode || l.Point2 == sNode).ToList();
                foreach (var line in Lines)
                {
                    // Check if the line is connected
                    var slop = line.M;
                    foreach (var subLine in connectedLines)
                    {
                        var subLineSlop = subLine.M;
                        if (Math.Round(slop, 3) == Math.Round(subLineSlop, 3) || float.IsNaN(slop) && float.IsNaN(subLineSlop))
                        {
                            if (HaveSameSlop(sNode, line.Point1, line.Point2))
                            {
                                // Check if its continues
                                var isContinues = DepthFirstSearchWithFiltering(sNode, line.Point1);
                                if (isContinues)
                                {
                                    BusyLocations.Add(line.Point1);
                                    BusyLocations.Add(line.Point2);
                                }
                                passedNodes.Clear();
                                stop = false;
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        public List<Map> GetDefualtMaps()
        {
            Map map1 = new Map()
            {
                Name = "Small",
                IsDeleteEnabled = false,
                Nodes = InitializeMap1()

            };
            Map map2 = new Map()
            {
                Name = "Complex",
                IsDeleteEnabled = false,
                Nodes = InitializeMap2()
            };

            return new List<Map>() { map1, map2 };
        }

        public void ConnectTwoNode(Node selectedNode, Node pSelectedNode)
        {
            bool isConnected = false;
            foreach (var node in pSelectedNode.ConnectedNodes)
            {
                if (node == selectedNode)
                {
                    isConnected = true;
                    break;
                }
            }
            if (!isConnected)
            {
                Line line = new Line()
                {
                    Point1 = selectedNode,
                    Point2 = pSelectedNode
                };
                selectedNode.ConnectedNodes.Add(pSelectedNode);
                pSelectedNode.ConnectedNodes.Add(selectedNode);
                Lines.Add(line);
            }
        }

        public bool ConnectTwoNodeAndBetween(Node selectedNode, Node pSelectedNode,
            List<Node> nodes, float[] xAxis, float[] yAxis, float circleRadius)
        {
            bool succefullConnected = false;
            if (IsFreeLocation(selectedNode))
            {
                Line line = new Line()
                {
                    Point1 = selectedNode,
                    Point2 = pSelectedNode
                };
                var linkedNodes = GetNodesOnLine(line, nodes, xAxis, yAxis, circleRadius);
                for (int i = 1; i < linkedNodes.Count; i++)
                {
                    ConnectTwoNode(linkedNodes[i - 1], linkedNodes[i]);
                    succefullConnected = true;
                }
                return succefullConnected;
            }
            return succefullConnected;
        }

        public void DissConnectTwoNode(Line line)
        {
            line.Point1.ConnectedNodes.Remove(line.Point2);
            line.Point2.ConnectedNodes.Remove(line.Point1);
            Lines.Remove(line);
        }

        public void AddToRemovedList(string letter)
        {
            RemovedLetters.Add(letter);
            RemovedLetters.Sort();
        }

        private List<string> AlphabeticallyLetters
        {
            get
            {
                var query = Enumerable.Range(0, 26)
                                .Select(i => ((char)('A' + i)).ToString()).ToList();
                return query;
            }
        }

        private bool IsFreeLocation(Node node)
        {
            bool free = true;
            foreach (var busyNode in BusyLocations)
            {
                if (busyNode == node)
                {
                    free = false;
                    break;
                }
            }
            return free;
        }

        private List<Node> InitializeMap1()
        {
            Node S = new Node() { NodeName = "S", X = 1, Y = 3 };
            Node A = new Node() { NodeName = "A", X = 3, Y = 2 };
            Node B = new Node() { NodeName = "B", X = 4, Y = 3 };
            Node C = new Node() { NodeName = "C", X = 5, Y = 1 };
            Node D = new Node() { NodeName = "D", X = 6, Y = 3 };
            Node E = new Node() { NodeName = "E", X = 7, Y = 1 };
            Node G = new Node() { NodeName = "G", X = 7, Y = 2 };

            S.ConnectedNodes = new List<Node>() { A, B };
            A.ConnectedNodes = new List<Node>() { S, C, B };
            B.ConnectedNodes = new List<Node>() { S, A, D };
            C.ConnectedNodes = new List<Node>() { A, E, D };
            D.ConnectedNodes = new List<Node>() { B, C, G };
            E.ConnectedNodes = new List<Node>() { C };
            G.ConnectedNodes = new List<Node>() { D };

            var nodee = new List<Node>() { S, A, B, C, D, E, G };

            return nodee;
        }

        private List<Node> InitializeMap2()
        {
            var nodes = new List<Node>();
            Node A = new Node() { NodeName = "A", X = 0, Y = 2 };
            Node B = new Node() { NodeName = "B", X = 1, Y = 1 };
            Node C = new Node() { NodeName = "C", X = 1, Y = 3 };
            Node D = new Node() { NodeName = "D", X = 3, Y = 0 };
            Node E = new Node() { NodeName = "E", X = 2, Y = 2 };
            Node F = new Node() { NodeName = "F", X = 2, Y = 1 };
            Node G = new Node() { NodeName = "G", X = 4, Y = 1 };
            Node H = new Node() { NodeName = "H", X = 3, Y = 4 };
            Node I = new Node() { NodeName = "I", X = 6, Y = 0 };
            Node J = new Node() { NodeName = "J", X = 5, Y = 2 };
            Node K = new Node() { NodeName = "K", X = 4, Y = 3 };
            Node L = new Node() { NodeName = "L", X = 7, Y = 1 };
            Node M = new Node() { NodeName = "M", X = 8, Y = 2 };
            Node N = new Node() { NodeName = "N", X = 6, Y = 3 };

            A.ConnectedNodes = new List<Node>() { B, C };
            B.ConnectedNodes = new List<Node>() { A, D, F };
            C.ConnectedNodes = new List<Node>() { A, E, H };
            D.ConnectedNodes = new List<Node>() { B, I, G };
            E.ConnectedNodes = new List<Node>() { F, C, K };
            F.ConnectedNodes = new List<Node>() { B, E, G };
            G.ConnectedNodes = new List<Node>() { D, F, J };
            H.ConnectedNodes = new List<Node>() { C, K, N };
            I.ConnectedNodes = new List<Node>() { D, L };
            J.ConnectedNodes = new List<Node>() { G };
            K.ConnectedNodes = new List<Node>() { E, H };
            L.ConnectedNodes = new List<Node>() { I, M };
            M.ConnectedNodes = new List<Node>() { L, N };
            N.ConnectedNodes = new List<Node>() { H, M };

            nodes = new List<Node>() { A, B, C, D, E, F, G, H, I, J, K, L, M, N };


            return nodes;
        }
    }
}
