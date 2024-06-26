﻿using System;

namespace ClipperLib
{

    public struct IntPoint
    {
        public Int64 X;
        public Int64 Y;
        #if use_xyz
    public cInt Z;
    
    public IntPoint(cInt x, cInt y, cInt z = 0)
    {
      this.X = x; this.Y = y; this.Z = z;
    }
    
    public IntPoint(double x, double y, double z = 0)
    {
      this.X = (cInt)x; this.Y = (cInt)y; this.Z = (cInt)z;
    }
    
    public IntPoint(DoublePoint dp)
    {
      this.X = (cInt)dp.X; this.Y = (cInt)dp.Y; this.Z = 0;
    }

    public IntPoint(IntPoint pt)
    {
      this.X = pt.X; this.Y = pt.Y; this.Z = pt.Z;
    }
        #else
        public IntPoint(Int64 X, Int64 Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public IntPoint(double x, double y)
        {
            X = (Int64) x;
            Y = (Int64) y;
        }

        public IntPoint(IntPoint pt)
        {
            X = pt.X;
            Y = pt.Y;
        }
        #endif

        public static bool operator ==(IntPoint a, IntPoint b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(IntPoint a, IntPoint b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is IntPoint)
            {
                var a = (IntPoint) obj;
                return (X == a.X) && (Y == a.Y);
            }
            return false;
        }

        public override int GetHashCode()
        {
            //simply prevents a compiler warning
            return base.GetHashCode();
        }

    }

}