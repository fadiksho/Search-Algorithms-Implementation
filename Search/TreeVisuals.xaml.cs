using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Models;
using Search.Models.GraphSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace Search
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TreeVisuals : Page
    {
        ObservableCollection<List<Node>> MySearchPath = new ObservableCollection<List<Node>>();
        List<Node> tree = new List<Node>();
        Dictionary<string, float> xAxis = new Dictionary<string, float>();
        List<float> yAxis = new List<float>();
        float canvasWidth, canvasHeight, canvasWidthMargin, canvasHeightMargin,
            blockWidth, blockHeight, circleRadius;

        int yLayer = 1;

        public TreeVisuals()
        {
            this.InitializeComponent();
        }

        private void Mycanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            int counter = 0;
            foreach (var path in MySearchPath)
            {
                yLayer = 1;
                for (int i = 0; i < path.Count; i++)
                {
                    counter += 1;
                    args.DrawingSession.DrawText(path[i].NodeName, blockWidth * (i + 1), blockHeight * (yLayer), Colors.Red);

                    yLayer++;
                }
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ObservableCollection<List<Node>> paths = (ObservableCollection<List<Node>>)e.Parameter;

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
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            canvasWidth = (float)container.ActualWidth;
            canvasHeight = (float)container.ActualHeight;

            canvasWidthMargin = canvasWidth / 30;
            canvasHeightMargin = canvasHeight / 15;

            blockWidth = (canvasWidth - 2 * canvasWidthMargin) / 9;
            blockHeight = (canvasHeight - 2 * canvasHeightMargin) / 4;

            if (blockHeight > blockWidth)
                circleRadius = blockWidth / 5;
            else
                circleRadius = blockHeight / 5;
        }
    }
}
