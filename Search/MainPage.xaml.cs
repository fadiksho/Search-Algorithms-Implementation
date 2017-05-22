using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Search
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Binding Propery

        public ObservableCollection<string> SearchTypes { get; set; }
        public ObservableCollection<string> StartLocations { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> GoolLocations { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Maps { get; set; }
        public ObservableCollection<string> SearchSpeed { get; set; }
        public string SelectedSearchType;
        public string SelectedMap;
        public string StartLocation;
        public string GoolLocation;
        public string SelectedSearchSpeed;

        // Canavas Propery
        float CanvasWidth;
        float CanvasHeight;
        float startWidth;
        float width;
        float startHeight;
        float height;
        float[] X_axis;
        float[] Y_axis;
        float circleradius;
        List<Node> Nodes;
        bool animationPlay;
        bool drawCorrentRoad;
        int speed = 1000;

        Node StartLocationOfPreviousNode;
        Node GoolLocationOfPreviousNode;

        Node SL;
        Node GL;

        public List<Road> AvaiableRoads { get; set; } = new List<Road>();
        public List<Path> DiscoverdRoad { get; set; } = new List<Path>();
        public List<Node> Walks { get; set; } = new List<Node>();
        
        public Queue<List<Node>> Path { get; set; } = new Queue<List<Node>>();
        
        public MainPage()
        {
            this.InitializeComponent();
            SearchTypes = new ObservableCollection<string>() { "DEPTH FIRST", "DEPTH FIRST WITH FILTER", "HILL CLIM" };
            SelectedSearchType = SearchTypes[0];
            Maps = new ObservableCollection<string>() { "SMALL", "MEDIUM", "LARGE" };
            SearchSpeed = new ObservableCollection<string> { "X1", "X2", "X4" };
            SelectedMap = Maps[0];
            SelectedSearchSpeed = SearchSpeed[0];
            circleradius = 15;
            newText = "!Fady!";
            CreateFlameEffect();
        }

        private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if(AvaiableRoads.Count > 0)
            {
                for (int i = 0; i < Walks.Count - 1; i++)
                {
                    args.DrawingSession.DrawLine(Walks[i].X, Walks[i].Y, Walks[i + 1].X, Walks[i + 1].Y, Colors.Red, 10);
                }
            }
            
        }

        private void canvascontroll_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = 0; j < Nodes[i].ConnectedNode.Count; j++)
                {
                    args.DrawingSession.DrawLine(Nodes[i].X, Nodes[i].Y, Nodes[i].ConnectedNode[j].X, Nodes[i].ConnectedNode[j].Y, Colors.Gray, 5f);
                }
            }
            if (AvaiableRoads.Count > 0 && drawCorrentRoad)
            {
                for (int i = AvaiableRoads[0].PassedRoad.Count - 1; i > 0; i--)
                {
                    args.DrawingSession.DrawLine(AvaiableRoads[0].PassedRoad[i].X, AvaiableRoads[0].PassedRoad[i].Y,
                        AvaiableRoads[0].PassedRoad[i - 1].X, AvaiableRoads[0].PassedRoad[i - 1].Y, Colors.LightGreen, 5f);
                }
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                args.DrawingSession.FillCircle(Nodes[i].X, Nodes[i].Y, circleradius, Colors.Black);
                var textformat = new CanvasTextFormat();
                textformat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                textformat.VerticalAlignment = CanvasVerticalAlignment.Center;
                args.DrawingSession.DrawText(Nodes[i].NodeName, Nodes[i].X - circleradius / 4, Nodes[i].Y - circleradius / 4, 5f, 5f, Colors.White, textformat);
                if (Nodes[i].StartPoint)
                {
                    args.DrawingSession.DrawCircle(Nodes[i].X, Nodes[i].Y, circleradius, Colors.Red, 5f);
                }
                if (Nodes[i].GoolPoint)
                {
                    args.DrawingSession.DrawCircle(Nodes[i].X, Nodes[i].Y, circleradius, Colors.LightGreen, 5f);
                }
            }
        }

        private void container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasWidth = (float)container.ActualWidth;
            startWidth = CanvasWidth / 10;
            width = CanvasWidth - startWidth;
            CanvasHeight = (float)container.ActualHeight;
            startHeight = CanvasHeight / 20;
            height = CanvasHeight - startHeight;
            X_axis = new float[10] { startWidth, width / 5, 3 * width / 10, 2 * width / 5, width / 2, 3 * width / 5, 7 * width / 10, 4 * width / 5, 9 * width / 10, width };
            Y_axis = new float[5] { startHeight, height / 4, height / 2, 3 * height / 4, height };

            if (SelectedMap == Maps[0])
            {
                initializeMap1();
            }
            else if (SelectedMap == Maps[1])
            {
                initializeMap2();
            }
        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            DiscoverdRoad.Clear();
            if (SelectedSearchType == SearchTypes[0] && SL != null && GL != null)
            {
                AvaiableRoads = new List<Road>();

                Road road = new Road() { HeadNode = SL };
                road.PossibleRoad = new Queue<Node>();
                road.PassedRoad = new List<Node>();
                foreach (var item in road.HeadNode.ConnectedNode)
                {
                    road.PossibleRoad.Enqueue(item);
                }
                DepthFirstSearch(road);
            }
            if(AvaiableRoads.Count > 0)
            {
                foreach (var path in AvaiableRoads[0].PassedRoad)
                {
                    foreach (var node in DiscoverdRoad)
                    {
                        if (path.NodeName == node.Node.NodeName)
                        {
                            node.CurrectPath = true;
                        }
                    }
                }
            }
            
            DrawPath();
        }

        private void DepthFirstSearchTest(Road road)
        {
            if (road.HeadNode.GoolPoint)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                AvaiableRoads.Add(road);
                return;
            }
            else if (road.PossibleRoad == null || road.PossibleRoad.Count <= 0)
                return;
            else
            {
                Road extendedRoad = new Road();
                extendedRoad.PassedRoad = new List<Node>();
                extendedRoad.PassedRoad.Add(road.HeadNode);
                foreach (var item in road.PassedRoad)
                    extendedRoad.PassedRoad.Add(item);

                while (road.PossibleRoad.Count > 0)
                {
                    extendedRoad.HeadNode = road.PossibleRoad.Dequeue();
                    extendedRoad.PossibleRoad = new Queue<Node>();
                    foreach (var item in extendedRoad.HeadNode.ConnectedNode)
                    {
                        bool passed = false;
                        foreach (var pastroad in extendedRoad.PassedRoad)
                        {
                            if (pastroad.NodeName == item.NodeName)
                                passed = true;
                        }
                        if (!passed)
                            extendedRoad.PossibleRoad.Enqueue(item);
                    }
                    DepthFirstSearchTest(extendedRoad);
                }
            }
            return;
        }

        private void DepthFirstSearch(Road road)
        {
            Path path = new Path();
            path.Node = road.HeadNode;
            DiscoverdRoad.Add(path);

            if (road.HeadNode.GoolPoint)
            {
                road.PassedRoad.Insert(0, road.HeadNode);
                AvaiableRoads.Add(road);
                return;
            }
            else if (road.PossibleRoad.Count <= 0)
                return;
            else
            {
                if (AvaiableRoads.Count <= 0)
                {
                    Road extendedRoad = new Road();
                    extendedRoad.PassedRoad = new List<Node>();
                    extendedRoad.PassedRoad.Add(road.HeadNode);
                    foreach (var item in road.PassedRoad)
                        extendedRoad.PassedRoad.Add(item);

                    while (road.PossibleRoad.Count != 0 && AvaiableRoads.Count <= 0)
                    {
                        if (road.PossibleRoad.Count > 0)
                        {
                            extendedRoad.HeadNode = road.PossibleRoad.Dequeue();
                            extendedRoad.PossibleRoad = new Queue<Node>();
                            foreach (var item in extendedRoad.HeadNode.ConnectedNode)
                            {
                                bool passed = false;
                                foreach (var pastroad in extendedRoad.PassedRoad)
                                {
                                    if (pastroad.NodeName == item.NodeName)
                                        passed = true;
                                }
                                if (!passed)
                                    extendedRoad.PossibleRoad.Enqueue(item);
                            }
                            DepthFirstSearch(extendedRoad);
                        }
                    }
                }
            }
            return;
        }

        private void initializeMap1()
        {
            if (Nodes == null)
            {
                Node S = new Node() { NodeName = "S" };
                Node A = new Node() { NodeName = "A" };
                Node B = new Node() { NodeName = "B" };
                Node C = new Node() { NodeName = "C" };
                Node D = new Node() { NodeName = "D" };
                Node E = new Node() { NodeName = "E" };
                Node G = new Node() { NodeName = "G" };


                S.ConnectedNode = new List<Node>() { A, B };
                A.ConnectedNode = new List<Node>() { S, C, B };
                B.ConnectedNode = new List<Node>() { S, A, D };
                C.ConnectedNode = new List<Node>() { A, E, D };
                D.ConnectedNode = new List<Node>() { B, C, G };
                E.ConnectedNode = new List<Node>() { C };
                G.ConnectedNode = new List<Node>() { D };

                Nodes = new List<Node>() { S, A, B, C, D, E, G };
                UpdatePoints();
            }
            // S
            Nodes[0].X = X_axis[1];
            Nodes[0].Y = Y_axis[3];
            // A
            Nodes[1].X = X_axis[3];
            Nodes[1].Y = Y_axis[2];
            // B
            Nodes[2].X = X_axis[4];
            Nodes[2].Y = Y_axis[3];
            // C
            Nodes[3].X = X_axis[5];
            Nodes[3].Y = Y_axis[1];
            // D
            Nodes[4].X = X_axis[6];
            Nodes[4].Y = Y_axis[3];
            // E
            Nodes[5].X = X_axis[7];
            Nodes[5].Y = Y_axis[1];
            // G
            Nodes[6].X = X_axis[7];
            Nodes[6].Y = Y_axis[2];


        }

        private void initializeMap2()
        {
            if (Nodes == null)
            {
                #region 
                Node A = new Node() { NodeName = "A" };
                Node B = new Node() { NodeName = "B" };
                Node C = new Node() { NodeName = "C" };
                Node D = new Node() { NodeName = "D" };
                Node E = new Node() { NodeName = "E" };
                Node F = new Node() { NodeName = "F" };
                Node G = new Node() { NodeName = "G" };
                Node H = new Node() { NodeName = "H" };
                Node I = new Node() { NodeName = "I" };
                Node J = new Node() { NodeName = "J" };
                Node K = new Node() { NodeName = "K" };
                Node L = new Node() { NodeName = "L" };
                Node M = new Node() { NodeName = "M" };
                Node N = new Node() { NodeName = "N" };
                //Node O = new Node() { NodeName = "H" };
                //Node p = new Node() { NodeName = "H" };

                A.ConnectedNode = new List<Node>() { B, C };
                B.ConnectedNode = new List<Node>() { A, D, F };
                C.ConnectedNode = new List<Node>() { A, E, H };
                D.ConnectedNode = new List<Node>() { B, I, G };
                E.ConnectedNode = new List<Node>() { F, C, K };
                F.ConnectedNode = new List<Node>() { B, E, G };
                G.ConnectedNode = new List<Node>() { D };
                H.ConnectedNode = new List<Node>() { C, K, N };
                I.ConnectedNode = new List<Node>() { D, L };
                J.ConnectedNode = new List<Node>() { G, L };
                K.ConnectedNode = new List<Node>() { E, H };
                L.ConnectedNode = new List<Node>() { I, J, M };
                M.ConnectedNode = new List<Node>() { L, N };
                N.ConnectedNode = new List<Node>() { H, M };
                Nodes = new List<Node>() { A, B, C, D, E, F, G, H, I, J, K, L, M, N };
                UpdatePoints();
                #endregion
            }
            // A
            Nodes[0].X = X_axis[0];
            Nodes[0].Y = Y_axis[2];
            // B
            Nodes[1].X = X_axis[1];
            Nodes[1].Y = Y_axis[1];
            // C
            Nodes[2].X = X_axis[1];
            Nodes[2].Y = Y_axis[3];
            // D
            Nodes[3].X = X_axis[3];
            Nodes[3].Y = Y_axis[0];
            // E
            Nodes[4].X = X_axis[2];
            Nodes[4].Y = Y_axis[2];
            // F
            Nodes[5].X = X_axis[2];
            Nodes[5].Y = Y_axis[1];
            // G
            Nodes[6].X = X_axis[4];
            Nodes[6].Y = Y_axis[1];
            // H
            Nodes[7].X = X_axis[3];
            Nodes[7].Y = Y_axis[4];
            // I
            Nodes[8].X = X_axis[6];
            Nodes[8].Y = Y_axis[0];
            // J
            Nodes[9].X = X_axis[5];
            Nodes[9].Y = Y_axis[2];
            // K
            Nodes[10].X = X_axis[4];
            Nodes[10].Y = Y_axis[3];
            // L
            Nodes[11].X = X_axis[7];
            Nodes[11].Y = Y_axis[1];
            // M
            Nodes[12].X = X_axis[8];
            Nodes[12].Y = Y_axis[2];
            // N
            Nodes[13].X = X_axis[6];
            Nodes[13].Y = Y_axis[3];
        }

        private void ComboBox_StartLocationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            drawCorrentRoad = false;
            if (!string.IsNullOrEmpty(value))
            {
                // initialze the previouseNode
                if (StartLocationOfPreviousNode != null)
                {
                    StartLocationOfPreviousNode.StartPoint = false;
                }
                StartLocationOfPreviousNode = Nodes.Find(n => n.NodeName == value);
                // add logo to start location
                SL = Nodes.Find(n => n.NodeName == value);
                SL.StartPoint = true;
                // update the ui
                canvascontroll.Invalidate();
            }

        }

        private void ComboBox_GoolLocationSelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // Get The New Select Gool Location
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            drawCorrentRoad = false;
            if (!string.IsNullOrEmpty(value))
            {
                // remove the Gool logo from the old Gool loaction
                if (GoolLocationOfPreviousNode != null)
                {
                    GoolLocationOfPreviousNode.GoolPoint = false;
                }

                GoolLocationOfPreviousNode = Nodes.Find(n => n.NodeName == value);
                GL = Nodes.Find(n => n.NodeName == value);
                GL.GoolPoint = true;
                // update the ui
                canvascontroll.Invalidate();
            }
        }

        private void ComboBox_SearchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            canvascontroll.Invalidate();
        }

        private void ComboBox_MapsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CanvasWidth > 0 && SelectedMap != Maps[2])
            {
                AvaiableRoads.Clear();
                Nodes = null;
                StartLocationOfPreviousNode = null;
                GoolLocationOfPreviousNode = null;
                SL = null;
                GL = null;
                if (SelectedMap == Maps[0])
                {
                    initializeMap1();
                }
                else if (SelectedMap == Maps[1])
                {
                    initializeMap2();
                }
                canvascontroll.Invalidate();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string value = comboBox.SelectedItem as string;
            if(value == SearchSpeed[0])
            {
                speed = 1000;
            }
            else if(value == SearchSpeed[1])
            {
                speed = 500;
            }
            else
            {
                speed = 250;
            }
        }

        private void UpdatePoints()
        {
            StartLocations.Clear();
            GoolLocations.Clear();
            foreach (var item in Nodes)
            {
                StartLocations.Add(item.NodeName);
                GoolLocations.Add(item.NodeName);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            animationPlay = true;
            
        }

        private async void DrawPath()
        {
            // note drawCorrectRoad = false;
            try
            {
                await Task.Run(async () =>
                {
                    for (int i = 0; i < DiscoverdRoad.Count; i++)
                    {
                        Walks.Add(DiscoverdRoad[i].Node);
                        await Task.Delay(speed);
                        if(i != DiscoverdRoad.Count - 1 && DiscoverdRoad[i+1].CurrectPath == true)
                        {
                            int count = i;
                            while (DiscoverdRoad[count].CurrectPath == false)
                            {
                                Walks.Remove(DiscoverdRoad[count].Node);
                                count--;
                            }
                        }
                    }
                    Walks.Clear();
                    drawCorrentRoad = true;
                    canvascontroll.Invalidate();
                });
            }
            catch
            {

            }
        }

        #region Burning Text
        CanvasCommandList textCommandList;
        MorphologyEffect morphology;
        CompositeEffect composite;
        Transform2DEffect flameAnimation;
        Transform2DEffect flamePosition;

        string text, newText;
        float fontSize = 30.0f;


        /// <summary>
        /// Calculates a good font size so the text will fit even on smaller phone screens.
        /// </summary>
        private static float GetFontSize(Size displaySize)
        {
            const float maxFontSize = 72;
            const float scaleFactor = 24;

            return Math.Min((float)displaySize.Width / scaleFactor, maxFontSize);
        }
        /// <summary>
        /// Renders text into a command list and sets this as the input to the flame
        /// effect graph. The effect graph must already be created before calling this method.
        /// </summary>
        private void SetupText(ICanvasResourceCreator resourceCreator)
        {
            textCommandList = new CanvasCommandList(resourceCreator);

            using (var ds = textCommandList.CreateDrawingSession())
            {
                ds.Clear(Color.FromArgb(0, 0, 0, 0));

                ds.DrawText(
                    text,
                    0,
                    0,
                    Colors.SteelBlue,
                    new CanvasTextFormat
                    {
                        FontFamily = "Segoe UI",
                        FontSize = fontSize,
                        HorizontalAlignment = CanvasHorizontalAlignment.Center,
                        VerticalAlignment = CanvasVerticalAlignment.Top
                    });
            }

            // Hook up the command list to the inputs of the flame effect graph.
            morphology.Source = textCommandList;
            composite.Sources[1] = textCommandList;
        }
        private void ConfigureEffect(CanvasTimingInformation timing)
        {
            // Animate the flame by shifting the Perlin noise upwards (-Y) over time.
            flameAnimation.TransformMatrix = Matrix3x2.CreateTranslation(0, -(float)timing.TotalTime.TotalSeconds * 60.0f);

            // Scale the flame effect 2x vertically, aligned so it starts above the text.
            float verticalOffset = fontSize * 1.4f;

            var centerPoint = new Vector2(0, verticalOffset);

            flamePosition.TransformMatrix = Matrix3x2.CreateScale(1, 2, centerPoint);
        }

        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if (animationPlay)
            {
                var ds = args.DrawingSession;
                var newFontSize = GetFontSize(sender.Size);
                if (newText != text || newFontSize != fontSize)
                {
                    text = newText;
                    fontSize = newFontSize;
                    SetupText(sender);
                };
                ConfigureEffect(args.Timing);
                ds.DrawImage(composite, width, height - startHeight * 2);
                //animationPlay = false;
            }
            else
                animationPlay = true;
        }

        

        // Alternative entrypoint for use by AppIconGenerator.
        private void CreateFlameEffect()
        {
            // Thicken the text.
            morphology = new MorphologyEffect
            {
                // The Source property is set by SetupText().
                Mode = MorphologyEffectMode.Dilate,
                Width = 7,
                Height = 1
            };

            // Blur, then colorize the text from black to red to orange as the alpha increases.
            var colorize = new ColorMatrixEffect
            {
                Source = new GaussianBlurEffect
                {
                    Source = morphology,
                    BlurAmount = 3f
                },
                ColorMatrix = new Matrix5x4
                {
                    M11 = 0f,
                    M12 = 0f,
                    M13 = 0f,
                    M14 = 0f,
                    M21 = 0f,
                    M22 = 0f,
                    M23 = 0f,
                    M24 = 0f,
                    M31 = 0f,
                    M32 = 0f,
                    M33 = 0f,
                    M34 = 0f,
                    M41 = 0f,
                    M42 = 1f,
                    M43 = 0f,
                    M44 = 1f,
                    M51 = 1f,
                    M52 = -0.5f,
                    M53 = 0f,
                    M54 = 0f
                }
            };

            // Generate a Perlin noise field (see flamePosition).
            // Animate the noise by modifying flameAnimation's transform matrix at render time.
            flameAnimation = new Transform2DEffect
            {
                Source = new BorderEffect
                {
                    Source = new TurbulenceEffect
                    {
                        Frequency = new Vector2(0.109f, 0.109f),
                        Size = new Vector2(500.0f, 80.0f)
                    },
                    // Use Mirror extend mode to allow us to spatially translate the noise
                    // without any visible seams.
                    ExtendX = CanvasEdgeBehavior.Mirror,
                    ExtendY = CanvasEdgeBehavior.Mirror
                }
            };

            // Give the flame its wavy appearance by generating a displacement map from the noise
            // (see flameAnimation) and applying this to the text.
            // Stretch and position this flame behind the original text.
            flamePosition = new Transform2DEffect
            {
                Source = new DisplacementMapEffect
                {
                    Source = colorize,
                    Displacement = flameAnimation,
                    Amount = 40.0f
                }
                // Set the transform matrix at render time as it depends on window size.
            };

            // Composite the text over the flames.
            composite = new CompositeEffect()
            {
                Sources = { flamePosition, null }
            };
        }
        #endregion

    }
}