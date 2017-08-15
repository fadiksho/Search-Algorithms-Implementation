
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

        bool drawCorrentRoad, drawAnimation, stop;
        int newNodeNameCount;
        float canvasWidth, canvasHeight, blockWidth, blockHeight,
            circleRadius, sCircleRadius, canvasWidthMargin, canvasHeightMargin,
            sThickness, mThickness, lThickness;
        CanvasTextFormat textformat = new CanvasTextFormat()
        {
            WordWrapping = CanvasWordWrapping.NoWrap,
            FontFamily = "Arial",
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };
        float[] xAxis, yAxis;
        Node sL, gL;
        Node selectedNode, pSelectedNode;
        List<Node> nodes = new List<Node>();
        List<Map> JsonMaps;
        HashSet<Node> busyLoactions = new HashSet<Node>();

        ObservableCollection<Road> CorrectPaths { get; set; } = new ObservableCollection<Road>();
        ObservableCollection<List<Node>> SearchedPaths { get; set; } = new ObservableCollection<List<Node>>();
        ObservableCollection<List<Node>> AnimationsWalks { get; set; } = new ObservableCollection<List<Node>>();
        HashSet<Node> PreviousWalks { get; set; } = new HashSet<Node>();
        List<Node> Walks { get; set; } = new List<Node>();

        List<string> letters = new List<string>();
        List<string> removedLetters = new List<string>();
        List<Line> Lines = new List<Line>();
        Stack<Node> stackNodes = new Stack<Node>();
        HashSet<Node> passedNodes = new HashSet<Node>();
        DispatcherTimer SearchSpeedTick = new DispatcherTimer();

        Random rnd = new Random();

        #endregion

        public GraphSearchPage()
        {
            this.InitializeComponent();
            SearchTool = new SearchToolViewModel("DEPTH FIRST");
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, SearchTool.SearchSpeed);
            SearchTool.animateMoveChanged += SearchTool_animateMoveChanged;
            CorrectPaths.CollectionChanged += CorrectPathss_CollectionChanged;
            SearchedPaths.CollectionChanged += MySearchPath_CollectionChanged;
            AnimationsWalks.CollectionChanged += AnimationsWalks_CollectionChanged;
        }

        private void DepthFirstSearch(Road road)
        {
            if (road.HeadNode.GoolPoint)
            {
                CorrectPaths.Add(road);
                SearchedPaths.Add(road.PassedRoad);
                return;
            }
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
            //return;
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
                        //return;
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

        private bool DepthFirstSearchWithFiltering(Node sNode, Node gNode)
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

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {   
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
                
                BuildAnimation3(SearchedPaths);
                for (int i = 1; i < AnimationsWalks.Count; i++)
                {
                    var walk = AnimationsWalks[i];
                    for (int j = i; j < AnimationsWalks.Count; j++)
                    {
                        if (i != j && walk.Count == AnimationsWalks[j].Count)
                        {
                            bool matched = true;
                            for (int k = walk.Count-1; k >= 0; k--)
                            {
                                if(walk[k].NodeName != AnimationsWalks[j][k].NodeName)
                                {
                                    matched = false;
                                    break;
                                }
                            }
                            if(matched)
                                AnimationsWalks.RemoveAt(j);
                        }
                    }
                }

                

                startAnimation();

                foreach (var item in SearchedPaths)
                {
                    foreach (var item2 in item)
                    {
                        Debug.Write(item2.NodeName + " ");
                    }
                    Debug.WriteLine(" ");
                }
                Debug.WriteLine("___________________________________________________________________________________________________________________");
                
            }
        }

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Defualt Mode
            if (SearchTool.ShowMap)
            {
                // Draw the Map Path
                drawMapPath(args, nodes, Colors.Gray, mThickness);
                // Draw the Corrent Path
                if (SearchTool.FindedCorrectPath && drawCorrentRoad)
                {
                    for (int i = 1; i < CorrectPaths[0].PassedRoad.Count; i++)
                    {
                        args.DrawingSession.DrawLine(
                            xAxis[CorrectPaths[0].PassedRoad[i].X],
                            yAxis[CorrectPaths[0].PassedRoad[i].Y],
                            xAxis[CorrectPaths[0].PassedRoad[i - 1].X],
                            yAxis[CorrectPaths[0].PassedRoad[i - 1].Y],
                            Colors.Green,
                            lThickness);

                        //if (!drawAnimation)
                        //{
                        //    args.DrawingSession.DrawText(
                        //        i.ToString(), 
                        //        xAxis[CorrectPaths[0].PassedRoad[i - 1].X] + circleRadius + 3,
                        //        yAxis[CorrectPaths[0].PassedRoad[i - 1].Y] - circleRadius - 3,
                        //        Colors.White,
                        //        textformat);

                        //    if (i == CorrectPaths[0].PassedRoad.Count - 1)
                        //    {
                        //        args.DrawingSession.DrawText(
                        //            (i + 1).ToString(), 
                        //            xAxis[CorrectPaths[0].PassedRoad[i].X] + circleRadius + 3,
                        //            yAxis[CorrectPaths[0].PassedRoad[i].Y] - circleRadius - 3,
                        //            Colors.White, 
                        //            textformat);
                        //    }
                        //}
                    }
                }
                // Draw the Searching Path
                if (SearchTool.IsAnimationAvailable && drawAnimation)
                {
                    for (int i = 1; i < AnimationsWalks[SearchTool.AnimateMoveNumber].Count; i++)
                    {
                        if (i != AnimationsWalks[SearchTool.AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawLine(
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y],
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X],
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y],
                                Colors.OrangeRed, 
                                lThickness);
                        }
                        else
                        {
                            args.DrawingSession.DrawLine(
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y], 
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X],
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y], 
                                Colors.Yellow,
                                lThickness);
                        }
                        args.DrawingSession.DrawText(
                            (i).ToString(),
                            xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X] + circleRadius + 3,
                            yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y] - circleRadius - 3,
                            Colors.White,
                            textformat);
                        args.DrawingSession.DrawText((i + 1).ToString(),
                            xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X] + circleRadius + 3,
                            yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y] - circleRadius - 3,
                            Colors.White,
                            textformat);

                        if (i == AnimationsWalks[SearchTool.AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawCircle(
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i - 1].Y], 
                                circleRadius,
                                Colors.YellowGreen, lThickness);
                            args.DrawingSession.DrawCircle(
                                xAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].X], 
                                yAxis[AnimationsWalks[SearchTool.AnimateMoveNumber][i].Y], 
                                circleRadius, 
                                Colors.Yellow, 
                                lThickness);

                        }
                    }
                }
                // Draw the Nodes
                drawNodes(args, nodes, circleRadius, textformat);
            }
            // Design Mode
            else
            {
                // Draw the y/x axis
                drawCoordinateGeometry(args, xAxis, yAxis, Colors.Black, sThickness);
                // Draw the Map Path
                drawMapPath(args, nodes, Colors.Gray, mThickness);
                // Draw free locations 
                if (nodes.Count < 26)
                {
                    drawFreeLocations(args, xAxis, yAxis, Lines, Colors.SteelBlue, sCircleRadius, mThickness);
                }
                // Draw the Nodes
                drawNodes(args, nodes, circleRadius, textformat);
                // Draw the selected Nodes and Available Nodes That can be Connected
                if (selectedNode != null)
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
                        {
                            args.DrawingSession.DrawCircle(
                                xAxis[node.X],
                                yAxis[node.Y],
                                circleRadius,
                                Colors.Green,
                                mThickness);
                        } 
                    }

                    args.DrawingSession.DrawCircle(
                        xAxis[selectedNode.X],
                        yAxis[selectedNode.Y],
                        circleRadius, 
                        Colors.White, 
                        mThickness);
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
                circleRadius = blockWidth / 5;
            else
                circleRadius = blockHeight / 5;

            sThickness = circleRadius / 10;
            mThickness = circleRadius / 8;
            lThickness = circleRadius / 4;
            sCircleRadius = circleRadius - sThickness;
            textformat.FontSize = circleRadius;
            xAxis = new float[10] { canvasWidthMargin, blockWidth + canvasWidthMargin, (2 * blockWidth) + canvasWidthMargin, (3 * blockWidth) + canvasWidthMargin, (4 * blockWidth) + canvasWidthMargin, (5 * blockWidth) + canvasWidthMargin, (6 * blockWidth) + canvasWidthMargin, (7 * blockWidth) + canvasWidthMargin, (8 * blockWidth) + canvasWidthMargin, (9 * blockWidth) + canvasWidthMargin };
            yAxis = new float[5] { canvasHeightMargin, blockHeight + canvasHeightMargin, (2 * blockHeight) + canvasHeightMargin, (3 * blockHeight) + canvasHeightMargin, (4 * blockHeight) + canvasHeightMargin };

            Line.XAxis = xAxis;
            Line.YAxis = yAxis;
        }

        private void canvascontroll_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SearchTool.SelectedMap == null)
            {
                var xTappedLoacation = (float)e.GetPosition(canvascontroll).X;
                var yTappedLoacation = (float)e.GetPosition(canvascontroll).Y;
                var xyIndex = GetXYAxis(xTappedLoacation, yTappedLoacation);
                bool exist = false;
                // Select the node if its exist on the board at the tap location
                if (xyIndex.Item1 != -1)
                {
                    Node node = getIfExistOnMap(xyIndex.Item1, xyIndex.Item2);
                    if (node != null)
                    {
                        exist = true;
                        pSelectedNode = selectedNode;
                        selectedNode = node;
                    }
                    // Add new node to the board if it's not exist at the tap location
                    if (!exist)
                    {
                        if (newNodeNameCount < letters.Count || removedLetters.Count > 0)
                        {
                            bool showFreeLocaion = true;
                            foreach (var line in Lines)
                            {
                                if (line.IfPointOnTheLine(xAxis[xyIndex.Item1], yAxis[xyIndex.Item2], circleRadius / 2))
                                {
                                    showFreeLocaion = false;
                                }
                            }
                            if (showFreeLocaion)
                            {
                                selectedNode = new Node();
                                selectedNode.X = xyIndex.Item1;
                                selectedNode.Y = xyIndex.Item2;

                                selectedNode.NodeName = getLetter();

                                selectedNode.ConnectedNodes = new List<Node>();
                                nodes.Add(selectedNode);
                                FillAvailableNodeToConnect(selectedNode);
                            }
                        }
                        else
                            selectedNode = null;
                    }
                    // Connect Two or More Nodes
                    else
                    {
                        if (selectedNode != null && pSelectedNode != null && selectedNode != pSelectedNode)
                        {
                            bool sucssefullConnected = ConnectTwoNodeAndBetween(selectedNode, pSelectedNode);
                            if (sucssefullConnected)
                                selectedNode = pSelectedNode;
                        }
                        else if (exist && selectedNode == pSelectedNode)
                        {
                            selectedNode = null;
                        }
                        FillAvailableNodeToConnect(selectedNode);
                    }
                }
                else
                {
                    selectedNode = null;
                }
                canvascontroll.Invalidate();
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
                    if (line.IfPointOnTheLine(xTappedLoacation, yTappedLoacation, circleRadius / 2))
                    {
                        DissConnectTwoNode(line);
                        canvascontroll.Invalidate();
                        break;
                    }
                }
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
            FillAvailableNodeToConnect(selectedNode);
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
            SearchTool.NewMapNameValidation = "";
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
            SearchTool.ChangeSpeed();
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, SearchTool.SearchSpeed);
        }

        private async void Save_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            bool exist = false;
            if (!string.IsNullOrWhiteSpace(SearchTool.NewMapName))
            {
                foreach (var mapName in SearchTool.Maps)
                {
                    if (mapName.Name == SearchTool.NewMapName)
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        nodes[i].ConnectedNodes = nodes[i].ConnectedNodes.OrderBy(c => c.X).ThenBy(c => c.Y).ToList();
                    }
                    var newMap = new Map() { Name = SearchTool.NewMapName, Nodes = nodes.OrderBy(c => c.NodeName).ToList(), IsDeleteEnabled = true };
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
                    SearchTool.NewMapNameValidation = "";
                    SearchTool.NewMapName = "";
                }
                else
                {
                    SearchTool.NewMapNameValidation = "This Name is exist in your Maps!";
                }
            }
            else
            {
                SearchTool.NewMapNameValidation = "Map name should not be empty!";
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
            MessageDialog showDialog = new MessageDialog("Left Click To Add Node.\nRight Click to Remove Node/Line.\nThe white circle indicates the selected node.\nThe green circle can connect to white circle.", "Information");
            showDialog.Commands.Add(new UICommand("Yes"));
            await showDialog.ShowAsync();
        }

        private void ComboBox_SearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // stop drawing animation if it's drawings
            var selectedSearch = searchType_ComboBox.SelectedItem.ToString();
            if(selectedSearch != SearchTool.SelectedSearchType)
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
            }
            // if we creating a new map
            else if (SearchTool.SelectedMap == null)
            {
                newNodeNameCount = 0;
                CleanCustomMapTool();
                letters = GetAlphabeticallyLetters();
            }
            ClearSearchAndAnimation();
        }

        private async void Page_LoadedAsync(object sender, RoutedEventArgs e)
        {
            var watch3 = Stopwatch.StartNew();
            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\Maps.json";
                if (!File.Exists(path))
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

                JsonMaps = JsonConvert.DeserializeObject<List<Map>>(File.ReadAllText(path), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                SearchTool.Maps = new ObservableCollection<Map>(JsonMaps);
            }
            catch
            {
                SearchTool.Maps = new ObservableCollection<Map>(GetDefualtMaps());
            }
            finally
            {
                SearchTool.SelectedMap = SearchTool.Maps[0];
            }
            watch3.Stop();
            Debug.WriteLine($"Loaded Time : {watch3.ElapsedMilliseconds}");
        }

        private void MySearchPath_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                PreviousWalks.Clear();
            }
        }

        private void CorrectPathss_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
        
        private void AnimationsWalks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                SearchTool.IsAnimationAvailable = true;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SearchTool.IsAnimationAvailable = false;
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
            SearchTool.AnimateMoveNumber = 0;
            SearchTool.FindedCorrectPath = false;
            SearchTool.NextAnimateButtonEnabled = false;
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
            SearchTool.AnimationPlayButtonToopTip = "Play Animation";
            SearchSpeedTick.Stop();
        }

        private void startAnimation()
        {
            drawAnimation = true;
            play.Symbol = Symbol.Pause;
            SearchTool.AnimationPlayButtonToopTip = "Pause Animation";
            SearchSpeedTick.Start();
        }

        private void debugAnimation()
        {
            drawAnimation = true;
            play.Symbol = Symbol.Play;
            SearchTool.AnimationPlayButtonToopTip = "Play Animation";
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

        private bool ConnectTwoNodeAndBetween(Node selectedNode, Node pSelectedNode)
        {
            bool succefullConnected = false;
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
                    succefullConnected = true;
                }
                if (Lines.Count > 1)
                {
                    for (int i = 1; i < Lines.Count; i++)
                    {
                        bool test = CheckIfTwoLineIntersect(Lines[i - 1], Lines[i]);
                    }
                }
                return succefullConnected;
            }
            return succefullConnected;
        }

        private List<Node> GetNodesOnLine(Line line)
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

        private Node getIfExistOnMap(int x, int y)
        {
            foreach (var node in nodes)
            {
                if (node.X == x && node.Y == y)
                    return node;
            }
            return null;
        }

        private void DissConnectTwoNode(Line line)
        {
            line.Point1.ConnectedNodes.Remove(line.Point2);
            line.Point2.ConnectedNodes.Remove(line.Point1);
            Lines.Remove(line);
            FillAvailableNodeToConnect(selectedNode);
        }

        private bool CheckIfTwoLineIntersect(Line line1, Line line2)
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

        private void FillAvailableNodeToConnect(Node sNode)
        {
            busyLoactions.Clear();
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
                                    busyLoactions.Add(line.Point1);
                                    busyLoactions.Add(line.Point2);
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

        private void BuildAnimation3(ObservableCollection<List<Node>> searchedPath)
        {
            List<List<Node>> Animation = new List<List<Node>>();
            List<Node> previoseMove = null;
            for (int i = 0; i < searchedPath.Count; i++)
            {
                List<List<Node>> move = null;
                if (previoseMove == null)
                {
                    move = CreatAnimationFromPath(searchedPath[i], 0);
                }
                else
                {
                    int endFilterIndex = (previoseMove.Count > searchedPath[i].Count) ? searchedPath[i].Count : previoseMove.Count;
                    bool finishFiltring = false;
                    for (int j = 0; j < endFilterIndex; j++)
                    {
                        if (searchedPath[i][j] != previoseMove[j])
                        {
                            move = CreatAnimationFromPath(searchedPath[i], j);
                            finishFiltring = true;
                            break;
                        }
                    }
                    if (!finishFiltring)
                        move = CreatAnimationFromPath(searchedPath[i], searchedPath[i].Count - 1);
                }
                previoseMove = move[move.Count - 1];
                foreach (var item in move)
                {
                    AnimationsWalks.Add(item);
                }
            }
        }

        private List<List<Node>> CreatAnimationFromPath(List<Node> nodes, int startIndex)
        {
            List<List<Node>> path = new List<List<Node>>();
            for (int i = startIndex; i < nodes.Count; i++)
            {
                List<Node> move = new List<Node>();

                for (int j = 0; j <= i; j++)
                {
                    move.Add(nodes[j]);
                }
                path.Add(move);
            }
            return path;
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

        private string getLetter()
        {
            if (removedLetters.Count > 0)
            {
                return GetFromRemovedLetters();
            }
            else
            {
                return letters[newNodeNameCount++];
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


        #region Drawing Method

        private void drawMapPath(
            CanvasDrawEventArgs args, List<Node> nodess, Color lineColor, 
            float lineThickness)
        {
            for (int i = 0; i < nodess.Count; i++)
            {
                for (int j = 0; j < nodess[i].ConnectedNodes.Count; j++)
                {
                    args.DrawingSession.DrawLine(
                        xAxis[nodess[i].X],
                        yAxis[nodess[i].Y], xAxis[nodess[i].ConnectedNodes[j].X],
                        yAxis[nodess[i].ConnectedNodes[j].Y], lineColor,
                        lineThickness);
                }
            }
        }

        private void drawFreeLocations(
            CanvasDrawEventArgs args, float[] xAxis, float[] yAxis,
            List<Line> Lines, Color circleColor, float circleRadius, float strokeWidth)
        {
            foreach (var x in xAxis)
            {
                foreach (var y in yAxis)
                {
                    bool showFreeLocaion = true;
                    foreach (var line in Lines)
                    {
                        if (line.IfPointOnTheLine(x, y, circleRadius / 2))
                        {
                            showFreeLocaion = false;
                        }
                    }
                    if (showFreeLocaion)
                        args.DrawingSession.DrawCircle(
                            x, y,
                            circleRadius,
                            circleColor,
                            strokeWidth);
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

        private void drawNodes(
            CanvasDrawEventArgs args, List<Node> nodes, float circleRadius, 
            CanvasTextFormat textFormat)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                args.DrawingSession.FillCircle(
                    xAxis[nodes[i].X],
                    yAxis[nodes[i].Y],
                    circleRadius,
                    Colors.Black);
                args.DrawingSession.DrawText
                    (nodes[i].NodeName,
                    xAxis[nodes[i].X],
                    yAxis[nodes[i].Y],
                    Colors.White,
                    textFormat);

                args.DrawingSession.DrawText(
                    string.Format("({0},{1})",
                    nodes[i].X.ToString(),
                    nodes[i].Y.ToString()),
                    xAxis[nodes[i].X] + circleRadius,
                    yAxis[nodes[i].Y] - circleRadius,
                    Colors.White,
                    textFormat);

                if (nodes[i].StartPoint)
                {
                    args.DrawingSession.DrawCircle(
                        xAxis[nodes[i].X],
                        yAxis[nodes[i].Y],
                        circleRadius,
                        Colors.Red,
                        sThickness);
                }
                if (nodes[i].GoolPoint)
                {
                    args.DrawingSession.DrawCircle(
                        xAxis[nodes[i].X],
                        yAxis[nodes[i].Y],
                        circleRadius,
                        Colors.Green,
                        sThickness);
                }
            }
        }
        #endregion

        private ObservableCollection<List<Node>> BuildAnimation2(ObservableCollection<List<Node>> searchedPath)
        {
            List<List<Node>> Animation = new List<List<Node>>();
            List<Node> previoseMove = null;
            for (int i = 0; i < searchedPath.Count; i++)
            {
                List<List<Node>> move = null;
                if (previoseMove == null)
                {
                    move = CreatAnimationFromPath(searchedPath[i], 0);
                }
                else
                {
                    int endFilterIndex = (previoseMove.Count > searchedPath[i].Count) ? searchedPath[i].Count : previoseMove.Count;
                    bool finishFiltring = false;
                    for (int j = 0; j < endFilterIndex; j++)
                    {
                        if (searchedPath[i][j] != previoseMove[j])
                        {
                            move = CreatAnimationFromPath(searchedPath[i], j);
                            finishFiltring = true;
                            break;
                        }
                    }
                    if (!finishFiltring)
                        move = CreatAnimationFromPath(searchedPath[i], searchedPath[i].Count - 1);
                }
                previoseMove = move[move.Count - 1];
                Animation.AddRange(move);
            }
            return new ObservableCollection<List<Node>>(Animation);
        }
        
    }
}