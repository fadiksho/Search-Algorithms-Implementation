using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.TreeVisuals
{
    public class Layer
    {
        public int YAxis { get; set; }
        public List<Branch> Branches { get; set; } = new List<Branch>();
    }
}
