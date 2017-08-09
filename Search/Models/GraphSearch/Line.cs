using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Models.GraphSearch
{
    public class Line
    {
        private bool updateM;
        private bool updateB;
        private static float[] xAxis;
        public static float[] XAxis
        {
            get { return xAxis; }
            set
            {
                xAxis = value;
            }
        }

        private static float[] yAxis;
        public static float[] YAxis
        {
            get { return yAxis; }
            set
            {
                yAxis = value;
            }
        }

        private float m;
        public float M
        {
            get
            {
                m = (float)Math.Round(GetSlop(), 4);
                return m;
            }
        }

        private float b;
        public float B
        {
            get
            {
                b = (float)Math.Round(GetB(), 4);
                return b;
            }
        }

        public Node Point1 { get; set; }
        public Node Point2 { get; set; }

        public bool IfPointOnTheLine(float x, float y, float linethicknes)
        {
            float xSide = x;
            float ySide = y;

            bool XScope = (x >= xAxis[Point1.X] && x <= xAxis[Point2.X]) || (x <= xAxis[Point1.X] && x >= xAxis[Point2.X]);
            bool YScope = (y >= yAxis[Point1.Y] && y <= yAxis[Point2.Y]) || (y <= yAxis[Point1.Y] && y >= yAxis[Point2.Y]);
            bool MScope;
            // vertical line
            if (Point1.X == Point2.X)
            {
                // xside = any x
                xSide = xAxis[Point1.X];
                MScope = x >= xSide - linethicknes && x <= xSide + linethicknes;
                if (MScope && YScope)
                    return true;
            }
            // Horizontal line
            else if (Point1.Y == Point2.Y)
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

        private float GetSlop()
        {
            return (float)(Point1.Y - Point2.Y) / (Point1.X - Point2.X);
        }

        private float GetB()
        {
            return (float)Point1.Y - (m * Point1.X);
        }

        public static bool nearlyEqual(float a, float b, float epsilon)
        {
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(a - b);

            if (a == b)
            { // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || (absA + absB < float.MinValue))
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * float.MinValue);
            }
            else
            { // use relative error
                return diff / Math.Min((absA + absB), float.MaxValue) < epsilon;
            }
        }
    }
}