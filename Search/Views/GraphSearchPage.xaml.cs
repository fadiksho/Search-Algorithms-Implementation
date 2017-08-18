
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
        public GraphSearchViewModels ViewModel { get; set; }
        
        public GraphSearchPage()
        {
            this.InitializeComponent();
            ViewModel = new GraphSearchViewModels();
            ViewModel.UpdateCanvasUi += SearchTool_UpdateCanvasUi;
        }

        private void SearchTool_UpdateCanvasUi(object sender, EventArgs e)
        {
            canvascontroll.Invalidate();
        }

        private void DeleteMap_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteMap_CommandAsync.Execute(sender);
            maps_ComboBox.IsDropDownOpen = false;
        }

    }
}