using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Data;
using System.Text;

namespace Search.Models.GraphSearch
{
    public class Map : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private bool _isDeleteEnabled;
        public bool IsDeleteEnabled
        {
            get { return _isDeleteEnabled; }
            set { _isDeleteEnabled = value; OnPropertyChanged(); }
        }

        public List<Node> Nodes { get; set; }


        public Map GetCopy()
        {
            Map newMap = new Map();
            newMap.Name = this.Name;
            newMap.IsDeleteEnabled = this.IsDeleteEnabled;
            newMap.Nodes = new List<Node>(this.Nodes);
            
            return newMap;
        }

        public Map ShallowCopy()
        {
            return (Map)this.MemberwiseClone();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}