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
        public SearchToolViewModel(string selectedSearchType)
        {
            this.selectedSearchType = selectedSearchType;
            searchSpeed = 1000;
            searchSpeedIcon = "\uEC49";
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

        public event EventHandler<int> animateMoveChanged;
        public void OnAnimateMoveChanged(int num)
        {
            animateMoveChanged?.Invoke(this, num);
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
                OnPropertyChanged();
            }
        }
        
        private bool isAnimationAvailable;
        public bool IsAnimationAvailable
        {
            get { return isAnimationAvailable; }
            set
            {
                isAnimationAvailable = value;
                OnPropertyChanged();
                
            }
        }

        private string selectedSearchType;
        public string SelectedSearchType
        {
            get { return selectedSearchType; }
            set
            {
                if (selectedSearchType != value)
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
                if (startLocation != value)
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
                if (goolLocation != value)
                {
                    goolLocation = value;
                    OnPropertyChanged();
                }

            }
        }

        private int searchSpeed;
        public int SearchSpeed { get { return searchSpeed; } }

        private string searchSpeedIcon;
        public string SearchSpeedIcon { get { return searchSpeedIcon; } }

        private string newMapName;
        public string NewMapName
        {
            get { return newMapName; }
            set
            {
                if (newMapName != value)
                {
                    newMapName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string newMapNameValidation;
        public string NewMapNameValidation
        {
            get { return newMapNameValidation; }
            set
            {
                if (string.IsNullOrWhiteSpace(newMapName))
                    IsNewMapNameValidationEnabled = true;
                else
                    IsNewMapNameValidationEnabled = false;
                OnPropertyChanged("IsNewMapNameValidationEnabled");

                newMapNameValidation = value;
                OnPropertyChanged();
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
                OnPropertyChanged("ShowCustomMapTool");
            }
        }

        public bool ShowCustomMapTool
        {
            get { return !ShowMap; }
        }

        //private bool connectingNodeTogleEnabled;
        //public bool ConnectingNodeTogleEnabled
        //{
        //    get { return connectingNodeTogleEnabled; }
        //    set
        //    {
        //        if (connectingNodeTogleEnabled != value)
        //        {
        //            connectingNodeTogleEnabled = value;
        //            OnPropertyChanged();
        //        }

        //    }
        //}

        //private bool deleteNodeButtonEnabled;
        //public bool DeleteNodeButtonEnabled
        //{
        //    get { return deleteNodeButtonEnabled; }
        //    set
        //    {
        //        deleteNodeButtonEnabled = value;
        //        OnPropertyChanged();
        //    }
        //}

        private bool previousAnimateButtonEnabled;
        public bool PreviousAnimateButtonEnabled
        {
            get { return previousAnimateButtonEnabled; }
            set
            {
                previousAnimateButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool nextAnimateButtonEnabled;
        public bool NextAnimateButtonEnabled
        {
            get { return nextAnimateButtonEnabled; }
            set
            {
                nextAnimateButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool findedCorrectPath;
        public bool FindedCorrectPath
        {
            get { return findedCorrectPath; }
            set
            {
                findedCorrectPath = value;
                OnPropertyChanged();
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void ChangeSpeed()
        {
            if (searchSpeed == 1000)
            {
                searchSpeedIcon = "\uEC4A";
                searchSpeed = 250;
            }
            else if (searchSpeed == 2000)
            {
                searchSpeedIcon = "\uEC49";
                searchSpeed = 1000;
            }
            else if (searchSpeed == 250)
            {
                searchSpeedIcon = "\uEC48";
                searchSpeed = 2000;
            }
            OnPropertyChanged("SearchSpeed");
            OnPropertyChanged("SearchSpeedIcon");
        }
    }
}
