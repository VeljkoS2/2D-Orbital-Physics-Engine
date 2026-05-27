using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace _2D_Orbital_Physics_Engine
{
    public static class SharedData
    {
        public static bool UseAnalytic { get; set; } = false;
        public static Vector FocusPosition = new Vector();
        public static bool drawGrid { get; set; } = true;
        public static double c { get; } = 299792458;
        public static double G { get; } = 6.6743e-11;
        public static double LightYear { get; } = 9460730472580800;
        public static double AU { get; } = 149597870700;
        public static double Scale { get; set; } = AU / 300;
        public static double SolarRadius { get; } = 6.957e8;
        public static bool PredictIntersections { get; set; } = true;
        public static bool DrawOrbits { get; set; } = true;
        public static double OrbitDrawSize { get; set; } = 100;
        public static double totalElapsedTime { get; set; } = 0;
        public static Vector Offset = new Vector();
        public static int SW { get; set; } = 0;
        public static int SH { get; set; } = 0;
        public static Celestial_Body ghost { get; set; } = new Planet();
        public static float FloatLimit { get; set; } = 5e8f;
        //public static int intersectionsPredicted = 0;

        public static List<Celestial_Body> bodies { get; set; } = new List<Celestial_Body>();

        public static double SolarMass { get; } = 2e30;
        public static double EarthMass { get; } = 6e24;
            public static double MoonMass { get; } = 7.3e22;
        public static double MercuryMass { get; } = 3.3e23;
        public static double VenusMass { get; } = 4.9e24;
        public static double MarsMass { get; } = 6.4e23;
            public static double PhobosMass { get; } = 1.06e16;
            public static double DeimosMass { get; } = 1.47e15;
        public static double CeresMass { get; } = 9.39e20;
        public static double VestaMass { get; } = 2.59e20;
        public static double MakemakeMass { get; } = 2.5e21;
        public static double HaumeaMass { get; } = 4e21;
        public static double PallasMass { get; } = 3.108e20;
        public static double HygieaMass { get; } = 8.67e19;
        public static double ErosMass { get; } = 6.687e15;
        public static double JupiterMass { get; } = 1.9e27;
            public static double IoMass { get; } = 8.93e22;
            public static double EuropaMass { get; } = 4.8e22;
            public static double GanymedeMass { get; } = 1.48e23;
            public static double CallistoMass { get; } = 1.08e23;
        public static double SaturnMass { get; } = 5.7e26;
            public static double TitanMass { get; } = 1.345e23;
            public static double EnceladusMass { get; } = 1.085e20;
            public static double MimasMass { get; } = 3.75e19;
            public static double IapetusMass { get; } = 180.59e19;
            public static double RheaMass { get; } = 230.9e19;
            public static double DioneMass { get; } = 109.572e19;
        public static double UranusMass { get; } = 8.7e25;
            public static double TitaniaMass { get; } = 3.5e21;
            public static double OberonMass { get; } = 3.014e21;
            public static double UmbrielMass { get; } = 1.27e21;
            public static double ArielMass { get; } = 1.25e21;
            public static double MirandaMass { get; } = 6.3e19;
        public static double NeptuneMass { get; } = 1e26;
            public static double TritonMass { get; } = 2.14e22;
            public static double ProteusMass { get; } = 4.4e19;
            public static double NereidMass { get; } = 3.1e19;
        public static double PlutoMass { get; } = 1.3e22;
            public static double CharonMass { get; } = 1.58e21;
            public static double NixMass { get; } = 2.6e16;
            public static double HydraMass { get; } = 3.31e16;
            public static double KerberosMass { get; } = 1.65e16;
            public static double StyxMass { get; } = 7.5e15;
        public static double ErisMass { get; } = 1.67e22;
        public static double SednaMass { get; } = 2e21;
        public static double HalleyMass { get; } = 2.2e14;
        public static double HaleBoppMass { get; } = 1.3e16;
        public static double EnckeMass { get; } = 5.791e13;
        public static double SpaceshipMass { get; } = 1000000;
        public static double Voyager1Mass { get; } = 815;
        public static double Voyager2Mass { get; } = 720;
        public static double ViltrumMass { get; } = 1.2e25;
        public static double SagittariusAMass { get; } = SolarMass * 4.3e6; 

        public static Color defaultColor { get; } = Color.Empty;

        public static void ClampFloat(ref float val)
        {
            if (val > FloatLimit) val = FloatLimit;
            if (val < -FloatLimit) val = -FloatLimit;
        }
        public static float ClampFloat(float val)
        {
            if (val > FloatLimit) return FloatLimit;
            if (val < -FloatLimit) return -FloatLimit;
            return val;
        }

        public static double Dist(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static string TimeScale(double time)
        {
            int seconds = (int)time % 60;
            time /= 60;
            int minutes = (int)time % 60;
            time /= 60;
            int hours = (int)time % 24;
            time /= 24;
            int days = (int)time % 365;
            time /= 365;
            int years = (int)time;
            return "Y " + years.ToString("#0") + " ,D " + days.ToString("#0") + " - " + hours.ToString("#0") + " : " + minutes.ToString("#0") + " : " + seconds.ToString("#0");
        }

        public static string SecondsToDate(double totalSeconds)
        {
            long totalDays = (long)(totalSeconds / 86400.0);

            long year = 1;
            long remaining = totalDays;

            long days400 = 146097; 
            long days100 = 36524;  
            long days4 = 1461;  
            long days1 = 365;

            year += 400 * (remaining / days400);
            remaining %= days400;

            long centuries = remaining / days100;
            if (centuries == 4) centuries = 3;
            year += 100 * centuries;
            remaining -= centuries * days100;

            year += 4 * (remaining / days4);
            remaining %= days4;

            long singles = remaining / days1;
            if (singles == 4) singles = 3;
            year += singles;
            remaining -= singles * days1;

            bool isLeap = (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
            int[] daysInMonth = isLeap ? new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 } : new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            int month = 1;
            foreach (int dim in daysInMonth)
            {
                if (remaining < dim) break;
                remaining -= dim;
                month++;
            }

            int day = (int)remaining + 1;

            long secondsToday = (long)(totalSeconds % 86400);
            int hours = (int)(secondsToday / 3600);
            int minutes = (int)((secondsToday % 3600) / 60);
            int seconds = (int)(secondsToday % 60);

            return $"Y{year:0000} M{month:00} D{day:00} - {hours:00}:{minutes:00}:{seconds:00}";
        }

        public static string SizeScale()
        {
            double dist = 300 * Scale;

            if (dist > LightYear / 10)
            {
                dist /= LightYear;
                return dist.ToString("N2") + " LY";
            }
            else if (dist > AU / 10)
            {
                dist /= AU;
                return dist.ToString("N2") + " AU";
            }
            else if (dist > 100000 * 1000.0)
            {
                dist /= 1000 * 1000.0;
                return dist.ToString("N2") + " Mm";
            }
            else if (dist > 100000)
            {
                dist /= 1000.0;
                return dist.ToString("N2") + " km";
            }
            else
            {
                return dist.ToString("N2") + " m";
            }
        }
        public static string SizeScale(double dist)
        {
            dist = Math.Abs(dist);
            if (dist > LightYear / 10)
            {
                dist /= LightYear;
                return dist.ToString("N2") + " LY";
            }
            else if (dist > AU / 10)
            {
                dist /= AU;
                return dist.ToString("N2") + " AU";
            }
            else if (dist > 100000 * 1000.0)
            {
                dist /= 1000 * 1000.0;
                return dist.ToString("N2") + " Mm";
            }
            else if (dist > 100000)
            {
                dist /= 1000.0;
                return dist.ToString("N2") + " km";
            }
            else
            {
                return dist.ToString("N2") + " m";
            }
        }
        public static Celestial_Body CreateBody(double x, double y, double mass, Vector velocity, string name = "", bool isSaturn = false, bool isViltrum = false, bool isUranus = false, bool isNeptune = false)
        {
            if (mass / SolarMass > 300)
            {
                return new BlackHole(x, y, mass, velocity, name);
            }
            else if (mass / SolarMass > 0.07)
            {
                return new Star(x, y, mass, velocity, name);
            }
            else if (mass > 1e7)
            {
                return new Planet(x, y, mass, velocity, name, isSaturn, isViltrum, isUranus, isNeptune);
            }
            else
            {
                return new Spaceship(x, y, mass, velocity, name);
            }
        }
        public static Celestial_Body CreateBody(double x, double y, double mass, Vector velocity, Color color, string name = "", bool isSaturn = false, bool isViltrum = false, bool isUranus = false, bool isNeptune = false)
        {
            if (mass / SolarMass > 300)
            {
                return new BlackHole(x, y, mass, velocity, name);
            }
            else if (mass / SolarMass > 0.07)
            {
                return new Star(x, y, mass, velocity, name);
            }
            else if (mass > 1e7)
            {
                return new Planet(x, y, mass, velocity, color, name, isSaturn, isViltrum, isUranus, isNeptune);
            }
            else
            {
                return new Spaceship(x, y, mass, velocity, color, name);
            }
        }
        public static double CalculateRadius(double mass)
        {
            if (mass / SolarMass > 300)
            {
                return (2 * G * mass) / (c * c);
            }
            else if (mass / SolarMass > 0.07)
            {
                return SolarRadius * Math.Pow(mass / SolarMass, 0.8);
            }
            else
            {
                double density = 5000;
                if (mass > 5e25)
                {
                    density = 1300;
                }
                return Math.Pow((3 * mass) / (4 * Math.PI * density), 1.0 / 3.0);
            }
        }

        public static float PutInScreenPosScaleXClamp(double val)
        {
            double relative = val - FocusPosition.X;
            return (float)ClampFloat((float)(relative / Scale + SW / 2.0 + Offset.X));
        }
        public static float PutInScreenPosScaleX(double val)
        {
            double relative = val - FocusPosition.X;
            return (float)(relative / Scale + SW / 2.0 + Offset.X);
        }
        public static double PutInWorldPosScaleX(double val)
        {
            return (val - SW / 2.0 - Offset.X) * Scale + FocusPosition.X;
        }

        public static float PutInScreenPosScaleYClamp(double val)
        {
            double relative = val - FocusPosition.Y;
            return (float)ClampFloat((float)(relative / Scale + SH / 2.0 + Offset.Y));
        }
        public static float PutInScreenPosScaleY(double val)
        {
            double relative = val - FocusPosition.Y;
            return (float)(relative / Scale + SH / 2.0 + Offset.Y);
        }
        public static double PutInScreenPosScaleXDouble(double val)
        {
            double relative = val - FocusPosition.X;
            return relative / Scale + SW / 2.0 + Offset.X;
        }
        public static double PutInScreenPosScaleYDouble(double val)
        {
            double relative = val - FocusPosition.Y;
            return relative / Scale + SH / 2.0 + Offset.Y;
        }
        public static double PutInWorldPosScaleY(double val)
        {
            return (val - SH / 2.0 - Offset.Y) * Scale + FocusPosition.Y;
        }
        public static float PutInScreenScaleClamp(double val)
        {
            return (float)ClampFloat((float)(val / Scale));
        }
        public static float PutInScreenScale(double val)
        {
            return (float)(val / Scale);
        }

        public static double PutInWorldScale(double val)
        {
            return val * Scale;
        }
    }
}
