using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Models;
using Search.Models.GraphSearch;
using Search.ViewModel;
using Search.ViewModel.GraphSearch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Search.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GraphSearchPage : Page
    {

        public static float canvasWidth, canvasHeight;

        public SearchToolViewModel SearchTool { get; set; }

        #region feilds

        bool drawPath, drawCorrentRoad;
        int speed, treePathCount, treePathChildCount;
        float startWidth, startHeight, circleRadius;
        float[] xAxis, yAxis;
        int newNodeNameCount = 0;
        Node sL, gL, startLocationOfPreviousNode, goolLocationOfPreviousNode;
        Node selectedNode, pSelectedNode;
        List<Node> nodes = new List<Node>();
        List<Node> availableNodeToConnect = new List<Node>();
        List<Node> Walks { get; set; } = new List<Node>();
        HashSet<Node> PreviousWalks { get; set; } = new HashSet<Node>();
        List<Road> AvaiableRoads { get; set; } = new List<Road>();
        List<List<Node>> MySearchPath { get; set; } = new List<List<Node>>();
        DispatcherTimer SearchSpeedTick = new DispatcherTimer();
        List<string> letters;
        List<string> removedLetters = new List<string>();
        List<Line> Lines = new List<Line>();

        #endregion

        public GraphSearchPage()
        {
            this.InitializeComponent();
            SearchTool = new SearchToolViewModel("DEPTH FIRST", "SMALL", "X2");
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
        }

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Draw the Map Path
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].ConnectedNode.Count; j++)
                {
                    args.DrawingSession.DrawLine(xAxis[nodes[i].X], yAxis[nodes[i].Y], xAxis[nodes[i].ConnectedNode[j].X], yAxis[nodes[i].ConnectedNode[j].Y], Colors.Gray, circleRadius / 2);
                }
            }
            if (SearchTool.ShowMap)
            {
                // Draw the Searching Path
                if (Walks.Count > 1)
                {
                    for (int i = 0; i < Walks.Count - 1; i++)
                    {
                        args.DrawingSession.DrawLine(xAxis[Walks[i].X], yAxis[Walks[i].Y], xAxis[Walks[i + 1].X], yAxis[Walks[i + 1].Y], Colors.Red, circleRadius / 3);
                    }
                }

                // Draw the Corrent Path
                if (AvaiableRoads.Count > 0 && drawCorrentRoad)
                {
                    for (int i = AvaiableRoads[0].PassedRoad.Count - 1; i > 0; i--)
                    {
                        args.DrawingSession.DrawLine(xAxis[AvaiableRoads[0].PassedRoad[i].X], yAxis[AvaiableRoads[0].PassedRoad[i].Y],
                            xAxis[AvaiableRoads[0].PassedRoad[i - 1].X], yAxis[AvaiableRoads[0].PassedRoad[i - 1].Y], Colors.LightGreen, circleRadius / 4);
                    }
                }
            }
            else
            {
                // Draw the y/x axis
                for (int i = 0; i < yAxis.Length; i++)
                {
                    args.DrawingSession.DrawLine(xAxis[0], yAxis[i], xAxis[xAxis.Length - 1], yAxis[i], Colors.Black);
                }
                for (int i = 0; i < xAxis.Length; i++)
                {
                    args.DrawingSession.DrawLine(xAxis[i], yAxis[0], xAxis[i], yAxis[yAxis.Length - 1], Colors.Black);
                }
                if(!SearchTool.ConnectingNodeTogleEnabled)
                {
                    foreach (var x in xAxis)
                    {
                        foreach (var y in yAxis)
                        {
                            bool showFreeLocaion = true;
                            foreach (var line in Lines)
                            {
                                if(line.IfPointOnTheLine(x, y, circleRadius / 2))
                                {
                                    showFreeLocaion = false;
                                }
                            }
                            if(showFreeLocaion)
                                args.DrawingSession.DrawCircle(x, y, 2*circleRadius/3 , Colors.SteelBlue, circleRadius / 8);
                            
                        }
                    }
                }
                
                if (selectedNode != null)
                {
                    foreach (var freeLoaction in availableNodeToConnect)
                    {
                        args.DrawingSession.DrawCircle(xAxis[freeLoaction.X], yAxis[freeLoaction.Y], circleRadius, Colors.Green, circleRadius / 8);
                    }
                    args.DrawingSession.DrawCircle(xAxis[selectedNode.X], yAxis[selectedNode.Y], circleRadius, Colors.White, circleRadius / 8);
                }
            }
            // Draw the Node
            for (int i = 0; i < nodes.Count; i++)
            {
                args.DrawingSession.FillCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Black);
                var textformat = new CanvasTextFormat() { FontSize = circleRadius, WordWrapping = CanvasWordWrapping.NoWrap, FontFamily = "Arial" };
                textformat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                textformat.VerticalAlignment = CanvasVerticalAlignment.Center;
                args.DrawingSession.DrawText(nodes[i].NodeName, xAxis[nodes[i].X], yAxis[nodes[i].Y], Colors.White, textformat);

                if (nodes[i].StartPoint)
                {
                    args.DrawingSession.DrawCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Red, circleRadius / 4);
                }
                if (nodes[i].GoolPoint)
                {
                    args.DrawingSession.DrawCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.LightGreen, circleRadius / 4);
                }
            }
        }

        private void container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasWidth = (float)container.ActualWidth;
            startWidth = canvasWidth / 11;

            canvasHeight = (float)container.ActualHeight;
            startHeight = canvasHeight / 6;

            xAxis = new float[10] { startWidth, 2 * startWidth, 3 * startWidth, 4 * startWidth, 5 * startWidth, 6 * startWidth, 7 * startWidth, 8 * startWidth, 9 * startWidth, 10 * startWidth };
            yAxis = new float[5] { startHeight, 2 * startHeight, 3 * startHeight, 4 * startHeight, 5 * startHeight };

            if (startHeight > startWidth)
                circleRadius = startWidth / 4;
            else
                circleRadius = startHeight / 4;

        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            #region

            if (sL != null && gL != null && sL.NodeName != gL.NodeName)
            {
                StopAnimation();
                Road road = new Road() { HeadNode = sL, PassedRoad = new List<Node>() { sL } };

                foreach (var item in road.HeadNode.ConnectedNode)
                {
                    road.PossibleNodes.Enqueue(item);
                }
                if (SearchTool.SelectedSearchType == SearchTool.SearchTypes[0])
                {
                    DepthFirstSearch(road);
                }
                else if (SearchTool.SelectedSearchType == SearchTool.SearchTypes[1])
                {
                    foreach (var possiblenode in road.PossibleNodes)
                    {
                        Road extendedRoad = new Road();
                        extendedRoad.HeadNode = possiblenode;
                        foreach (var passed in road.PassedRoad)
                        {
                            extendedRoad.PassedRoad.Add(passed);
                        }
                        extendedRoad.PassedRoad.Insert(0, possiblenode);
                        road.PossibleRoads.Add(extendedRoad);
                    }
                    BreadthFirst(road);

                }
                for (int i = 0; i < MySearchPath.Count; i++)
                {
                    for (int j = MySearchPath[i].Count - 1; j >= 0; j--)
                    {
                        Debug.Write(MySearchPath[i][j].NodeName);
                    }
                    Debug.WriteLine("");
                }
                DrawPath();
                SearchSpeedTick.Start();
            }

            #endregion
        }

        private void DepthFirstSearch(Road road)
        {
            // if there isn't connected node roll back we rich the end of the tell

            if (road.HeadNode.GoolPoint)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                MySearchPath.Add(road.PassedRoad);
                AvaiableRoads.Add(road);
            }
            else if (road.PossibleNodes.Count <= 0)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                MySearchPath.Add(road.PassedRoad);
                return;
            }
            // if there is connected node
            else
            {
                // check if we already find one path if we do roll back
                if (AvaiableRoads.Count <= 0)
                {
                    while (road.PossibleNodes.Count != 0 && AvaiableRoads.Count <= 0)
                    {
                        // if not extend the node
                        Road extendedRoad = new Road();
                        extendedRoad.PassedRoad.Add(road.HeadNode);
                        foreach (var item in road.PassedRoad)
                            extendedRoad.PassedRoad.Add(item);

                        extendedRoad.HeadNode = road.PossibleNodes.Dequeue();

                        // add the possible road for the extended node
                        foreach (var item in extendedRoad.HeadNode.ConnectedNode)
                        {
                            bool passed = false;
                            foreach (var pastroad in extendedRoad.PassedRoad)
                            {
                                if (pastroad.NodeName == item.NodeName)
                                    passed = true;
                            }
                            if (!passed)
                                extendedRoad.PossibleNodes.Enqueue(item);
                        }
                        DepthFirstSearch(extendedRoad);
                    }

                }
            }
        }

        private void BreadthFirst(Road road)
        {
            // check the extended roads
            foreach (var extendedroad in road.PossibleRoads)
            {
                MySearchPath.Add(extendedroad.PassedRoad);
                if (extendedroad.HeadNode.GoolPoint)
                {
                    AvaiableRoads.Add(extendedroad);
                    // return
                }
            }
            if (AvaiableRoads.Count <= 0)
            {
                // extended the extended road 
                Road extendedRoads = new Road();
                foreach (var extendedRoad in road.PossibleRoads)
                {
                    foreach (var node in extendedRoad.HeadNode.ConnectedNode)
                    {
                        Road newExtendedRoad = new Road();
                        newExtendedRoad.HeadNode = node;
                        bool passed = false;
                        foreach (var pastroad in extendedRoad.PassedRoad)
                        {
                            if (pastroad.NodeName == node.NodeName)
                                passed = true;
                            newExtendedRoad.PassedRoad.Add(pastroad);
                        }
                        if (!passed)
                        {
                            newExtendedRoad.PossibleNodes.Enqueue(node);
                            newExtendedRoad.PassedRoad.Insert(0, node);
                            extendedRoads.PossibleRoads.Add(newExtendedRoad);
                        }
                    }
                }
                BreadthFirst(extendedRoads);
            }

        }

        private void ComboBox_SearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation if it's drawings
            StopAnimation();

            canvascontroll.Invalidate();
        }

        private void ComboBox_StartLocationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation
            StopAnimation();

            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(value))
            {
                // initialze the previouseNode
                if (startLocationOfPreviousNode != null)
                {
                    startLocationOfPreviousNode.StartPoint = false;
                }
                startLocationOfPreviousNode = nodes.Find(n => n.NodeName == value);
                // add logo to start location
                sL = nodes.Find(n => n.NodeName == value);
                sL.StartPoint = true;
                // update the ui
                canvascontroll.Invalidate();
            }

        }

        private void ComboBox_GoolLocationSelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation
            StopAnimation();

            // Get The New Select Gool Location
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(value))
            {
                // remove the Gool logo from the old Gool location
                if (goolLocationOfPreviousNode != null)
                {
                    goolLocationOfPreviousNode.GoolPoint = false;
                }

                goolLocationOfPreviousNode = nodes.Find(n => n.NodeName == value);
                gL = nodes.Find(n => n.NodeName == value);
                gL.GoolPoint = true;

                canvascontroll.Invalidate();
            }
        }

        private void ComboBox_MapsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopAnimation();
            AvaiableRoads.Clear();
            nodes.Clear();
            startLocationOfPreviousNode = null;
            goolLocationOfPreviousNode = null;
            SearchTool.ShowMap = true;
            SearchTool.ShowCustomMapTool = false;
            sL = null;
            gL = null;
            selectedNode = null;
            if (SearchTool.SelectedMap == SearchTool.Maps[0])
            {
                initializeMap1();
            }
            else if (SearchTool.SelectedMap == SearchTool.Maps[1])
            {
                initializeMap2();
            }
            else
            {
                SearchTool.StartLocations.Clear();
                SearchTool.GoolLocations.Clear();
                SearchTool.ShowMap = false;
                SearchTool.ShowCustomMapTool = true;
                newNodeNameCount = 0;
                letters = GetAlphabeticallyLetters();
            }
            canvascontroll.Invalidate();

        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(TreeVisuals), MySearchPath);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        private void canvascontroll_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (SearchTool.SelectedMap == SearchTool.Maps[2])
            {
                var xTappedLoacation = (float)e.GetPosition(canvascontroll).X;
                var yTappedLoacation = (float)e.GetPosition(canvascontroll).Y;
                var xyIndex = GetXYAxis(xTappedLoacation, yTappedLoacation);
                bool exist = false;
                // select the node if its exist on the board at the tap location
                if (xyIndex.Item1 != -1)
                {
                    foreach (var node in nodes)
                    {
                        if (node.X == xyIndex.Item1 && node.Y == xyIndex.Item2)
                        {
                            exist = true;

                            pSelectedNode = selectedNode;
                            selectedNode = node;
                            canvascontroll.Invalidate();
                            break;
                        }
                    }
                }
                else
                    exist = true;

                // if I am adding nodes
                if (!SearchTool.ConnectingNodeTogleEnabled)
                {
                    // add new node to the board if it's not exist at the tap location
                    if (!exist && (newNodeNameCount < 26 || removedLetters.Count > 0))
                    {
                        selectedNode = new Node();
                        selectedNode.X = xyIndex.Item1;
                        selectedNode.Y = xyIndex.Item2;
                        if (removedLetters.Count > 0)
                        {
                            selectedNode.NodeName = GetFromRemovedLetters();
                        }
                        else
                        {
                            selectedNode.NodeName = letters[newNodeNameCount++];
                        }
                        selectedNode.ConnectedNode = new List<Node>();
                        nodes.Add(selectedNode);
                        canvascontroll.Invalidate();
                    }
                }
                // if I am connecting the nodes!
                else
                {
                    // check if the previous node and the selected node are defferent
                    if (selectedNode != null && pSelectedNode != null && selectedNode.NodeName != pSelectedNode.NodeName)
                    {
                        ConnectTwoNode(selectedNode, pSelectedNode);
                    }
                }
                if (SearchTool.ConnectingNodeTogleEnabled)
                {
                    GetAvailableNodeToConnect(selectedNode);
                    canvascontroll.Invalidate();
                }

            }
        }

        private void canvascontroll_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (SearchTool.SelectedMap == SearchTool.Maps[2])
            {
                var xTappedLoacation = (float)e.GetPosition(canvascontroll).X;
                var yTappedLoacation = (float)e.GetPosition(canvascontroll).Y;
                var xyIndex = GetXYAxis(xTappedLoacation, yTappedLoacation);

                // remove the node if it's exist on the board
                if (xyIndex.Item1 != -1)
                {
                    foreach (var node in nodes)
                    {
                        if (node.X == xyIndex.Item1 && node.Y == xyIndex.Item2)
                        {
                            AddToRemovedList(node.NodeName);
                            // cut all the road that lead to this node
                            foreach (var subNode in node.ConnectedNode)
                            {
                                subNode.ConnectedNode.Remove(node);
                            }
                            nodes.Remove(node);
                            selectedNode = null;
                            canvascontroll.Invalidate();
                            break;
                        }
                    }
                }
                // remove the line between the connected nodes
                foreach (var line in Lines)
                {
                    if (line.IfPointOnTheLine(xTappedLoacation, yTappedLoacation, circleRadius / 2))
                    {
                        DissConnectTwoNode(line.LineName.Item1, line.LineName.Item2);
                        Lines.Remove(line);
                        canvascontroll.Invalidate();
                        break;
                    }
                }
            }
        }

        private void ComboBox_SpeedSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            if (value == SearchTool.SearchSpeed[0])
            {
                speed = 2000;
            }
            else if (value == SearchTool.SearchSpeed[1])
            {
                speed = 500;
            }
            else
            {
                speed = 250;
            }
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, speed);
        }

        /// <summary>
        /// Update the Start/Gool point after the map changed
        /// </summary>
        private void UpdatePoints()
        {
            SearchTool.StartLocations.Clear();
            SearchTool.GoolLocations.Clear();
            foreach (var item in nodes)
            {
                SearchTool.StartLocations.Add(item.NodeName);
                SearchTool.GoolLocations.Add(item.NodeName);
            }
        }

        private void RemoveNode_Button_Click(object sender, RoutedEventArgs e)
        {
            nodes.Clear();
            removedLetters.Clear();
            selectedNode = null;
            newNodeNameCount = 0;
            canvascontroll.Invalidate();
        }

        private void SearchSpeedTick_Tick(object sender, object e)
        {
            DrawPath();
        }

        private void DrawPath()
        {
            if (treePathChildCount == -1)
            {
                Walks.Clear();
                drawPath = true;
                treePathCount += 1;
                SearchTool.SearchedPathCount += 1;

                if (treePathCount == MySearchPath.Count)
                {
                    drawCorrentRoad = true;
                    SearchSpeedTick.Stop();
                    treePathCount = 0;
                    PreviousWalks.Clear();
                    canvascontroll.Invalidate();
                    return;
                }
            }
            if (treePathCount < MySearchPath.Count && drawPath == true)
            {
                treePathChildCount = MySearchPath[treePathCount].Count - 1;
                drawPath = false;
                if (PreviousWalks.Count > 1)
                {
                    while (treePathChildCount > 0)
                    {
                        bool passedNode = false;
                        foreach (var passed in PreviousWalks)
                        {
                            if (MySearchPath[treePathCount][treePathChildCount].NodeName == passed.NodeName)
                            {
                                Walks.Add(passed);
                                treePathChildCount -= 1;
                                passedNode = true;
                                break;
                            }
                            if (treePathChildCount == 0)
                            {
                                break;
                            }
                        }
                        if (!passedNode)
                        {
                            break;
                        }
                    }
                }

            }
            if (treePathCount != MySearchPath.Count && treePathChildCount != -1)
            {
                Walks.Add(MySearchPath[treePathCount][treePathChildCount]);
                PreviousWalks.Add(MySearchPath[treePathCount][treePathChildCount]);
                treePathChildCount -= 1;
                canvascontroll.Invalidate();
            }
        }

        private void StopAnimation()
        {
            treePathCount = 0;
            treePathChildCount = 0;
            SearchTool.SearchedPathCount = 0;
            drawPath = true;
            SearchSpeedTick.Stop();
            drawCorrentRoad = false;
            Walks.Clear();
            AvaiableRoads.Clear();
            MySearchPath.Clear();
            PreviousWalks.Clear();
            canvascontroll.Invalidate();
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            bool exist = false;
            if(!string.IsNullOrWhiteSpace(SearchTool.NewMapNameTextBox))
            {
                foreach (var mapName in SearchTool.Maps)
                {
                    if (mapName == SearchTool.NewMapNameTextBox)
                    {
                        exist = true;
                        break;
                    }
                }
                if(!exist)
                {
                    MapName_TextBox.Header = "";
                }
                else
                {
                    MapName_TextBox.Header = "This Name is exist in your Maps!";
                }
            }
            else
            {
                MapName_TextBox.Header = "Map name should not be empty!";
            }
        }

        private void ConnectNodes_Button_Click(object sender, RoutedEventArgs e)
        {
            GetAvailableNodeToConnect(selectedNode);
            canvascontroll.Invalidate();
        }

        private void initializeMap1()
        {
            Node S = new Node() { NodeName = "S", X = 1, Y = 3 };
            Node A = new Node() { NodeName = "A", X = 3, Y = 2 };
            Node B = new Node() { NodeName = "B", X = 4, Y = 3 };
            Node C = new Node() { NodeName = "C", X = 5, Y = 1 };
            Node D = new Node() { NodeName = "D", X = 6, Y = 3 };
            Node E = new Node() { NodeName = "E", X = 7, Y = 1 };
            Node G = new Node() { NodeName = "G", X = 7, Y = 2 };

            S.ConnectedNode = new List<Node>() { A, B };
            A.ConnectedNode = new List<Node>() { S, C, B };
            B.ConnectedNode = new List<Node>() { S, A, D };
            C.ConnectedNode = new List<Node>() { A, E, D };
            D.ConnectedNode = new List<Node>() { B, C, G };
            E.ConnectedNode = new List<Node>() { C };
            G.ConnectedNode = new List<Node>() { D };

            nodes = new List<Node>() { S, A, B, C, D, E, G };
            UpdatePoints();


        }

        private void initializeMap2()
        {

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

            A.ConnectedNode = new List<Node>() { B, C };
            B.ConnectedNode = new List<Node>() { A, D, F };
            C.ConnectedNode = new List<Node>() { A, E, H };
            D.ConnectedNode = new List<Node>() { B, I, G };
            E.ConnectedNode = new List<Node>() { F, C, K };
            F.ConnectedNode = new List<Node>() { B, E, G };
            G.ConnectedNode = new List<Node>() { D, F, J };
            H.ConnectedNode = new List<Node>() { C, K, N };
            I.ConnectedNode = new List<Node>() { D, L };
            J.ConnectedNode = new List<Node>() { G };
            K.ConnectedNode = new List<Node>() { E, H };
            L.ConnectedNode = new List<Node>() { I, M };
            M.ConnectedNode = new List<Node>() { L, N };
            N.ConnectedNode = new List<Node>() { H, M };

            nodes = new List<Node>() { A, B, C, D, E, F, G, H, I, J, K, L, M, N };
            UpdatePoints();


        }

        private Tuple<int, int> GetXYAxis(float x, float y)
        {
            for (int i = 0; i < xAxis.Length; i++)
            {
                for (int j = 0; j < yAxis.Length; j++)
                {
                    if ((x > xAxis[i] - circleRadius && x < xAxis[i] + circleRadius) &&
                            y > yAxis[j] - circleRadius && y < yAxis[j] + circleRadius)
                    {
                        return new Tuple<int, int>(i, j);
                    }
                }
            }
            return new Tuple<int, int>(-1, 0);
        }

        private List<string> GetAlphabeticallyLetters()
        {
            var query = Enumerable.Range(0, 26)
                            .Select(i => ((char)('A' + i)).ToString()).ToList();
            return query;
        }

        private void AddToRemovedList(string letter)
        {
            removedLetters.Add(letter);
            removedLetters.Sort();
        }

        private string GetFromRemovedLetters()
        {
            string nodeName = removedLetters[0];
            removedLetters.RemoveAt(0);
            return nodeName;
        }

        private Node GetNodeByName(string name)
        {
            return nodes.Where(n => n.NodeName == name).FirstOrDefault();
        }

        private void ConnectTwoNode(Node selectedNode, Node pSelectedNode)
        {
            bool exist = false;
            foreach (var node in availableNodeToConnect)
            {
                if (node.NodeName == selectedNode.NodeName)
                {
                    exist = true;
                    break;
                }
            }
            if (exist)
            {
                selectedNode.ConnectedNode.Add(pSelectedNode);
                pSelectedNode.ConnectedNode.Add(selectedNode);
                Line line = new Line()
                {
                    LineName = new Tuple<Node, Node>(this.selectedNode, this.pSelectedNode),
                    Point1 = new Tuple<float, float>(xAxis[selectedNode.X], yAxis[selectedNode.Y]),
                    Point2 = new Tuple<float, float>(xAxis[pSelectedNode.X], yAxis[pSelectedNode.Y])
                };
                Lines.Add(line);
                this.selectedNode = null;
                this.pSelectedNode = null;
            }
        }

        private void DissConnectTwoNode(Node node1, Node node2)
        {
            node1.ConnectedNode.Remove(node2);
            node2.ConnectedNode.Remove(node1);
        }

        private void GetAvailableNodeToConnect(Node sNode)
        {
            availableNodeToConnect.Clear();
            if(sNode != null && SearchTool.ConnectingNodeTogleEnabled)
            {
                Node top = null, right = null, bottom = null, left = null,
               topRight = null, bottomRight = null, bottomLeft = null, topLeft = null;
                foreach (var node in nodes)
                {
                    if (node != sNode)
                    {
                        // remove Verticals loactions
                        if (node.X == sNode.X)
                        {
                            // bottom
                            if (node.Y > sNode.Y)
                            {
                                if (bottom == null)
                                {
                                    bottom = node;
                                    availableNodeToConnect.Add(node);
                                }
                                else if (node.Y < bottom.Y)
                                {
                                    availableNodeToConnect.Remove(bottom);
                                    bottom = node;
                                    availableNodeToConnect.Add(node);
                                }
                            }
                            // top
                            else if (node.Y < sNode.Y)
                            {
                                if (top == null)
                                {
                                    top = node;
                                    availableNodeToConnect.Add(node);
                                }
                                else if (node.Y > top.Y)
                                {
                                    availableNodeToConnect.Remove(top);
                                    top = node;
                                    availableNodeToConnect.Add(node);
                                }
                            }
                        }
                        // remove Horizonalas locations
                        else if (node.Y == sNode.Y)
                        {
                            // left
                            if (node.X < sNode.X)
                            {
                                if (left == null)
                                {
                                    left = node;
                                    availableNodeToConnect.Add(node);
                                }
                                else if (node.X > left.X)
                                {
                                    availableNodeToConnect.Remove(left);
                                    left = node;
                                    availableNodeToConnect.Add(node);
                                }
                            }
                            // right
                            else if (node.X > sNode.X)
                            {
                                if (right == null)
                                {
                                    right = node;
                                    availableNodeToConnect.Add(node);
                                }

                                else if (node.X < right.X)
                                {
                                    availableNodeToConnect.Remove(right);
                                    right = node;
                                    availableNodeToConnect.Add(node);
                                }
                            }
                        }
                        // remove the half bottom right Diameters
                        else if (node.X > sNode.X && node.Y > sNode.Y && Math.Abs(node.X - node.Y) == Math.Abs(sNode.X - sNode.Y))
                        {
                            if (bottomRight == null)
                            {
                                bottomRight = node;
                                availableNodeToConnect.Add(node);
                            }
                            // 3,3 4,4 5,5
                            else if (node.X < bottomRight.X && node.Y < bottomRight.Y)
                            {
                                availableNodeToConnect.Remove(bottomRight);
                                bottomRight = node;
                                availableNodeToConnect.Add(node);
                            }
                        }
                        //  remove the half top right Dimaters
                        else if (node.X > sNode.X && node.Y < sNode.Y && node.X + node.Y == sNode.X + sNode.Y)
                        {
                            if (topRight == null)
                            {
                                topRight = node;
                                availableNodeToConnect.Add(node);
                            }
                            // 3,3 4,2 5,1
                            else if (node.X < topRight.X && node.Y > topRight.Y)
                            {
                                availableNodeToConnect.Remove(topRight);
                                topRight = node;
                                availableNodeToConnect.Add(node);
                            }
                        }
                        // remove the half top Left Diameter
                        else if (node.X < sNode.X && node.Y < sNode.Y && Math.Abs(node.X - node.Y) == Math.Abs(sNode.X - sNode.Y))
                        {
                            if (topLeft == null)
                            {
                                topLeft = node;
                                availableNodeToConnect.Add(node);
                            }
                            // 3,3 2,2, 1,1
                            else if (node.X > topLeft.X && node.Y > topLeft.Y)
                            {
                                availableNodeToConnect.Remove(topLeft);
                                topLeft = node;
                                availableNodeToConnect.Add(node);
                            }
                        }
                        // remove the half bottom left Diameter
                        else if (node.X < sNode.X && node.Y > sNode.Y && node.X + node.Y == sNode.X + sNode.Y)
                        {
                            if (bottomLeft == null)
                            {
                                bottomLeft = node;
                                availableNodeToConnect.Add(node);
                            }
                            // 3,3 2,4 1,5
                            else if (node.X > bottomLeft.X && node.Y < bottomLeft.Y)
                            {
                                availableNodeToConnect.Remove(bottomLeft);
                                bottomLeft = node;
                                availableNodeToConnect.Add(node);
                            }
                        }
                        // No Problem
                        else
                        {
                            availableNodeToConnect.Add(node);
                        }
                    }
                }
                foreach (var connectedNode in sNode.ConnectedNode)
                {
                    availableNodeToConnect.Remove(connectedNode);
                }
            }
        }
    }
}