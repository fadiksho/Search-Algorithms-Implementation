using Search.Models.GraphSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Search.ViewModel.GraphSearch
{
    public class SearchToolViewModel : INotifyPropertyChanged
    {
        public SearchToolViewModel(string selectedSearchType, string selectedSearchSpeed)
        {
            this.selectedSearchType = selectedSearchType;
            this.selectedMap = selectedMap;
            this.selectedSearchSpeed = selectedSearchSpeed;
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
                    OnPropertyChanged();
                }
            }
               
        }

        private string startLocation;
        public string StartLocation
        {
            get { return startLocation; }
            set
            {
                if(startLocation != value)
                {
                    startLocation = value;
                    OnPropertyChanged();
                }
                
            }
        }

        private string goolLocation;
        public string GoolLocation
        {
            get { return goolLocation; }
            set
            {
                if(goolLocation != value)
                {
                    goolLocation = value;
                    OnPropertyChanged();    
                }
                
            }
        }

        private Map selectedMap;
        public Map SelectedMap
        {
            get { return selectedMap; }
            set
            {
                if(selectedMap != value)
                {
                    selectedMap = value;
                    OnPropertyChanged();
                }
                
            }
        }

        private string selectedSearchSpeed;
        public string SelectedSearchSpeed
        {
            get { return selectedSearchSpeed; }
            set
            {
                if(selectedSearchSpeed != value)
                {
                    selectedSearchSpeed = value;
                    OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private bool showMap;
        public bool ShowMap
        {
            get { return showMap; }
            set
            {
                showMap = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDiagnostic
        {
            get { return !showCustomMapTool; }
        }

        private bool showCustomMapTool;
        public bool ShowCustomMapTool
        {
            get { return showCustomMapTool; }
            set
            {
                showCustomMapTool = value;
                OnPropertyChanged();
                OnPropertyChanged("ShowDiagnostic");
            }
        }

        private bool connectingNodeTogleEnabled;
        public bool ConnectingNodeTogleEnabled
        {
            get { return connectingNodeTogleEnabled; }
            set
            {
                if(connectingNodeTogleEnabled != value)
                {
                    connectingNodeTogleEnabled = value;
                    OnPropertyChanged();
                }
                
            }
        }

        private bool deleteNodeButtonEnabled;
        public bool DeleteNodeButtonEnabled
        {
            get { return deleteNodeButtonEnabled; }
            set
            {
                deleteNodeButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        private string newMapName;

        public string NewMapNameTextBox
        {
            get { return newMapName; }
            set
            {
                if(newMapName != value)
                {
                    newMapName = value;
                    OnPropertyChanged();
                }
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
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> SearchSpeed { get; set; } =
            new ObservableCollection<string>() { "X1", "X2", "X4" };
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
