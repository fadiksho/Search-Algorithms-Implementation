using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Newtonsoft.Json;
using Search.Models;
using Search.Models.GraphSearch;
using Search.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Search.ViewModel.GraphSearch
{
    public class GraphSearchViewModels : ViewModelBase
    {
        public DesignMapHelper DesignMapHelper { get; set; }

        public GraphSearchViewModels()
        {
            selectedSearchType = "DEPTH FIRST";
            searchSpeed = 1000;
            searchSpeedIcon = "\uEC49";
            PlayIcon = Symbol.Play;

            DesignMapHelper = new DesignMapHelper();

            SearchSpeedTick = new DispatcherTimer();
            SearchSpeedTick.Tick += SearchSpeedTick_Tick;
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, searchSpeed);

            AnimateMoveChanged += SearchTool_animateMoveChanged;
            CorrectPaths.CollectionChanged += CorrectPathss_CollectionChanged;
            AnimationsWalks = new ObservableCollection<List<Node>>();

            DeleteMap_Command = new RelayCommand<object>(DeleteMapClickHandler);
            SearchCommand = new RelayCommand(SearchClickHandler, CanSearch);
            Tapped_Command = new RelayCommand<Point>(TappedCommandHandler, CanTapp);
            RightTapped_Command = new RelayCommand<Point>(RightTappedCommandHandler, CanTapp);
            SizeChanged_Command = new RelayCommand<Point>(SizeChangedHandler);
            AddNewMapClick_Command = new RelayCommand(AddNewMapClick_Handler);
            PlayAnimationClick_Command = new RelayCommand(PlayAnimationClick_Handler);
            PreviouseAnimtionStepClick_Command = new RelayCommand(PreviouseAnimtionStepClick_Handler);
            NextAnimationStepClick_Command = new RelayCommand(NextAnimationStepClick_Handler);
            AnimationSpeedClick_Command = new RelayCommand(AnimationSpeedClick_Handler);

            SaveNewMapButton_Command = new RelayCommand(SaveNewMapButton_HandlerAsync);
            CancelNewMapButton_Command = new RelayCommand(CancelNewMapButton_Handler);
            
            StartLocationsComboBoxSelection_Command = new RelayCommand(StartLocationsComboBoxSelection_Handler);
            GoalLocationsComboBoxSelection_Command = new RelayCommand(GoalLocationsComboBoxSelection_Handler);
            MapsComboBoxSlection_Command = new RelayCommand(MapsComboBoxSlection_Handler);
        }

        #region Grapth Search Fields

        bool drawCorrentRoad, drawAnimation;
        int searchSpeed, newNodeNameCount;

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
        Node sL, gL, selectedNode, pSelectedNode;
        List<Node> nodes = new List<Node>();
        List<Map> JsonMaps;

        ObservableCollection<List<Node>> CorrectPaths { get; set; } = new ObservableCollection<List<Node>>();
        List<List<Node>> SearchedPaths { get; set; } = new List<List<Node>>();

        ObservableCollection<List<Node>> animationWalks;
        ObservableCollection<List<Node>> AnimationsWalks
        {
            get { return animationWalks; }
            set
            {
                if (value != null && value.Count > 1)
                {
                    IsAnimationAvailable = true;
                }
                animationWalks = value;
            }
        }

        DispatcherTimer SearchSpeedTick;
        #endregion

        #region Commands
        
        public RelayCommand<object> DeleteMap_Command { get; set; }
        private async void DeleteMapClickHandler(object sender)
        {
            try
            {
                var button = (Button)sender;
                button.IsEnabled = false;
                var tag = button.Tag;
                var map = Maps.Where(m => m.Name == tag.ToString()).FirstOrDefault();
                if (SelectedMap == map)
                {
                    SelectedMap = Maps[0];
                }
                Maps.Remove(map);
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
                Debug.WriteLine(ex.Message);
            }
        }

        public RelayCommand SearchCommand { get; set; }
        private bool CanSearch()
        {
            Debug.WriteLine("Can Search Excuted!");
            if (!string.IsNullOrEmpty(StartLocation) &&
                !string.IsNullOrEmpty(GoolLocation) &&
                StartLocation != GoolLocation)
                return true;
            else
                return false;
        }
        private void SearchClickHandler()
        {
            ClearSearchAndAnimation();
            Road road = new Road() { HeadNode = sL, PassedRoad = new List<Node>() { sL } };
            //Firt all the connected Node are possible Roads
            foreach (var connectedNode in road.HeadNode.ConnectedNodes)
            {
                road.PossibleNodes.Enqueue(connectedNode);
            }
            // Depth First Search
            if (SelectedSearchType == SearchTypes[0])
            {
                DepthFirstSearch(road);
            }
            // Breadth First Search
            else if (SelectedSearchType == SearchTypes[1])
            {
                foreach (var possiblenode in road.PossibleNodes)
                {
                    Road extendedRoad = new Road
                    {
                        HeadNode = possiblenode
                    };
                    foreach (var passed in road.PassedRoad)
                    {
                        extendedRoad.PassedRoad.Add(passed);
                    }
                    extendedRoad.PassedRoad.Add(possiblenode);
                    road.PossibleRoads.Add(extendedRoad);
                }
                BreadthFirst(road);
            }

            AnimationsWalks = GetAnimationMoves(SearchedPaths);

            StartAnimation();
        }

        public RelayCommand<Point> Tapped_Command { get; set; }
        private void TappedCommandHandler(Point point)
        {
            var xTappedLoacation = (float)point.X;
            var yTappedLoacation = (float)point.Y;
            var xyIndex = GetXYAxis(xTappedLoacation, yTappedLoacation);
            bool exist = false;
            // Select the node if its exist on the board at the tap location
            if (xyIndex.Item1 != -1)
            {
                Node node = DesignMapHelper.GetNodeIfExistOnMap(xyIndex.Item1, xyIndex.Item2, nodes);
                if (node != null)
                {
                    exist = true;
                    pSelectedNode = selectedNode;
                    selectedNode = node;
                }
                // Add new node to the board if it's not exist at the tap location
                if (!exist)
                {
                    if (newNodeNameCount < DesignMapHelper.Letters.Count || DesignMapHelper.RemovedLetters.Count > 0)
                    {
                        bool showFreeLocaion = true;
                        foreach (var line in DesignMapHelper.Lines)
                        {
                            if (line.IfPointOnTheLine(xAxis[xyIndex.Item1], yAxis[xyIndex.Item2], circleRadius / 2))
                            {
                                showFreeLocaion = false;
                            }
                        }
                        if (showFreeLocaion)
                        {
                            selectedNode = new Node
                            {
                                X = xyIndex.Item1,
                                Y = xyIndex.Item2,

                                NodeName = GetLetter(),

                                ConnectedNodes = new List<Node>()
                            };
                            nodes.Add(selectedNode);
                            DesignMapHelper.FillAvailableNodeToConnect(selectedNode);
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
                        bool sucssefullConnected = DesignMapHelper.ConnectTwoNodeAndBetween(
                            selectedNode, pSelectedNode, nodes, xAxis, yAxis, circleRadius);
                        if (sucssefullConnected)
                            selectedNode = pSelectedNode;
                    }
                    else if (exist && selectedNode == pSelectedNode)
                    {
                        selectedNode = null;
                    }
                    DesignMapHelper.FillAvailableNodeToConnect(selectedNode);
                }
            }
            else
            {
                selectedNode = null;
            }

            OnUpdateCanvasUi();
        }

        public RelayCommand<Point> RightTapped_Command { get; set; }
        private void RightTappedCommandHandler(Point point)
        {
            var xTappedLoacation = (float)point.X;
            var yTappedLoacation = (float)point.Y;
            var xyIndex = GetXYAxis(xTappedLoacation, yTappedLoacation);

            // remove the node if it's exist on the board
            if (xyIndex.Item1 != -1)
            {
                foreach (var node in nodes)
                {
                    if (node.X == xyIndex.Item1 && node.Y == xyIndex.Item2)
                    {
                        DesignMapHelper.AddToRemovedList(node.NodeName);

                        // cut all the road that lead to this node
                        foreach (var subNode in node.ConnectedNodes)
                        {
                            subNode.ConnectedNodes.Remove(node);
                        }

                        DesignMapHelper.RemoveLine(node);
                        nodes.Remove(node);
                        if (selectedNode == node)
                            selectedNode = null;
                        OnUpdateCanvasUi();
                        break;
                    }
                }
            }
            // remove the line between the connected nodes
            foreach (var line in DesignMapHelper.Lines)
            {
                if (line.IfPointOnTheLine(xTappedLoacation, yTappedLoacation, circleRadius / 2))
                {
                    DesignMapHelper.DissConnectTwoNode(line);
                    DesignMapHelper.FillAvailableNodeToConnect(selectedNode);
                    OnUpdateCanvasUi();
                    break;
                }
            }

        }

        public RelayCommand<Point> SizeChanged_Command { get; set; }
        private void SizeChangedHandler(Point point)
        {
            canvasWidth = (float)point.X;
            canvasHeight = (float)point.Y;

            canvasWidthMargin = canvasWidth / 15;
            canvasHeightMargin = canvasHeight / 15;

            blockWidth = (canvasWidth - 2 * canvasWidthMargin) / 8;
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
            xAxis = new float[] { canvasWidthMargin, blockWidth + canvasWidthMargin, (2 * blockWidth) + canvasWidthMargin, (3 * blockWidth) + canvasWidthMargin, (4 * blockWidth) + canvasWidthMargin, (5 * blockWidth) + canvasWidthMargin, (6 * blockWidth) + canvasWidthMargin, (7 * blockWidth) + canvasWidthMargin, (8 * blockWidth) + canvasWidthMargin };
            yAxis = new float[5] { canvasHeightMargin, blockHeight + canvasHeightMargin, (2 * blockHeight) + canvasHeightMargin, (3 * blockHeight) + canvasHeightMargin, (4 * blockHeight) + canvasHeightMargin };

            Line.XAxis = xAxis;
            Line.YAxis = yAxis;
        }
        private bool CanTapp(Point point)
        {
            if (selectedMap != null)
                return false;

            return true;
        }

        public RelayCommand AddNewMapClick_Command { get; set; }
        private void AddNewMapClick_Handler()
        {
            ClearSearchAndAnimation();
            SelectedMap = null;
            NewMapName = "";
            NewMapNameValidation = "";

        }

        public RelayCommand PlayAnimationClick_Command { get; set; }
        private void PlayAnimationClick_Handler()
        {
            if (SearchSpeedTick.IsEnabled)
                DebugAnimation();
            else
            {
                if (AnimateMoveNumber == AnimationsWalks.Count && !drawAnimation)
                    AnimateMoveNumber = 0;

                StartAnimation();
            }

        }

        public RelayCommand PreviouseAnimtionStepClick_Command { get; set; }
        public void PreviouseAnimtionStepClick_Handler()
        {
            if (AnimationsWalks.Count > 0)
            {
                DebugAnimation();
                AnimateMoveNumber--;
            }
        }

        public RelayCommand NextAnimationStepClick_Command { get; set; }
        public void NextAnimationStepClick_Handler()
        {
            AnimateMoveNumber++;
        }

        public RelayCommand AnimationSpeedClick_Command { get; set; }
        public void AnimationSpeedClick_Handler()
        {
            if (searchSpeed == 1000)
            {
                SearchSpeedIcon = "\uEC4A";
                searchSpeed = 250;
            }
            else if (searchSpeed == 2000)
            {
                SearchSpeedIcon = "\uEC49";
                searchSpeed = 1000;
            }
            else if (searchSpeed == 250)
            {
                SearchSpeedIcon = "\uEC48";
                searchSpeed = 2000;
            }
            SearchSpeedTick.Interval = new TimeSpan(0, 0, 0, 0, searchSpeed);
        }

        public RelayCommand SaveNewMapButton_Command { get; set; }
        public async void SaveNewMapButton_HandlerAsync()
        {
            bool exist = false;
            if (!string.IsNullOrWhiteSpace(NewMapName))
            {
                foreach (var mapName in Maps)
                {
                    if (mapName.Name == NewMapName)
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
                    var newMap = new Map() { Name = NewMapName, Nodes = nodes.OrderBy(c => c.NodeName).ToList(), IsDeleteEnabled = true };
                    JsonMaps.Add(newMap);
                    Maps.Add(newMap);
                    var jsonStringMap = JsonConvert.SerializeObject(JsonMaps, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
                    var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Maps.json");
                    await FileIO.WriteTextAsync(file, jsonStringMap);
                    SelectedMap = newMap;

                    NewMapName = "";
                    NewMapNameValidation = "";
                }
                else
                {
                    NewMapNameValidation = "This Name is exist in your Maps!";
                }
            }
            else
            {
                NewMapNameValidation = "Map name should not be empty!";
            }
        }

        public RelayCommand CancelNewMapButton_Command { get; set; }
        public void CancelNewMapButton_Handler()
        {
            SelectedMap = Maps[0];
            NewMapNameValidation = "";
            NewMapName = "";
        }

        public RelayCommand StartLocationsComboBoxSelection_Command { get; set; }
        public void StartLocationsComboBoxSelection_Handler()
        {
            if (!string.IsNullOrEmpty(StartLocation))
            {
                // Remove the Start logo from the old Start Node
                if (sL != null)
                    sL.StartPoint = false;

                // add the Start logo to new selected Node
                sL = nodes.Find(n => n.NodeName == StartLocation);
                sL.StartPoint = true;

                // Stop Animation
                ClearSearchAndAnimation();
            }
        }

        public RelayCommand GoalLocationsComboBoxSelection_Command { get; set; }
        public void GoalLocationsComboBoxSelection_Handler()
        {
            if (!string.IsNullOrEmpty(GoolLocation))
            {
                // remove the Gool logo from the old Gool Node
                if (gL != null)
                    gL.GoolPoint = false;

                // add the Gool logo to new selected Node
                gL = nodes.Find(n => n.NodeName == GoolLocation);
                gL.GoolPoint = true;

                // stop drawing animation
                ClearSearchAndAnimation();
            }
        }

        public RelayCommand MapsComboBoxSlection_Command { get; set; }
        public void MapsComboBoxSlection_Handler()
        {
            MapSelectionChanged_Clear();
            // if we change the map
            if (SelectedMap != null)
            {
                nodes = GetNodes(SelectedMap.Nodes);
                UpdatePoints();
            }
            // if we creating a new map
            else if (SelectedMap == null)
            {
                newNodeNameCount = 0;
                CleanCustomMapTool();
            }
            ClearSearchAndAnimation();
        }

        #endregion

        #region Binding Properties

        public event EventHandler UpdateCanvasUi;
        private void OnUpdateCanvasUi()
        {
            UpdateCanvasUi?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler AnimationStoped;
        public void OnAnimationStoped()
        {
            AnimationStoped?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler AnimationStarted;
        public void OnAnimationStarted()
        {
            AnimationStarted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<int> AnimateMoveChanged;
        public void OnAnimateMoveChanged(int num)
        {
            AnimateMoveChanged?.Invoke(this, num);
        }

        private int animateMoveNumber;
        public int AnimateMoveNumber
        {
            get
            {
                return animateMoveNumber;
            }
            set
            {
                if (animateMoveNumber != value)
                {
                    animateMoveNumber = value;
                    OnAnimateMoveChanged(value);
                }

            }
        }

        private string animationPlayButtonToolTip;
        public string AnimationPlayButtonToopTip
        {
            get { return animationPlayButtonToolTip; }
            set
            {
                animationPlayButtonToolTip = value;
                RaisePropertyChanged();
            }
        }

        private bool isAnimationAvailable;
        public bool IsAnimationAvailable
        {
            get { return isAnimationAvailable; }
            set
            {
                isAnimationAvailable = value;
                RaisePropertyChanged();
            }
        }

        private string selectedSearchType;
        public string SelectedSearchType
        {
            get { return selectedSearchType; }
            set
            {
                if(selectedSearchType != value)
                {
                    selectedSearchType = value;
                    RaisePropertyChanged();
                    ClearSearchAndAnimation();
                }
                
            }

        }

        private string startLocation;
        public string StartLocation
        {
            get { return startLocation; }
            set
            {
                if (startLocation != value)
                {
                    startLocation = value;
                    RaisePropertyChanged();
                    SearchCommand.RaiseCanExecuteChanged();
                }

            }
        }

        private string goolLocation;
        public string GoolLocation
        {
            get { return goolLocation; }
            set
            {
                if (goolLocation != value)
                {
                    goolLocation = value;
                    RaisePropertyChanged();
                    SearchCommand.RaiseCanExecuteChanged();
                }

            }
        }

        private string searchSpeedIcon;
        public string SearchSpeedIcon
        {
            get { return searchSpeedIcon; }
            set
            {
                searchSpeedIcon = value;
                RaisePropertyChanged();
            }
        }

        private string newMapName;
        public string NewMapName
        {
            get { return newMapName; }
            set
            {
                if (newMapName != value)
                {
                    newMapName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string newMapNameValidation;
        public string NewMapNameValidation
        {
            get { return newMapNameValidation; }
            set
            {
                newMapNameValidation = value;
                if (!string.IsNullOrEmpty(newMapNameValidation))
                    IsNewMapNameValidationEnabled = true;
                else
                    IsNewMapNameValidationEnabled = false;

                RaisePropertyChanged();
                RaisePropertyChanged("IsNewMapNameValidationEnabled");
            }
        }

        public bool IsNewMapNameValidationEnabled { get; set; }

        private Map selectedMap;
        public Map SelectedMap
        {
            get { return selectedMap; }
            set
            {
                if (selectedMap != value)
                {
                    selectedMap = value;
                    if (selectedMap != null)
                        ShowMap = true;
                    else
                        ShowMap = false;
                    RaisePropertyChanged();
                }

            }
        }

        private int searchedPathCount;
        public int SearchedPathCount
        {
            get { return searchedPathCount; }
            set
            {
                searchedPathCount = value;
                RaisePropertyChanged();
            }
        }

        private bool showMap;
        public bool ShowMap
        {
            get { return showMap; }
            set
            {
                showMap = value;
                RaisePropertyChanged();
                RaisePropertyChanged("ShowCustomMapTool");
            }
        }

        public bool ShowCustomMapTool
        {
            get { return !ShowMap; }
        }

        private bool previousAnimateButtonEnabled;
        public bool PreviousAnimateButtonEnabled
        {
            get { return previousAnimateButtonEnabled; }
            set
            {
                previousAnimateButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool nextAnimateButtonEnabled;
        public bool NextAnimateButtonEnabled
        {
            get { return nextAnimateButtonEnabled; }
            set
            {
                nextAnimateButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool findedCorrectPath;
        public bool FindedCorrectPath
        {
            get { return findedCorrectPath; }
            set
            {
                findedCorrectPath = value;
                RaisePropertyChanged();
            }
        }

        private Symbol playIcon;

        public Symbol PlayIcon
        {
            get
            {
                return playIcon;
            }
            set
            {
                playIcon = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<string> SearchTypes { get; set; } =
            new ObservableCollection<string> { "DEPTH FIRST", "BREADTH FIRST", "DEPTH FIRST WITH FILTER" };

        public ObservableCollection<string> StartLocations { get; set; }
            = new ObservableCollection<string>();

        public ObservableCollection<string> GoolLocations { get; set; }
            = new ObservableCollection<string>();

        private ObservableCollection<Map> maps;
        public ObservableCollection<Map> Maps
        {
            get
            {
                return maps;
            }
            set
            {
                maps = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Page Events
        
        //public async void Tree_Button_ClickAsync(object sender, RoutedEventArgs e)
        //{
        //    CoreApplicationView newView = CoreApplication.CreateNewView();
        //    int newViewId = 0;
        //    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        Frame frame = new Frame();

        //        frame.Navigate(typeof(TreeVisualsPage), SearchedPaths);
        //        Window.Current.Content = frame;
        //        // You have to activate the window in order to show it later.
        //        Window.Current.Activate();

        //        newViewId = ApplicationView.GetForCurrentView().Id;
        //    });
        //    bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        //}

        public async void Info_Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            string text = "Left Click To Add Node.\nRight Click to Remove Node/Line.\nThe white circle indicates the selected node.\nThe green circle can connect to white circle.";
            MessageDialog showDialog = new MessageDialog(text, "Information");
            showDialog.Commands.Add(new UICommand("Yes"));
            await showDialog.ShowAsync();
        }
        
        public void Page_LoadedAsync(object sender, RoutedEventArgs e)
        {


        }

        public async void Page_Loading(FrameworkElement sender, object args)
        {
            try
            {
                string path = ApplicationData.Current.LocalFolder.Path + "\\Maps.json";
                if (!File.Exists(path))
                {
                    string jsonStringMaps = JsonConvert.SerializeObject(DesignMapHelper.GetDefualtMaps(), new JsonSerializerSettings
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
                Maps = new ObservableCollection<Map>(JsonMaps);
            }
            catch
            {
                Maps = new ObservableCollection<Map>(DesignMapHelper.GetDefualtMaps());
            }
            finally
            {
                SelectedMap = Maps[0];
            }
        }

        #endregion

        #region Canvas Events

        //CanvasControl sender, CanvasDrawEventArgs args
        public void Canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillCircle(10, 10, 2, Colors.Red);
            // Defualt Mode
            if (ShowMap)
            {
                // Draw the Map Path
                DrawMapPath(args, nodes, Colors.Gray, mThickness);
                // Draw the Corrent Path
                if (FindedCorrectPath && drawCorrentRoad)
                {
                    for (int i = 1; i < CorrectPaths[0].Count; i++)
                    {
                        args.DrawingSession.DrawLine(
                            xAxis[CorrectPaths[0][i].X],
                            yAxis[CorrectPaths[0][i].Y],
                            xAxis[CorrectPaths[0][i - 1].X],
                            yAxis[CorrectPaths[0][i - 1].Y],
                            Colors.Green,
                            lThickness);
                    }
                }
                // Draw the Searching Path
                if (IsAnimationAvailable && drawAnimation)
                {
                    for (int i = 1; i < AnimationsWalks[AnimateMoveNumber].Count; i++)
                    {
                        if (i != AnimationsWalks[AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawLine(
                                xAxis[AnimationsWalks[AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i - 1].Y],
                                xAxis[AnimationsWalks[AnimateMoveNumber][i].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i].Y],
                                Colors.OrangeRed,
                                lThickness);
                        }
                        else
                        {
                            args.DrawingSession.DrawLine(
                                xAxis[AnimationsWalks[AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i - 1].Y],
                                xAxis[AnimationsWalks[AnimateMoveNumber][i].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i].Y],
                                Colors.Yellow,
                                lThickness);
                        }
                        args.DrawingSession.DrawText(
                            (i).ToString(),
                            xAxis[AnimationsWalks[AnimateMoveNumber][i - 1].X] + circleRadius + sThickness,
                            yAxis[AnimationsWalks[AnimateMoveNumber][i - 1].Y] - circleRadius - sThickness,
                            Colors.White,
                            textformat);
                        args.DrawingSession.DrawText((i + 1).ToString(),
                            xAxis[AnimationsWalks[AnimateMoveNumber][i].X] + circleRadius + sThickness,
                            yAxis[AnimationsWalks[AnimateMoveNumber][i].Y] - circleRadius - sThickness,
                            Colors.White,
                            textformat);

                        if (i == AnimationsWalks[AnimateMoveNumber].Count - 1)
                        {
                            args.DrawingSession.DrawCircle(
                                xAxis[AnimationsWalks[AnimateMoveNumber][i - 1].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i - 1].Y],
                                circleRadius,
                                Colors.YellowGreen, lThickness);
                            args.DrawingSession.DrawCircle(
                                xAxis[AnimationsWalks[AnimateMoveNumber][i].X],
                                yAxis[AnimationsWalks[AnimateMoveNumber][i].Y],
                                circleRadius,
                                Colors.Yellow,
                                lThickness);
                        }
                    }
                }
                // Draw the Nodes
                DrawNodes(args, nodes, circleRadius, textformat);
            }
            // Design Mode
            else
            {
                // Draw the y/x axis
                DrawCoordinateGeometry(args, xAxis, yAxis, Colors.Black, sThickness);
                // Draw the Map Path
                DrawMapPath(args, nodes, Colors.Gray, mThickness);
                // Draw free locations 
                if (nodes.Count < 26)
                {
                    DrawFreeLocations(args, xAxis, yAxis, DesignMapHelper.Lines, Colors.SteelBlue, sCircleRadius, mThickness);
                }
                // Draw the Nodes
                DrawNodes(args, nodes, circleRadius, textformat);
                // Draw the selected Nodes and Available Nodes That can be Connected
                if (selectedNode != null)
                {
                    foreach (var node in nodes)
                    {
                        bool busy = false;
                        foreach (var busyNode in DesignMapHelper.BusyLocations)
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

        #endregion

        #region Drawing Method

        private void DrawMapPath(
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

        private void DrawFreeLocations(
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

        private void DrawCoordinateGeometry(
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

        private void DrawNodes(
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

        #region Graph Search Logic

        private void DepthFirstSearch(Road road)
        {
            if (road.HeadNode.GoolPoint)
            {
                CorrectPaths.Add(road.PassedRoad);
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
                        CorrectPaths.Add(extendedroad.PassedRoad);
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
                            Road newExtendedRoad = new Road
                            {
                                HeadNode = node
                            };
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

        private void ClearSearchAndAnimation()
        {
            SearchSpeedTick.Stop();
            AnimateMoveNumber = 0;
            FindedCorrectPath = false;
            NextAnimateButtonEnabled = false;
            drawCorrentRoad = false;
            drawAnimation = false;
            CorrectPaths.Clear();
            OnUpdateCanvasUi();
        }

        private void MapSelectionChanged_Clear()
        {
            nodes = new List<Node>();
            sL = null;
            gL = null;
        }

        private void StopAnimation()
        {
            drawAnimation = false;
            PlayIcon = Symbol.Play;
            AnimationPlayButtonToopTip = "Play Animation";
            SearchSpeedTick.Stop();
        }

        private void StartAnimation()
        {
            drawAnimation = true;
            PlayIcon = Symbol.Pause;
            AnimationPlayButtonToopTip = "Pause Animation";
            SearchSpeedTick.Start();
        }

        private void DebugAnimation()
        {
            drawAnimation = true;
            PlayIcon = Symbol.Play;
            AnimationPlayButtonToopTip = "Play Animation";
            SearchSpeedTick.Stop();
        }

        private List<Node> GetNodes(List<Node> nodes)
        {
            Node newNode;
            List<Node> newNodes = new List<Node>();
            foreach (var node in nodes)
            {
                newNode = new Node
                {
                    X = node.X,
                    Y = node.Y,
                    NodeName = node.NodeName,
                    StartPoint = node.StartPoint,
                    GoolPoint = node.GoolPoint,
                    ConnectedNodes = new List<Node>()
                };
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

        private ObservableCollection<List<Node>> GetAnimationMoves(List<List<Node>> searchedPath)
        {
            List<List<Node>> Animation = new List<List<Node>>();
            List<Node> previoseMove = null;
            // Generate a moves
            for (int i = 0; i < searchedPath.Count; i++)
            {
                List<List<Node>> move = null;
                if (previoseMove == null)
                {
                    move = CreatAnimationFromOnePath(searchedPath[i], 0);
                }
                else
                {
                    int endFilterIndex = (previoseMove.Count > searchedPath[i].Count) ? searchedPath[i].Count : previoseMove.Count;
                    bool finishFiltring = false;
                    for (int j = 0; j < endFilterIndex; j++)
                    {
                        if (searchedPath[i][j] != previoseMove[j])
                        {
                            move = CreatAnimationFromOnePath(searchedPath[i], j);
                            finishFiltring = true;
                            break;
                        }
                    }
                    if (!finishFiltring)
                        move = CreatAnimationFromOnePath(searchedPath[i], searchedPath[i].Count - 1);
                }
                previoseMove = move[move.Count - 1];
                Animation.AddRange(move);
            }

            // Remove the duplicated move
            for (int i = 1; i < Animation.Count; i++)
            {
                var walk = Animation[i];
                for (int j = i; j < Animation.Count; j++)
                {
                    if (i != j && walk.Count == Animation[j].Count)
                    {
                        bool matched = true;
                        for (int k = walk.Count - 1; k >= 0; k--)
                        {
                            if (walk[k].NodeName != Animation[j][k].NodeName)
                            {
                                matched = false;
                                break;
                            }
                        }
                        if (matched)
                            Animation.RemoveAt(j);
                    }
                }
            }

            return new ObservableCollection<List<Node>>(Animation);
        }

        private List<List<Node>> CreatAnimationFromOnePath(List<Node> nodes, int startIndex)
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
            StartLocations.Clear();
            GoolLocations.Clear();
            foreach (var item in nodes)
            {
                StartLocations.Add(item.NodeName);
                GoolLocations.Add(item.NodeName);
            }
        }

        #endregion

        #region Collections Events

        private void SearchSpeedTick_Tick(object sender, object e)
        {
            AnimateMoveNumber++;
        }

        private void CorrectPathss_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                FindedCorrectPath = true;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SearchedPaths.Clear();
                AnimationsWalks.Clear();
                IsAnimationAvailable = false;
            }
        }

        private void SearchTool_animateMoveChanged(object sender, int animateMoveNumber)
        {
            if (animateMoveNumber == 0)
            {
                PreviousAnimateButtonEnabled = false;
            }
            
            else if (AnimateMoveNumber >= AnimationsWalks.Count)
            {
                NextAnimateButtonEnabled = false;
                drawCorrentRoad = true;
                drawAnimation = false;
                StopAnimation();
            }
            else
            {
                PreviousAnimateButtonEnabled = true;
                NextAnimateButtonEnabled = true;
            }
            OnUpdateCanvasUi();
        }

        #endregion

        #region Design Mode

        private void RemoveNode_Button_Click()
        {
            nodes.Clear();
            DesignMapHelper.RemovedLetters.Clear();
            DesignMapHelper.Lines.Clear();
            selectedNode = null;
            newNodeNameCount = 0;
            OnUpdateCanvasUi();
        }

        private void CleanCustomMapTool()
        {
            selectedNode = null;
            NewMapNameValidation = string.Empty;
            IsNewMapNameValidationEnabled = false;
            DesignMapHelper.Lines.Clear();
        }

        private string GetLetter()
        {
            if (DesignMapHelper.RemovedLetters.Count > 0)
            {
                return GetFromRemovedLetters();
            }
            else
            {
                return DesignMapHelper.Letters[newNodeNameCount++];
            }
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

        private string GetFromRemovedLetters()
        {
            string nodeName = DesignMapHelper.RemovedLetters[0];
            DesignMapHelper.RemovedLetters.RemoveAt(0);
            return nodeName;
        }

        #endregion

    }
}
