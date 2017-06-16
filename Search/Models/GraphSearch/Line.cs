using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class Line
    {
        private float m;
        private float b;
        public Tuple<Node, Node> LineName { get; set; }
        public Tuple<float, float> Point1 { get; set; }
        public Tuple<float, float> Point2 { get; set; }
        public bool IfPointOnTheLine(float x, float y, float linethicknes)
        {
            float xSide = 0;
            float ySide = y;

            bool XScope = (x >= Point1.Item1 && x <= Point2.Item1) || (x <= Point1.Item1 && x >= Point2.Item1);
            bool YScope = (y >= Point1.Item2 && y <= Point2.Item2) || (y <= Point1.Item2 && y >= Point2.Item2);
            bool MScope;
            // vertical line
            if (Point1.Item1 == Point2.Item1)
            {
                // xside = any x 
                xSide = Point1.Item1;
                MScope = x >= xSide - linethicknes && x <= xSide + linethicknes;
                if(MScope && YScope)
                    return true;
            }
            // Horizontal line
            else if (Point1.Item2 == Point2.Item2)
            {
                xSide = Point1.Item2;
                MScope = xSide + linethicknes > ySide && ySide > xSide - linethicknes;
                if (MScope && XScope)
                    return true;
            }
            else
            {
                m = ((Point1.Item2 - Point2.Item2) / (Point1.Item1 - Point2.Item1));
                b = Point1.Item2 - (m * Point1.Item1);
                xSide = (m * x) + b;
                MScope = xSide + linethicknes > ySide && ySide > xSide - linethicknes;
                if (MScope && XScope && YScope)
                    return true;
            }
            return false;
        }
    }
}