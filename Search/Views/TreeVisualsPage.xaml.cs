using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Models.GraphSearch;
using Search.Models.TreeVisuals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Search.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TreeVisualsPage : Page
    {
        List<List<Node>> MySearchPath = new List<List<Node>>();

        float[] xAxis, yAxis;
        List<Layer> layers = new List<Layer>();
        List<List<Node>> Tree = new List<List<Node>>();
        int midleRate = 0;
        public List<Node> Nodes { get; private set; }

        float canvasWidth, canvasHeight, canvasWidthMargin, canvasHeightMargin,
            blockWidth, blockHeight, circleRadius;

        CanvasTextFormat textformat = new CanvasTextFormat()
        {
            WordWrapping = CanvasWordWrapping.NoWrap,
            FontFamily = "Arial",
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };
        private float lThickness;

        public TreeVisualsPage()
        {
            this.InitializeComponent();
        }
        
        private void Mycanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            drawCoordinateGeometry(args, xAxis, yAxis, Colors.Black, lThickness / 2);
            foreach (var Path in Tree)
            {
                foreach (var Node in Path)
                {
                    foreach (var childNode in Node.ConnectedNodes)
                    {
                        if(Node != childNode)
                            args.DrawingSession.DrawLine(xAxis[Node.X], yAxis[Node.Y], xAxis[childNode.X], yAxis[childNode.Y], Colors.Gray, lThickness);
                    }
                }
            }
            foreach (var Path in Tree)
            {
                foreach (var Node in Path)
                {
                    args.DrawingSession.FillCircle(xAxis[Node.X], yAxis[Node.Y], circleRadius, Colors.Black);
                    args.DrawingSession.DrawText(Node.NodeName, xAxis[Node.X], yAxis[Node.Y], Colors.White, textformat);
                    args.DrawingSession.DrawText($"({Node.X},{Node.Y})", xAxis[Node.X] - circleRadius, yAxis[Node.Y] - circleRadius, Colors.White, textformat);
                }
            }

        }

        private void drawCoordinateGeometry(
           CanvasDrawEventArgs args, float[] xAxis, float[] yAxis,
           Color lineColor, float thickness)
        {
            for (int i = 0; i < yAxis.Length; i++)
            {
                args.DrawingSession.DrawLine(
                    xAxis[0], yAxis[i],
                    xAxis[xAxis.Length - 1],
                    yAxis[i], lineColor,
                    thickness);
            }
            for (int i = 0; i < xAxis.Length; i++)
            {
                args.DrawingSession.DrawLine(
                    xAxis[i],
                    yAxis[0],
                    xAxis[i],
                    yAxis[yAxis.Length - 1],
                    lineColor,
                    thickness);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            List<List<Node>> paths = (List<List<Node>>)e.Parameter;

            int deepTreeLength = paths.Count;
            for (int i = 0; i < paths.Count; i++)
            {
                List<Node> nodes = new List<Node>();
                if (paths[i].Count > deepTreeLength)
                    deepTreeLength = paths[i].Count;
                for (int j = 0; j < paths[i].Count; j++)
                {
                    Node node = paths[i][j].GetCopy();
                    node.ConnectedNodes.Clear();
                    nodes.Add(node);
                }
                MySearchPath.Add(nodes);
            }
            layers = BuildLayer(MySearchPath);
            Tree = GetTree(layers);
            int maxColumn = (Tree.Select(l => l.Count).Max() * 2);
            if (maxColumn % 2 == 0) maxColumn++;
            xAxis = new float[maxColumn];
            yAxis = new float[layers.Count];
            Nodes = AddXaxisToTreeNodes(Tree);
            
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            canvasWidth = (float)container.ActualWidth;
            canvasHeight = (float)container.ActualHeight;

            canvasWidthMargin = canvasWidth / 30;
            canvasHeightMargin = canvasHeight / 15;

            blockWidth = (canvasWidth - 2 * canvasWidthMargin) / (xAxis.Length - 1);
            blockHeight = (canvasHeight - 2 * canvasHeightMargin) / (yAxis.Length - 1);

            if (blockHeight > blockWidth)
                circleRadius = blockWidth / 5;
            else
                circleRadius = blockHeight / 5;

            textformat.FontSize = circleRadius;

            for (int i = 0; i < xAxis.Length; i++)
            {
                xAxis[i] = canvasWidthMargin + (i * blockWidth);
            }
            for (int j = 0; j < yAxis.Length; j++)
            {
                yAxis[j] = canvasHeightMargin + (j * blockHeight);
            }
            lThickness = circleRadius / 4;
            mycanvas.Invalidate();
        }

        /// <summary>
        /// This Method Generate The Tree Layers
        /// Each Layer Have Y Axis And Branches
        /// Each Branch Contain One Node And it's Parrent Node
        /// </summary>
        /// <param name="searchedPath"></param>
        /// <returns>List<Layer></returns>
        public List<Layer> BuildLayer(List<List<Node>> searchedPath)
        {
            List<Layer> Layers = new List<Layer>();
            int maxLayerCount = searchedPath.Select(t => t.Count).Max();
            for (int i = 0; i < maxLayerCount; i++)
            {
                var treeBranch = new Layer() { YAxis = i };
                if (i == 0)
                    treeBranch.Branches = new List<Branch>() { new Branch() { BaseNode = null, Node = searchedPath[0][0] } };
                else if (i == maxLayerCount - 1)
                    treeBranch.Branches = ConnectNodesToThereBases(searchedPath, Layers[i - 1].Branches, i, false);
                else
                    treeBranch.Branches = ConnectNodesToThereBases(searchedPath, Layers[i - 1].Branches, i, true);

                Layers.Add(treeBranch);
            }
            return Layers;
        }

        /// <summary>
        /// Generat The Braches of The Layer
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="previuseLayer"></param>
        /// <param name="layerLevel"></param>
        /// <param name="filtering"></param>
        /// <returns>List<Branch></returns>
        public List<Branch> ConnectNodesToThereBases(List<List<Node>> tree, List<Branch> previuseLayer, int layerLevel, bool filtering)
        {
            List<Branch> Branches = new List<Branch>();
            List<Node> NewNodeLayer = new List<Node>();

            for (int i = 0; i < tree.Count; i++)
            {
                if (tree[i].Count > layerLevel)
                {
                    Node node = tree[i][layerLevel];
                    Node baseNode = null;
                    baseNode = previuseLayer.Where(n => n.Node.NodeName == tree[i][layerLevel - 1].NodeName).FirstOrDefault().Node;
                    Branch branch = new Branch() { Node = node, BaseNode = baseNode };
                    if (filtering)
                    {
                        Node duplicatedNode = NewNodeLayer.Where(n => n.NodeName == node.NodeName).FirstOrDefault();
                        if (duplicatedNode == null)
                        {
                            NewNodeLayer.Add(node);
                            Branches.Add(branch);
                        }
                    }
                    else
                        Branches.Add(branch);
                }
            }
            return Branches;
        }

        /// <summary>
        /// This method Build The Tree Nodes From The Layers
        /// </summary>
        /// <param name="branchLayers"></param>
        /// <returns>List<List<Node>></returns>
        public List<List<Node>> GetTree(List<Layer> branchLayers)
        {
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < branchLayers.Count; i++)
            {

                for (int j = 0; j < branchLayers[i].Branches.Count; j++)
                {
                    var node = branchLayers[i].Branches[j].Node;
                    var baseNode = branchLayers[i].Branches[j].BaseNode;
                    node.Y = branchLayers[i].YAxis;
                    if (baseNode != null)
                    {
                        node.ConnectedNodes.Add(baseNode);
                        //baseNode.ConnectedNodes.Add(node);
                    }
                    nodes.Add(node);
                }
            }
            // Group Each Layer Based On The YAxis
            return nodes.GroupBy(c => c.Y).Select(grp => grp.ToList()).ToList();
        }

        /// <summary>
        /// This Method Add X coordient to The Nodes That are inside the tree
        /// </summary>
        /// <param name="layers"></param>
        public List<Node> AddXaxisToTreeNodes(List<List<Node>> layers)
        {
            int maxColumnLayer = layers.Select(t => t.Count).Max();
            for (int i = 0; i < layers.Count; i++)
            {
                bool filtering = layers[i].Select(c => c.ConnectedNodes).FirstOrDefault().Count > 0 ? true : false;
                List<Node> layer = null;
                if (filtering)
                    layer = layers[i].OrderBy(c => c.ConnectedNodes[0].X).ToList();
                else
                    layer = layers[i];


                int layerNumber = layers[i].Count;
                float Equitable = (xAxis.Length) / layerNumber;
                float Rate = Equitable / 2;
                for (int j = 0; j < layer.Count; j++)
                {
                    layer[j].X = (int)((Equitable * (j + 1)) - Rate);
                }
            }

            return layers.SelectMany(n => n).ToList();
        }
    }
}
