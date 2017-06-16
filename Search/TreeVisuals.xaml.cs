using Microsoft.Graphics.Canvas.UI.Xaml;
using Search.Models;
using Search.Models.GraphSearch;
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

namespace Search
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TreeVisuals : Page
    {
        List<List<Node>> MySearchPath = new List<List<Node>>();
        Dictionary<string, float> xAxis = new Dictionary<string, float>();
        List<float> yAxis = new List<float>();
        float canvasWidth, canvasHeight;
        float yScale = 100;
        float xScale = 100;
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
                for (int i = path.Count - 1; i > 0; i--)
                {
                    counter += 1;
                    args.DrawingSession.DrawText(path[i].NodeName, xScale * i, yScale * yLayer, Colors.Red);

                    yLayer++;
                }
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MySearchPath = (List<List<Node>>)e.Parameter;
            foreach (var item in MySearchPath)
            {
                item.Reverse();
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasWidth = (float)container.ActualWidth;
            canvasHeight = (float)container.ActualHeight;
        }
    }
}
