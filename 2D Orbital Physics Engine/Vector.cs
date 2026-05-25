using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2D_Orbital_Physics_Engine
{
    public class Vector
    {
        public double X {get; set;}
        public double Y {get; set;}

        public Vector()
        {
            X = 0;
            Y = 0;
        }
        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector(Vector a)
        {
            X = a.X;
            Y = a.Y;
        }

        public double SquaredMagnitude()
        {
            return X * X + Y * Y;
        }

        //Adition
        public static Vector operator +(Vector a, Vector b)
            => new Vector(a.X + b.X, a.Y + b.Y);
        
        //Subtraction
        public static Vector operator -(Vector a, Vector b)
            => new Vector(a.X - b.X, a.Y - b.Y);

        //Scaling
        public static Vector operator %(Vector a, double factor)
            => new Vector(a.X * factor, a.Y * factor);

        //Magnitude
        public static double operator !(Vector a)
            => Math.Sqrt(a.X * a.X + a.Y * a.Y);

        //Normalization
        public static Vector operator ~(Vector a)
        {
            double magnitue = !a;
            if(magnitue == 0) return new Vector(0, 0);
            return new Vector(a.X / (magnitue), a.Y / (magnitue));
        }

        //Dot Product
        public static double operator *(Vector a, Vector b)
            => a.X * b.X + a.Y * b.Y;
    }
}

