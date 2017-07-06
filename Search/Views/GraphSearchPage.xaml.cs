
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Newtonsoft.Json;
using Search.Models.GraphSearch;
using Search.ViewModel.GraphSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public SearchToolViewModel SearchTool { get; set; }

        #region feilds

        bool drawPath, drawCorrentRoad, drawAnimation;
        int speed, treePathCount, treePathChildCount, newNodeNameCount;
        float canvasWidth, canvasHeight, blockWidth, blockHeight, circleRadius, canvasWidthMargin, canvasHeightMargin;
        float[] xAxis, yAxis;

        Node sL, gL;
        Node selectedNode, pSelectedNode;
        List<Node> nodes = new List<Node>();
        List<Map> JsonMaps;
        HashSet<Node> busyLoactions = new HashSet<Node>();

        ObservableCollection<Road> CorrectPaths { get; set; } = new ObservableCollection<Road>();
        ObservableCollection<List<Node>> SearchedPaths { get; set; } = new ObservableCollection<List<Node>>();
        List<List<Node>> AnimationsWalks { get; set; } = new List<List<Node>>();
        HashSet<Node> PreviousWalks { get; set; } = new HashSet<Node>();
        List<Node> Walks { get; set; } = new List<Node>();

        List<string> letters;
        List<string> removedLetters = new List<string>();
        List<Line> Lines = new List<Line>();

        DispatcherTimer SearchSpeedTick = new DispatcherTimer();

        #endregion

        public GraphSearchPage()
        {
            this.InitializeComponent();
            SearchTool = new SearchToolViewModel("DEPTH FIRST", "X1");
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            SearchTool.animateMoveChanged += SearchTool_animateMoveChanged;
            CorrectPaths.CollectionChanged += AvaiableRoads_CollectionChanged;
            SearchedPaths.CollectionChanged += MySearchPath_CollectionChanged;
        }

        private void DepthFirstSearch(Road road)
        {
            if (road.HeadNode.GoolPoint)
            {
                CorrectPaths.Add(road);
                SearchedPaths.Add(road.PassedRoad);
                return;
            }
            // mabe we dont need this one
            else if (road.PossibleNodes.Count <= 0)
            {
                SearchedPaths.Add(road.PassedRoad);
                return;
            }
            // if there is connected node
            else
            {
                // check if we already find one path if we do roll back
                if (CorrectPaths.Count <= 0)
                {
                    while (road.PossibleNodes.Count != 0 && CorrectPaths.Count <= 0)
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
            if (road.PossibleRoads.Count > 0)
            {
                // check the extended roads
                foreach (var extendedroad in road.PossibleRoads)
                {
                    SearchedPaths.Add(extendedroad.PassedRoad);
                    if (extendedroad.HeadNode.GoolPoint)
                    {
                        CorrectPaths.Add(extendedroad);
                        return;
                    }
                }
                if (CorrectPaths.Count <= 0)
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

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var textformat = new CanvasTextFormat() { FontSize = circleRadius, WordWrapping = CanvasWordWrapping.NoWrap, FontFamily = "Arial" };
            textformat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
            textformat.VerticalAlignment = CanvasVerticalAlignment.Center;

            if (SearchTool.ShowMap)
            {
                // Draw the Map Path
                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = 0; j < nodes[i].ConnectedNodes.Count; j++)
                    {
                        args.DrawingSession.DrawLine(xAxis[nodes[i].X], yAxis[nodes[i].Y], xAxis[nodes[i].ConnectedNodes[j].X], yAxis[nodes[i].ConnectedNodes[j].Y], Colors.Gray, circleRadius / 8);
                    }
                }
                // Draw the Corrent Path
                if (SearchTool.FindedCorrectPath && drawCorrentRoad)
                {
                    for (int i = 1; i < CorrectPaths[0].PassedRoad.Count; i++)
                    {
                        args.DrawingSession.DrawLine(xAxis[CorrectPaths[0].PassedRoad[i].X], yAxis[CorrectPaths[0].PassedRoad[i].Y],
                        xAxis[CorrectPaths[0].PassedRoad[i - 1].X], yAxis[CorrectPaths[0].PassedRoad[i - 1].Y], Colors.Green, circleRadius / 4);

                        if (!drawAnimation)
                        {
                            args.DrawingSession.DrawText(i.ToString(), xAxis[CorrectPaths[0].PassedRoad[i - 1].X] + circleRadius + 3, yAxis[CorrectPaths[0].PassedRoad[i - 1].Y] - circleRadius - 3, Colors.White, textformat);
                            if (i == CorrectPaths[0].PassedRoad.Count - 1)
                            {
                                args.DrawingSession.DrawText((i + 1).ToString(), xAxis[CorrectPaths[0].PassedRoad[i].X] + circleRadius + 3, yAxis[CorrectPaths[0].PassedRoad[i].Y] - circleRadius - 3, Colors.White, textformat);
                            }
                        }
                    }
                }
                // Draw the Searching Path
                if (SearchTool.IsAnimationAvailable && drawAnimation)
                {
                    for (int i = 1; i < AnimationsWalks[SearchTool.AnimateMoveNumber].Count; i++)
                    {
                        if (i != AnimationsWalks[SearchTool.AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawLine(xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y], xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y], Colors.OrangeRed, circleRadius / 10);
                        }
                        else
                        {
                            args.DrawingSession.DrawLine(xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y], xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y], Colors.Yellow, circleRadius / 10);
                        }
                        args.DrawingSession.DrawText((i).ToString(), xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X] + circleRadius + 3, yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y] - circleRadius - 3, Colors.White, textformat);
                        args.DrawingSession.DrawText((i + 1).ToString(), xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X] + circleRadius + 3, yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y] - circleRadius - 3, Colors.White, textformat);

                        if (i == AnimationsWalks[SearchTool.AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawCircle(xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y], circleRadius, Colors.OrangeRed, circleRadius / 4);
                            args.DrawingSession.DrawCircle(xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X], yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y], circleRadius, Colors.YellowGreen, circleRadius / 4);
                        }
                    }
                }
                // Draw the Nodes
                for (int i = 0; i < nodes.Count; i++)
                {
                    args.DrawingSession.FillCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Black);
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
            else
            {
                // Draw the y/x axis
                for (int i = 0; i < yAxis.Length; i++)
                {
                    args.DrawingSession.DrawLine(xAxis[0], yAxis[i], xAxis[xAxis.Length - 1], yAxis[i], Colors.Black, circleRadius / 20);
                }
                for (int i = 0; i < xAxis.Length; i++)
                {
                    args.DrawingSession.DrawLine(xAxis[i], yAxis[0], xAxis[i], yAxis[yAxis.Length - 1], Colors.Black, circleRadius / 20);
                }
                // Draw the Map Path
                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = 0; j < nodes[i].ConnectedNodes.Count; j++)
                    {
                        args.DrawingSession.DrawLine(xAxis[nodes[i].X], yAxis[nodes[i].Y], xAxis[nodes[i].ConnectedNodes[j].X], yAxis[nodes[i].ConnectedNodes[j].Y], Colors.Gray, circleRadius / 8);
                    }
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
                // Draw the Nodes
                for (int i = 0; i < nodes.Count; i++)
                {
                    args.DrawingSession.FillCircle(xAxis[nodes[i].X], yAxis[nodes[i].Y], circleRadius, Colors.Black);
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

                if (selectedNode != null)
                {
                    if (SearchTool.ConnectingNodeTogleEnabled)
                    {
                        foreach (var node in nodes)
                        {
                            bool busy = false;
                            foreach (var busyNode in busyLoactions)
                            {
                                if (node == busyNode)
                                {
                                    busy = true;
                                    break;
                                }
                            }
                            if (!busy)
                                args.DrawingSession.DrawCircle(xAxis[node.X], yAxis[node.Y], circleRadius, Colors.Green, circleRadius / 6);
                        }
                    }
                    args.DrawingSession.DrawCircle(xAxis[selectedNode.X], yAxis[selectedNode.Y], circleRadius, Colors.White, circleRadius / 6);
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

            xAxis = new float[10] { canvasWidthMargin, blockWidth + canvasWidthMargin, (2 * blockWidth) + canvasWidthMargin, (3 * blockWidth) + canvasWidthMargin, (4 * blockWidth) + canvasWidthMargin, (5 * blockWidth) + canvasWidthMargin, (6 * blockWidth) + canvasWidthMargin, (7 * blockWidth) + canvasWidthMargin, (8 * blockWidth) + canvasWidthMargin, (9 * blockWidth) + canvasWidthMargin };
            yAxis = new float[5] { canvasHeightMargin, blockHeight + canvasHeightMargin, (2 * blockHeight) + canvasHeightMargin, (3 * blockHeight) + canvasHeightMargin, (4 * blockHeight) + canvasHeightMargin };

        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            #region
            if (sL != null && gL != null && sL != gL)
            {
                ClearSearchAndAnimation();
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
                }

                if (SearchTool.IsAnimationAvailable)
                {
                    BuildAnimationPaths();
                    //foreach (var item in AnimationsWalks)
                    //{
                    //    foreach (var item2 in item)
                    //    {
                    //        Debug.Write(item2.NodeName + " ");
                    //    }
                    //    Debug.WriteLine(" ");
                    //}
                    startAnimation();
                }
            }
            #endregion
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
                    if (selectedNode != null && pSelectedNode != null && selectedNode != pSelectedNode)
                    {
                        GetAvailableNodeToConnect2(pSelectedNode);
                        ConnectTwoNodeAndBetween(selectedNode, pSelectedNode);
                        selectedNode = null;
                        pSelectedNode = null;
                    }
                    else if (exist && selectedNode == pSelectedNode)
                    {
                        selectedNode = null;
                    }
                }
                if (SearchTool.ConnectingNodeTogleEnabled)
                {
                    GetAvailableNodeToConnect2(selectedNode);
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
                            if (selectedNode == node)
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
            SearchTool.AnimateMoveNumber++;
        }

        private void ConnectNodes_Button_Click(object sender, RoutedEventArgs e)
        {
            GetAvailableNodeToConnect2(selectedNode);
            canvascontroll.Invalidate();
        }

        private void AddNewMap_ButtonClick(object sender, RoutedEventArgs e)
        {
            ClearSearchAndAnimation();
            SearchTool.SelectedMap = null;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            SearchTool.SelectedMap = SearchTool.Maps[0];
        }
        
        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SearchSpeedTick.IsEnabled)
                debugAnimation();
            else
            {
                if (SearchTool.AnimateMoveNumber == AnimationsWalks.Count && !drawAnimation)
                    SearchTool.AnimateMoveNumber = 0;

                startAnimation();
            }

        }

        private void PreviousStep_Button_Click(object sender, RoutedEventArgs e)
        {
            if (AnimationsWalks.Count > 0)
            {
                debugAnimation();
                SearchTool.AnimateMoveNumber--;
            }
        }

        private void NextStep_Button_Click(object sender, RoutedEventArgs e)
        {
            SearchTool.AnimateMoveNumber++;
        }

        private void Speed_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTool.SelectedSearchSpeed == "X.5")
            {
                speed = 1000;
                SearchTool.SelectedSearchSpeed = "X1";
            }
            else if (SearchTool.SelectedSearchSpeed == "X1")
            {
                speed = 500;
                SearchTool.SelectedSearchSpeed = "X2";
            }
            else if (SearchTool.SelectedSearchSpeed == "X2")
            {
                speed = 250;
                SearchTool.SelectedSearchSpeed = "X4";
            }
            else if (SearchTool.SelectedSearchSpeed == "X4")
            {
                speed = 2000;
                SearchTool.SelectedSearchSpeed = "X.5";
            }
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, speed);
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
                    var newMap = new Map() { Name = SearchTool.NewMapNameTextBox, Nodes = nodes.OrderBy(c => c.NodeName).ToList(), IsDeleteEnabled = true };
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

        private async void Tree_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(TreeVisuals), SearchedPaths);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        private async void DeleteMap_Button_ClickAsync(object sender, RoutedEventArgs e)
        {

            try
            {
                var button = (Button)sender;
                button.IsEnabled = false;
                var tag = button.Tag;
                var map = SearchTool.Maps.Where(m => m.Name == tag.ToString()).FirstOrDefault();
                if (SearchTool.SelectedMap == map)
                {
                    if (SearchTool.Maps.Count > 0)
                    {
                        SearchTool.SelectedMap = SearchTool.Maps[0];
                    }
                    else
                    {
                        SearchTool.SelectedMap = null;
                    }

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

                    try
                    {
                        var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Maps.json");
                        await FileIO.WriteTextAsync(file, jsonStringMap);
                    }
                    catch
                    {

                    }
                    button.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {

            }


        }

        private async void Info_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            MessageDialog showDialog = new MessageDialog("Left Click To Add Node.\nRight Click to Remove Node/Line.", "Information");
            showDialog.Commands.Add(new UICommand("Yes"));
            await showDialog.ShowAsync();
        }

        private void ComboBox_SearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation if it's drawings
            ClearSearchAndAnimation();
        }

        private void ComboBox_StartLocationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchTool.StartLocation))
            {
                // remove the Start logo from the old Start Node
                if (sL != null)
                {
                    sL.StartPoint = false;
                }
                // add the Start logo to new selected Node
                sL = nodes.Find(n => n.NodeName == SearchTool.StartLocation);
                sL.StartPoint = true;

                // stop drawing animation
                ClearSearchAndAnimation();
            }
        }

        private void ComboBox_GoolLocationSelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {


            // Get The New Select Gool Location
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(value))
            {
                // remove the Gool logo from the old Gool Node
                if (gL != null)
                {
                    gL.GoolPoint = false;
                }

                // add the Gool logo to new selected Node
                gL = nodes.Find(n => n.NodeName == value);
                gL.GoolPoint = true;

                // stop drawing animation
                ClearSearchAndAnimation();
            }
        }

        private void ComboBox_MapsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
            ClearSearchAndAnimation();
        }

        private async void Page_LoadedAsync(object sender, RoutedEventArgs e)
        {
            try
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
            catch
            {

            }
        }

        private void MySearchPath_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                PreviousWalks.Clear();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                SearchTool.IsAnimationAvailable = true;
            }
        }

        private void AvaiableRoads_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                SearchTool.FindedCorrectPath = true;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SearchedPaths.Clear();
                AnimationsWalks.Clear();
                Walks.Clear();
            }
        }

        private void SearchTool_animateMoveChanged(object sender, int animateMoveNumber)
        {
            if (animateMoveNumber == 0)
            {
                SearchTool.PreviousAnimateButtonEnabled = false;
            }
            else if (SearchTool.AnimateMoveNumber >= AnimationsWalks.Count)
            {
                SearchTool.NextAnimateButtonEnabled = false;
                drawCorrentRoad = true;
                stopAnimation();
            }
            else
            {
                SearchTool.PreviousAnimateButtonEnabled = true;
                SearchTool.NextAnimateButtonEnabled = true;
            }
            canvascontroll.Invalidate();
        }

        private void ClearSearchAndAnimation()
        {
            SearchSpeedTick.Stop();
            treePathCount = 0;
            treePathChildCount = 0;
            SearchTool.AnimateMoveNumber = 0;
            SearchTool.IsAnimationAvailable = false;
            SearchTool.FindedCorrectPath = false;
            SearchTool.NextAnimateButtonEnabled = false;
            drawPath = true;
            drawCorrentRoad = false;
            drawAnimation = false;

            CorrectPaths.Clear();

            canvascontroll.Invalidate();
        }

        private void MapSelectionChanged_Clear()
        {
            nodes = new List<Node>();
            sL = null;
            gL = null;
        }

        private void stopAnimation()
        {
            drawAnimation = false;
            play.Symbol = Symbol.Play;
            SearchSpeedTick.Stop();
        }

        private void startAnimation()
        {
            drawAnimation = true;
            play.Symbol = Symbol.Pause;
            SearchSpeedTick.Start();
        }

        private void debugAnimation()
        {
            drawAnimation = true;
            play.Symbol = Symbol.Play;
            SearchSpeedTick.Stop();
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

        private bool isFreeLocation(Node node)
        {
            bool free = true;
            foreach (var busyNode in busyLoactions)
            {
                if (busyNode == node)
                {
                    free = false;
                    break;
                }
            }
            return free;
        }

        private void ConnectTwoNode(Node selectedNode, Node pSelectedNode)
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

        private void ConnectTwoNodeAndBetween(Node selectedNode, Node pSelectedNode)
        {
            if (isFreeLocation(selectedNode))
            {
                Line line = new Line()
                {
                    Point1 = selectedNode,
                    Point2 = pSelectedNode
                };
                var linkedNodes = GetNodesOnLine(line);
                for (int i = 1; i < linkedNodes.Count; i++)
                {
                    ConnectTwoNode(linkedNodes[i - 1], linkedNodes[i]);
                }
            }
        }

        private List<Node> GetNodesOnLine(Line line)
        {
            var onLine = new List<Node>();
            foreach (var node in nodes)
            {
                if (line.IfPointOnTheLine(xAxis[node.X], yAxis[node.Y], circleRadius / 2, xAxis, yAxis))
                {
                    onLine.Add(node);
                }
            }
            onLine = onLine.OrderBy(s => s.X).OrderBy(s => s.Y).ToList();
            return onLine;
        }

        private void DissConnectTwoNode(Line line)
        {
            line.Point1.ConnectedNodes.Remove(line.Point2);
            line.Point2.ConnectedNodes.Remove(line.Point1);
            Lines.Remove(line);
            GetAvailableNodeToConnect2(selectedNode);
        }

        private void GetAvailableNodeToConnect2(Node sNode)
        {
            busyLoactions.Clear();
            if (sNode != null)
            {
                var connectedLines = Lines.Where(l => l.Point1 == sNode || l.Point2 == sNode).ToList();
                foreach (var line in Lines)
                {
                    var slop = line.GetSlop(xAxis, yAxis);
                    foreach (var subLine in connectedLines)
                    {
                        var subLineSlop = subLine.GetSlop(xAxis, yAxis);
                        if (Math.Round(slop, 3) == Math.Round(subLineSlop, 3) || float.IsNaN(slop) && float.IsNaN(subLineSlop))
                        {
                            if (HaveSameSlop(sNode, line.Point1, line.Point2))
                            {
                                busyLoactions.Add(line.Point1);
                                busyLoactions.Add(line.Point2);
                                break;
                            }
                        }
                    }
                }

            }
        }
        
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
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Point2 == node || Lines[i].Point1 == node)
                {
                    Lines.RemoveAt(i);
                    i--;
                }
            }
        }

        private bool HaveSameSlop(Node node1, Node node2, Node node3)
        {
            //(y1 - y2) * (x1 - x3) == (y1 - y3) * (x1 - x2)
            return (node1.Y - node2.Y) * (node1.X - node3.X) == (node1.Y - node3.Y) * (node1.X - node2.X);
        }

        private void BuildAnimationPaths()
        {
            if (treePathChildCount == SearchedPaths[treePathCount].Count)
            {
                // Start New Path
                treePathCount++;
                Walks.Clear();
                // Check if We Finished Building Exit
                if (treePathCount == SearchedPaths.Count)
                {
                    treePathCount = 0;
                    SearchedPaths.Clear();
                    return;
                }
                // if Not Start the Newed Path from the Start
                else
                {
                    treePathChildCount = 0;
                    drawPath = true;
                }
            }
            // Check if we Passed those Nodes
            if (drawPath == true)
            {
                drawPath = false;
                if (PreviousWalks.Count > 1)
                {
                    while (treePathChildCount < SearchedPaths[treePathCount].Count)
                    {
                        bool passedNode = false;
                        foreach (var passed in PreviousWalks)
                        {
                            if (SearchedPaths[treePathCount][treePathChildCount] == passed)
                            {
                                Walks.Add(passed);
                                treePathChildCount += 1;
                                passedNode = true;
                                break;
                            }
                            if (treePathChildCount == SearchedPaths[treePathCount].Count - 1)
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
            if (treePathChildCount < SearchedPaths[treePathCount].Count)
            {
                Walks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                PreviousWalks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                treePathChildCount += 1;
                AnimationsWalks.Add(new List<Node>(Walks));
                BuildAnimationPaths();
            }
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

        private void DrawPath()
        {
            if (treePathChildCount == -1)
            {
                Walks.Clear();
                drawPath = true;
                treePathCount += 1;
                SearchTool.SearchedPathCount += 1;

                if (treePathCount == SearchedPaths.Count)
                {
                    drawCorrentRoad = true;
                    SearchSpeedTick.Stop();
                    treePathCount = 0;
                    PreviousWalks.Clear();
                    canvascontroll.Invalidate();
                    return;
                }
            }
            if (treePathCount < SearchedPaths.Count && drawPath == true)
            {
                treePathChildCount = SearchedPaths[treePathCount].Count - 1;
                drawPath = false;
                if (PreviousWalks.Count > 1)
                {
                    while (treePathChildCount > 0)
                    {
                        bool passedNode = false;
                        foreach (var passed in PreviousWalks)
                        {
                            if (SearchedPaths[treePathCount][treePathChildCount].NodeName == passed.NodeName)
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
            if (treePathCount != SearchedPaths.Count && treePathChildCount != -1)
            {
                Walks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                PreviousWalks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                treePathChildCount -= 1;
                canvascontroll.Invalidate();
            }
        }

        public void Draw2()
        {
            if (treePathChildCount == SearchedPaths[treePathCount].Count)
            {
                SearchTool.SearchedPathCount += 1;
                treePathCount++;
                Walks.Clear();
                if (treePathCount == SearchedPaths.Count)
                {
                    drawCorrentRoad = true;
                    SearchSpeedTick.Stop();
                    treePathCount = 0;
                    PreviousWalks.Clear();
                    SearchedPaths.Clear();
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
                    while (treePathChildCount < SearchedPaths[treePathCount].Count)
                    {
                        bool passedNode = false;
                        foreach (var passed in PreviousWalks)
                        {
                            if (SearchedPaths[treePathCount][treePathChildCount] == passed)
                            {
                                Walks.Add(passed);
                                treePathChildCount += 1;
                                passedNode = true;
                                break;
                            }
                            if (treePathChildCount == SearchedPaths[treePathCount].Count - 1)
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
            if (treePathChildCount < SearchedPaths[treePathCount].Count)
            {
                Walks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                PreviousWalks.Add(SearchedPaths[treePathCount][treePathChildCount]);
                treePathChildCount += 1;
                canvascontroll.Invalidate();
                AnimationsWalks.Add(new List<Node>(Walks));
            }
        }
    }
}