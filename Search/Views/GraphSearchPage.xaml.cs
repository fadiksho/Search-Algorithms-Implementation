
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Newtonsoft.Json;
using Search.Models.GraphSearch;
using Search.ViewModel.GraphSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
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
        float blockWidth, blockHeight, circleRadius, canvasWidthMargin, canvasHeightMargin;
        float[] xAxis, yAxis;
        int newNodeNameCount = 0;
        Node sL, gL, startLocationOfPreviousNode, goolLocationOfPreviousNode;
        Node selectedNode, pSelectedNode;
        List<Node> nodes = new List<Node>();
        List<Map> JsonMaps;
        List<Node> availableNodeToConnect = new List<Node>();
        List<Node> Walks { get; set; } = new List<Node>();
        HashSet<Node> PreviousWalks { get; set; } = new HashSet<Node>();
        List<Road> AvaiableRoads { get; set; } = new List<Road>();
        List<List<Node>> MySearchPath { get; set; } = new List<List<Node>>();
        DispatcherTimer SearchSpeedTick = new DispatcherTimer();
        List<string> letters;
        List<string> removedLetters = new List<string>();
        List<Line> Lines = new List<Line>();
        Random rnd = new Random();

        #endregion

        public GraphSearchPage()
        {
            this.InitializeComponent();
            SearchTool = new SearchToolViewModel("DEPTH FIRST", "X2");
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
            
        }

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Draw the Map Path
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].ConnectedNodes.Count; j++)
                {
                    args.DrawingSession.DrawLine(xAxis[nodes[i].X], yAxis[nodes[i].Y], xAxis[nodes[i].ConnectedNodes[j].X], yAxis[nodes[i].ConnectedNodes[j].Y], Colors.Gray, circleRadius / 10);
                }
            }
            if (SearchTool.ShowMap)
            {

                // Draw the Searching Path
                var textformat = new CanvasTextFormat() { FontSize = circleRadius, WordWrapping = CanvasWordWrapping.NoWrap, FontFamily = "Arial" };
                textformat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                textformat.VerticalAlignment = CanvasVerticalAlignment.Center;
                if (Walks.Count > 1)
                {
                    for (int i = 0; i < Walks.Count - 1; i++)
                    {
                        args.DrawingSession.DrawLine(xAxis[Walks[i].X], yAxis[Walks[i].Y], xAxis[Walks[i + 1].X], yAxis[Walks[i + 1].Y], Colors.Red, circleRadius / 10);
                        args.DrawingSession.DrawText( (i + 1).ToString(), xAxis[Walks[i].X] + circleRadius + 3, yAxis[Walks[i].Y] - circleRadius - 3, Colors.White, textformat);
                        if(i == Walks.Count - 2)
                        {
                            args.DrawingSession.DrawCircle(xAxis[Walks[i+1].X], yAxis[Walks[i + 1].Y], circleRadius, Colors.Green, circleRadius / 4);
                            args.DrawingSession.DrawText((Walks.Count).ToString(), xAxis[Walks[i + 1].X] + circleRadius + 3, yAxis[Walks[i + 1].Y] - circleRadius - 3, Colors.White, textformat);
                        }
                    }
                }

                // Draw the Corrent Path
                if (AvaiableRoads.Count > 0 && drawCorrentRoad)
                {
                    for (int i = 1; i < AvaiableRoads[0].PassedRoad.Count; i++)
                    {
                        args.DrawingSession.DrawLine(xAxis[AvaiableRoads[0].PassedRoad[i].X], yAxis[AvaiableRoads[0].PassedRoad[i].Y],
                        xAxis[AvaiableRoads[0].PassedRoad[i - 1].X], yAxis[AvaiableRoads[0].PassedRoad[i - 1].Y], Colors.LightGreen, circleRadius / 4);

                        args.DrawingSession.DrawText(i.ToString(), xAxis[AvaiableRoads[0].PassedRoad[i - 1].X] + circleRadius + 3, yAxis[AvaiableRoads[0].PassedRoad[i - 1].Y] - circleRadius - 3, Colors.White, textformat);
                        if(i == AvaiableRoads[0].PassedRoad.Count - 1)
                        {
                            args.DrawingSession.DrawText((i+1).ToString(), xAxis[AvaiableRoads[0].PassedRoad[i].X] + circleRadius + 3, yAxis[AvaiableRoads[0].PassedRoad[i].Y] - circleRadius - 3, Colors.White, textformat);
                        }

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
                if (!SearchTool.ConnectingNodeTogleEnabled)
                {
                    foreach (var x in xAxis)
                    {
                        foreach (var y in yAxis)
                        {
                            bool showFreeLocaion = true;
                            foreach (var line in Lines)
                            {
                                if (line.IfPointOnTheLine(x, y, circleRadius / 2, xAxis, yAxis))
                                {
                                    showFreeLocaion = false;
                                }
                            }
                            if (showFreeLocaion)
                                args.DrawingSession.DrawCircle(x, y, 2 * circleRadius / 3, Colors.SteelBlue, circleRadius / 8);
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
                    args.DrawingSession.DrawCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Red, circleRadius / 10);
                }
                if (nodes[i].GoolPoint)
                {
                    args.DrawingSession.DrawCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Green, circleRadius / 10);
                }
            }
        }

        private void container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasWidth = (float)CanvasContainer.ActualWidth;
            canvasHeight = (float)CanvasContainer.ActualHeight;

            canvasWidthMargin = canvasWidth / 30;
            canvasHeightMargin = canvasHeight / 15;

            blockWidth = (canvasWidth - 2 * canvasWidthMargin) / 9;
            blockHeight = (canvasHeight - 2 * canvasHeightMargin) / 4;

            if (blockHeight > blockWidth)
            {
                circleRadius = blockWidth / 5;
            }
            else
            {
                circleRadius = blockHeight / 5;
            }
            
            xAxis = new float[10] {canvasWidthMargin, blockWidth + canvasWidthMargin , (2 * blockWidth) + canvasWidthMargin, (3 * blockWidth) + canvasWidthMargin, (4 * blockWidth) + canvasWidthMargin, (5 * blockWidth) + canvasWidthMargin, (6 * blockWidth) + canvasWidthMargin, (7 * blockWidth) + canvasWidthMargin, (8 * blockWidth) + canvasWidthMargin, (9 * blockWidth) + canvasWidthMargin };
            yAxis = new float[5] { canvasHeightMargin, blockHeight + canvasHeightMargin , (2 * blockHeight) + canvasHeightMargin, (3 * blockHeight) + canvasHeightMargin, (4 * blockHeight) + canvasHeightMargin };

        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            #region
            if (sL != null && gL != null && sL != gL)
            {
                StopAnimation();
                Road road = new Road() { HeadNode = sL, PassedRoad = new List<Node>() { sL } };
                //Firt all the connected Node are possible Roads
                foreach (var connectedNode in road.HeadNode.ConnectedNodes)
                {
                    road.PossibleNodes.Enqueue(connectedNode);
                }
                // Depth First Search
                if (SearchTool.SelectedSearchType == SearchTool.SearchTypes[0])
                {
                    DepthFirstSearch(road);
                    
                    Draw2();
                }
                // Breadth First Search
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
                        extendedRoad.PassedRoad.Add(possiblenode);
                        road.PossibleRoads.Add(extendedRoad);
                    }
                    BreadthFirst(road);
                    foreach (var path in MySearchPath)
                    {
                        foreach (var node in path)
                        {
                            Debug.Write(node.NodeName + " ");
                        }
                        Debug.WriteLine("");
                    }

                    Draw2();
                }
                
                SearchSpeedTick.Start();
            }
            #endregion
        }

        private void DepthFirstSearch(Road road)
        {
            if (road.HeadNode.GoolPoint)
            {
                AvaiableRoads.Add(road);
                MySearchPath.Add(road.PassedRoad);
                return;
            }
            // mabe we dont need this one
            else if (road.PossibleNodes.Count <= 0)
            {
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
                        
                        foreach (var item in road.PassedRoad)
                            extendedRoad.PassedRoad.Add(item);

                        extendedRoad.HeadNode = road.PossibleNodes.Dequeue();
                        extendedRoad.PassedRoad.Add(extendedRoad.HeadNode);
                        
                        // add the possible road for the extended node
                        foreach (var item in extendedRoad.HeadNode.ConnectedNodes)
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
            if(road.PossibleRoads.Count > 0)
            {
                // check the extended roads
                foreach (var extendedroad in road.PossibleRoads)
                {
                    MySearchPath.Add(extendedroad.PassedRoad);
                    if (extendedroad.HeadNode.GoolPoint)
                    {
                        AvaiableRoads.Add(extendedroad);
                        return;
                    }
                }
                if (AvaiableRoads.Count <= 0)
                {
                    // extended the extended road 
                    Road extendedRoads = new Road();
                    foreach (var extendedRoad in road.PossibleRoads)
                    {
                        foreach (var node in extendedRoad.HeadNode.ConnectedNodes)
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
                                newExtendedRoad.PassedRoad.Add(node);
                                extendedRoads.PossibleRoads.Add(newExtendedRoad);
                            }
                        }
                    }
                    BreadthFirst(extendedRoads);
                }
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
            MapSelectionChanged_Clear();

            // if we change the map
            if (SearchTool.SelectedMap != null)
            {
                nodes = GetNodes(SearchTool.SelectedMap.Nodes);
                UpdatePoints();
                SearchTool.ShowMap = true;
                SearchTool.ShowCustomMapTool = false;
            }
            // if we creating a new map
            else if (SearchTool.SelectedMap == null)
            {
                SearchTool.ShowMap = false;
                SearchTool.ShowCustomMapTool = true;
                newNodeNameCount = 0;
                CleanCustomMapTool();
                letters = GetAlphabeticallyLetters();
            }
            canvascontroll.Invalidate();
        }

        private void MapSelectionChanged_Clear()
        {
            AvaiableRoads.Clear();
            nodes = new List<Node>();
            startLocationOfPreviousNode = null;
            goolLocationOfPreviousNode = null;
            sL = null;
            gL = null;
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

        private void canvascontroll_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SearchTool.SelectedMap == null)
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

                        bool showFreeLocaion = true;
                        foreach (var line in Lines)
                        {
                            if (line.IfPointOnTheLine(xAxis[xyIndex.Item1], yAxis[xyIndex.Item2], circleRadius / 2, xAxis, yAxis))
                            {
                                showFreeLocaion = false;
                            }
                        }
                        if (showFreeLocaion)
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
                            selectedNode.ConnectedNodes = new List<Node>();
                            nodes.Add(selectedNode);
                            canvascontroll.Invalidate();
                        }


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
                    else if(exist && selectedNode == pSelectedNode)
                    {
                        selectedNode = null;
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
            if (SearchTool.SelectedMap == null)
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
                            foreach (var subNode in node.ConnectedNodes)
                            {
                                subNode.ConnectedNodes.Remove(node);
                            }
                            RemoveLine(node);
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
                    if (line.IfPointOnTheLine(xTappedLoacation, yTappedLoacation, circleRadius / 2, xAxis, yAxis))
                    {
                        DissConnectTwoNode(line);
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
            Lines.Clear();
            selectedNode = null;
            newNodeNameCount = 0;
            canvascontroll.Invalidate();
        }

        private void SearchSpeedTick_Tick(object sender, object e)
        {
            if(MySearchPath.Count > 0)
                Draw2();
        }

        private void ConnectNodes_Button_Click(object sender, RoutedEventArgs e)
        {
            GetAvailableNodeToConnect(selectedNode);
            canvascontroll.Invalidate();
        }

        private async void Page_LoadedAsync(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ApplicationData.Current.LocalFolder.Path + "\\Maps.json"))
            {
                string jsonStringMaps = JsonConvert.SerializeObject(GetDefualtMaps(), new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile sampleFile = await localFolder.CreateFileAsync("Maps.json");
                await FileIO.WriteTextAsync(sampleFile, jsonStringMaps);
            }

            JsonMaps = JsonConvert.DeserializeObject<List<Map>>(File.ReadAllText(ApplicationData.Current.LocalFolder.Path + "\\Maps.json"), new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

            SearchTool.Maps = new ObservableCollection<Map>(JsonMaps);

            SearchTool.SelectedMap = SearchTool.Maps[0];

        }

        private List<Node> initializeMap1()
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
        
        private List<Node> initializeMap2()
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

        private List<Map> GetDefualtMaps()
        {
            Map map1 = new Map()
            {
                Name = "Small",
                IsDeleteEnabled = false,
                Nodes = initializeMap1()

            };
            Map map2 = new Map()
            {
                Name = "Complex",
                IsDeleteEnabled = false,
                Nodes = initializeMap2()
            };

            return new List<Map>() { map1, map2 };
        }

        private void AddNewMap_ButtonClick(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            SearchTool.SelectedMap = null;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            SearchTool.SelectedMap = SearchTool.Maps[0];
        }

        private async void Save_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            bool exist = false;
            if (!string.IsNullOrWhiteSpace(SearchTool.NewMapNameTextBox))
            {
                foreach (var mapName in SearchTool.Maps)
                {
                    if (mapName.Name == SearchTool.NewMapNameTextBox)
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                {
                    var newMap = new Map() { Name = SearchTool.NewMapNameTextBox, Nodes = nodes, IsDeleteEnabled = true };
                    JsonMaps.Add(newMap);
                    SearchTool.Maps.Add(newMap);
                    var jsonStringMap = JsonConvert.SerializeObject(JsonMaps, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                    var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Maps.json");
                    await FileIO.WriteTextAsync(file, jsonStringMap);
                    SearchTool.SelectedMap = newMap;
                }
                else
                {
                    MapValidation_TextBlock.Visibility = Visibility.Visible;
                    MapValidation_TextBlock.Text = "This Name is exist in your Maps!";
                }
            }
            else
            {
                MapValidation_TextBlock.Visibility = Visibility.Visible;
                MapValidation_TextBlock.Text = "Map name should not be empty!";
            }
        }

        private async void DeleteMap_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tag = button.Tag;
            var map = SearchTool.Maps.Where(m => m.Name == tag.ToString()).FirstOrDefault();
            if(SearchTool.SelectedMap == map)
            {
                SearchTool.SelectedMap = SearchTool.Maps[0];
            }
            SearchTool.Maps.Remove(map);
            if (map != null)
            {
                JsonMaps.Remove(map);
                var jsonStringMap = JsonConvert.SerializeObject(JsonMaps, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Maps.json");
                await FileIO.WriteTextAsync(file, jsonStringMap);
            }
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

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            UpdatePlayButtonIcon();
        }
        
        public void Draw2()
        {
            if(treePathChildCount == MySearchPath[treePathCount].Count)
            {
                SearchTool.SearchedPathCount += 1;
                treePathCount++;
                Walks.Clear();
                if (treePathCount == MySearchPath.Count)
                {
                    drawCorrentRoad = true;
                    SearchSpeedTick.Stop();
                    treePathCount = 0;
                    PreviousWalks.Clear();
                    MySearchPath.Clear();
                    canvascontroll.Invalidate();
                    return;
                }
                else
                {
                    treePathChildCount = 0;
                    drawPath = true;
                }
            }
            if (drawPath == true)
            {
                drawPath = false;
                if (PreviousWalks.Count > 1)
                {
                    while (treePathChildCount < MySearchPath[treePathCount].Count)
                    {
                        bool passedNode = false;
                        foreach (var passed in PreviousWalks)
                        {
                            if (MySearchPath[treePathCount][treePathChildCount] == passed)
                            {
                                Walks.Add(passed);
                                treePathChildCount += 1;
                                passedNode = true;
                                break;
                            }
                            if (treePathChildCount == MySearchPath[treePathCount].Count - 1)
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
            if (treePathChildCount < MySearchPath[treePathCount].Count)
            {
                Walks.Add(MySearchPath[treePathCount][treePathChildCount]);
                PreviousWalks.Add(MySearchPath[treePathCount][treePathChildCount]);
                treePathChildCount += 1;
                canvascontroll.Invalidate();
            }
        }

        private async void Info_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            MessageDialog showDialog = new MessageDialog("Left Click To Add Node.\nRight Click to Remove Node/Line.", "Information");
            showDialog.Commands.Add(new UICommand("Yes"));
            await showDialog.ShowAsync();
        }

        private void StopAnimation()
        {
            SearchSpeedTick.Stop();
            

            treePathCount = 0;
            treePathChildCount = 0;
            SearchTool.SearchedPathCount = 0;
            drawPath = true;
            drawCorrentRoad = false;
            
            Walks.Clear();
            AvaiableRoads.Clear();
            MySearchPath.Clear();
            PreviousWalks.Clear();
            canvascontroll.Invalidate();
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
                selectedNode.ConnectedNodes.Add(pSelectedNode);
                pSelectedNode.ConnectedNodes.Add(selectedNode);
                Line line = new Line()
                {
                    Point1 = this.selectedNode,
                    Point2 = this.pSelectedNode
                };
                Lines.Add(line);
                this.selectedNode = null;
                this.pSelectedNode = null;
            }
        }

        private void DissConnectTwoNode(Line line)
        {
            line.Point1.ConnectedNodes.Remove(line.Point2);
            line.Point2.ConnectedNodes.Remove(line.Point1);
            Lines.Remove(line);
        }

        private void GetAvailableNodeToConnect(Node sNode)
        {
            availableNodeToConnect.Clear();
            if (sNode != null && SearchTool.ConnectingNodeTogleEnabled)
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
                foreach (var connectedNode in sNode.ConnectedNodes)
                {
                    availableNodeToConnect.Remove(connectedNode);
                }
            }
        }

        /// <summary>
        /// Get Copy of the nodes from the selected Map
        /// </summary>
        /// <returns></returns>
        private List<Node> GetNodes(List<Node> nodes)
        {
            Node newNode;
            List<Node> newNodes = new List<Node>();
            foreach (var node in nodes)
            {
                newNode = new Node();
                newNode.X = node.X;
                newNode.Y = node.Y;
                newNode.NodeName = node.NodeName;
                newNode.StartPoint = node.StartPoint;
                newNode.GoolPoint = node.GoolPoint;
                newNode.ConnectedNodes = new List<Node>();
                newNodes.Add(newNode);
            }
            // loop throw all newNodes to add the connected nodes that have the same refernce
            for (int i = 0; i < newNodes.Count; i++)
            {
                foreach (var child in nodes[i].ConnectedNodes)
                {
                    var subNode = newNodes.Where(n => n.NodeName == child.NodeName).FirstOrDefault();
                    newNodes[i].ConnectedNodes.Add(subNode);
                }
            }

            return newNodes;
        }

        private void CleanCustomMapTool()
        {
            selectedNode = null;
            MapValidation_TextBlock.Text = string.Empty;
            MapValidation_TextBlock.Visibility = Visibility.Collapsed;
            Lines.Clear();
        }

        private void RemoveLine(Node node)
        {
            for(int i = 0; i < Lines.Count; i++)
            {
                if(Lines[i].Point2 == node || Lines[i].Point1 == node)
                {
                    Lines.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdatePlayButtonIcon()
        {
            if(SearchSpeedTick.IsEnabled)
            {
                play.Symbol = Symbol.Pause;
            }
            else
            {
                play.Symbol = Symbol.Play;
            }
        }
    }
}