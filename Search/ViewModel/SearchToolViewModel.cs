using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Search.ViewModel
{
    public class SearchToolViewModel : INotifyPropertyChanged
    {
        public SearchToolViewModel(string selectedSearchType,string selectedMap, string selectedSearchSpeed)
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

        private string selectedMap;
        public string SelectedMap
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

        public ObservableCollection<string> SearchTypes { get; set; } =
            new ObservableCollection<string> { "DEPTH FIRST", "BREADTH FIRST", "DEPTH FIRST WITH FILTER" };
        public ObservableCollection<string> StartLocations { get; set; }
            = new ObservableCollection<string>();
        public ObservableCollection<string> GoolLocations { get; set; }
            = new ObservableCollection<string>();
        public ObservableCollection<string> Maps { get; set; } =
            new ObservableCollection<string>() { "SMALL", "MEDIUM", "LARGE" };
        public ObservableCollection<string> SearchSpeed { get; set; } =
            new ObservableCollection<string>() { "X1", "X2", "X4" };
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
