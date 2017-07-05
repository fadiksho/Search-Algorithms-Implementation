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
        public Node Point1 { get; set; }
        public Node Point2 { get; set; }

        public bool IfPointOnTheLine(float x, float y, float linethicknes, float[] xAxis, float[] yAxis)
        {
            float xSide = x;
            float ySide = y;

            bool XScope = (x >= xAxis[Point1.X] && x <= xAxis[Point2.X]) || (x <= xAxis[Point1.X] && x >= xAxis[Point2.X]);
            bool YScope = (y >= yAxis[Point1.Y] && y <= yAxis[Point2.Y]) || (y <= yAxis[Point1.Y] && y >= yAxis[Point2.Y]);
            bool MScope;
            // vertical line
            if (xAxis[Point1.X] == xAxis[Point2.X])
            {
                // xside = any x 
                xSide = xAxis[Point1.X];
                MScope = x >= xSide - linethicknes && x <= xSide + linethicknes;
                if (MScope && YScope)
                    return true;
            }
            // Horizontal line
            else if (yAxis[Point1.Y] == yAxis[Point2.Y])
            {
                xSide = yAxis[Point1.Y];
                MScope = xSide + linethicknes > ySide && ySide > xSide - linethicknes;
                if (MScope && XScope)
                    return true;
            }
            else
            {
                m = ((yAxis[Point1.Y] - yAxis[Point2.Y]) / (xAxis[Point1.X] - xAxis[Point2.X]));
                b = yAxis[Point1.Y] - (m * xAxis[Point1.X]);
                xSide = (m * x) + b;
                MScope = xSide + linethicknes > ySide && ySide > xSide - linethicknes;
                if (MScope && XScope && YScope)
                    return true;
            }
            return false;
        }
        public float GetSlop(float[] xAxis, float[] yAxis)
        {
            if (xAxis[Point1.X] == xAxis[Point2.X])
            {
                return float.NaN;
            }
            else if(yAxis[Point1.Y] == yAxis[Point2.Y])
            {
                return 0;
            }
            else
            {
                return ((yAxis[Point1.Y] - yAxis[Point2.Y]) / (xAxis[Point1.X] - xAxis[Point2.X]));
            }
        }
    }
}