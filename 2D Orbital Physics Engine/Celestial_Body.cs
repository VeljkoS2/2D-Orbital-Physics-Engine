using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace _2D_Orbital_Physics_Engine
{
    abstract public class Celestial_Body : IDisposable
    {
        public string Name { get; set; } = "";
        public Size TextWidth { get; set; }
        public Vector Position = new Vector();
        public bool Initialized { get; set; } = false;
        public double Mass { get; set; }
        public bool IsSaturn { get; set; } = false;
        public bool IsViltrum { get; set; } = false;
        public bool IsUranus { get; set; } = false;
        public bool IsNeptune { get; set; } = false;
        public double Radius { get; set; } = 0;
        public Vector Velocity  = new Vector();
        public Vector Acceleration = new Vector();
        public Color Color { get; set; } = Color.Red;
        public Random Rnd { get; set; } = new Random();
        public Vector[] Trail = new Vector[200];
        public int TrailHead { get; set; } = 0;
        public int TrailCount { get; set; } = 0;
        public PointF[] TrailPoints = new PointF[200];
        public Pen Pen { get; set; } = new Pen(Color.Red ,2);
        public Pen TrailPen { get; set; } = new Pen(Color.Red ,2);
        public SolidBrush Brush { get; set; } = new SolidBrush(Color.Red);
        public GraphicsPath TrailPath { get; set; } = new GraphicsPath();
        public bool TrailDirty { get; set; } = true;
        public int HyperbolaPointCount { get; set; } = 0;
        public GraphicsPath HyperbolaPath { get; set; } = new GraphicsPath();
        public bool HyperbolaDirty { get; set; } = true;
        public double InvMass { get; set; } = 0;
        public SolidBrush OrbitalBrush { get; set; } = new SolidBrush(Color.Red);
        ////////////////////////////////////
        /// Orbit Paramaters
        public Celestial_Body DominantBody { get; set; }
        public double A { get; set; } = 0;
        public double B { get; set; } = 0;
        public double C { get; set; } = 0;
        public Vector E  = new Vector();
        public double EScal { get; set; } = 0;
        public double Angle { get; set; } = 0;
        public Pen OrbitPen { get; set; }
        public bool OrbitalDirty { get; set; } = true;
        public Vector Focus1 = new Vector();
        public Vector Focus2 = new Vector();
        public Vector Periapsis = new Vector();
        public double PeriapsisHeight { get; set; } = 0;
        public double TimeToPeriapsis { get; set; } = 0;
        public double ApoapsisHeight { get; set; } = 0;
        public double TimeToApoapsis { get; set; } = 0;
        public Vector Apoapsis = new Vector();
        public Vector OrbitCenter  = new Vector();
        public Vector OrbitCenterScreen = new Vector();
        public double EccentricAnomaly { get; set; } = 0;
        public double HyperbolicAnomaly { get; set; } = 0;
        public double AvgAngSpeed { get; set; } = 0;
        public double OrbitalPeriod { get; set; } = 0;
        public Vector Intersection = new Vector();
        public double CosAngle { get; set; } = 0;
        public double SinAngle { get; set; } = 0;
        public double AngleRad { get; set; } = 0;
        public double CosMAngle { get; set; } = 0;
        public double SinMAngle { get; set; } = 0;
        public bool HasIntersection { get; set; } = false;
        public double TimeToIntersection { get; set; } = 0;
        public Celestial_Body IntersectingBody { get; set; }
        ////////////////////////////////////
        /// Spaceship parameters
        public float DirAngleSS { get; set; } = 0;
        public double Throttle { get; set; } = 0;
        public bool Prograde { get; set; } = false;
        public bool Retrograde { get; set; } = false;
        public bool Radial { get; set; } = false;
        public bool Antiradial { get; set; } = false;
        public bool Free { get; set; } = true;
        public double Thrust { get; set; } = 500000000;
        public bool Landed { get; set; } = false;
        public bool LifitngOff { get; set; } = false;
        public Celestial_Body LandedBody { get; set; }
   

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                TrailPath?.Dispose();
                HyperbolaPath?.Dispose();
                Pen?.Dispose();
                TrailPen?.Dispose();
                OrbitPen?.Dispose();
                Brush?.Dispose();
                OrbitalBrush?.Dispose();
            }
            _disposed = true;
        }

        ~Celestial_Body()
        {
            Dispose(false);
        }

        public Celestial_Body(double x, double y, double mass, Vector StartingVelocity, string name = "")
        {
            Position = new Vector(x, y);
            Mass = mass;
            InvMass = 1.0 / mass;
            Velocity = StartingVelocity;
            Pen.Color = Color;
            OrbitPen = new Pen(Color.FromArgb(150, Color));
            TrailPen.Color = Color.FromArgb(Color.A / 2, Color);
            OrbitalBrush = new SolidBrush(Color.FromArgb(150, Color));
            CalcRadius();
            Name = name;
            TextWidth = TextRenderer.MeasureText(name, Control.DefaultFont);
        }
        public Celestial_Body() { }

        public double GetTimeToPeriapsis()
        {
            if (DominantBody == null) return 0;

            double t0 = 0;
            double t1 = EScal < 1.0 ? OrbitalPeriod : Math.Abs(A) / !(Velocity - DominantBody.Velocity);

            for (int i = 0; i < 50; i++)
            {
                double m1 = t0 + (t1 - t0) / 3.0;
                double m2 = t1 - (t1 - t0) / 3.0;

                double d1 = !(GetPositionAtTime(m1) - DominantBody.Position);
                double d2 = !(GetPositionAtTime(m2) - DominantBody.Position);

                if (d1 > d2) t0 = m1;
                else t1 = m2;
            }

            return (t0 + t1) * 0.5;
        }
        public void DrawPostSOIOrbit(Graphics g, Vector offset, double scale, float screenW, float screenH)
        {
            if (DominantBody == null || DominantBody.DominantBody == null) return;
            if (EScal < 1.0) return; 

            double rSOI = Math.Pow(DominantBody.Mass / DominantBody.DominantBody.Mass, 0.4) * !(DominantBody.Position - DominantBody.DominantBody.Position);

            double tExit = FindSOIExitTime(rSOI);
            if (double.IsNaN(tExit)) return;

            Vector exitPos = GetPositionAtTime(tExit);
            Vector exitVel = GetVelocityAtTime(tExit);

            Vector parentVel = exitVel + DominantBody.Velocity;

            SharedData.ghost.Position = exitPos;
            SharedData.ghost.Velocity = exitVel;
            SharedData.ghost.Mass = Mass;
            SharedData.ghost.Color = Color.FromArgb(40, Color);
            SharedData.ghost.OrbitPen = OrbitPen;
            SharedData.ghost.OrbitalBrush = OrbitalBrush;
            SharedData.ghost.DominantBody = DominantBody.DominantBody;
            SharedData.ghost.CalculateOrbit();
            SharedData.ghost.OrbitalDirty = true;
            SharedData.ghost.HyperbolaDirty = true;
            SharedData.ghost.DrawOrbit(g, offset, scale, screenW, screenH);
        }

        double FindSOIExitTime(double rSOI)
        {
            double tMax = OrbitalPeriod > 0 ? OrbitalPeriod : 1e7;
            double step = tMax / 100.0;
            double tOut = double.NaN;

            for (double t = 0; t < tMax * 10; t += step)
            {
                Vector pos = GetPositionAtTime(t);
                if (!(pos - DominantBody.Position) > rSOI)
                {
                    tOut = t;
                    break;
                }
            }

            if (double.IsNaN(tOut)) return double.NaN;

            double t0 = tOut - step, t1 = tOut;
            for (int i = 0; i < 40; i++)
            {
                double mid = (t0 + t1) * 0.5;
                Vector pos = GetPositionAtTime(mid);
                if (!(pos - DominantBody.Position) > rSOI) t1 = mid;
                else t0 = mid;
            }

            return t1;
        }

        public int HierarchyDepth()
        {
            int depth = 0;

            Celestial_Body current = DominantBody;

            while (current != null)
            {
                depth++;
                current = current.DominantBody;
            }

            return depth;
        }

        public bool IsInsideSOI(Celestial_Body target, double rSOI)
        {
            if (target.DominantBody == null)
                return false;

            double dist = !(Position - target.Position);

            return dist <= rSOI;
        }

        public bool IsAncestorOf(Celestial_Body body)
        {
            Celestial_Body current = body;

            while (current != null)
            {
                if (current == this)
                    return true;

                current = current.DominantBody;
            }

            return false;
        }

        public double SolveForKeplerE(double M)
        {
            M %= 2 * Math.PI;
            if (M < 0) M += 2 * Math.PI;

            double E = (EScal < 0.8) ? M : Math.PI;

            const double precision = 1e-8;

            for (int i = 0; i < 50; i++)
            {
                double f = E - EScal * Math.Sin(E) - M;
                double fp = 1 - EScal * Math.Cos(E);

                double delta = f / fp;

                E -= delta;

                if (Math.Abs(delta) < precision)
                    break;
            }

            return E;
        }

        public bool CanPossiblyIntersect(Celestial_Body target, double rSOI)
        {
            if (target.DominantBody != DominantBody)
            {
                double myDist = (EScal < 1.0) ? ApoapsisHeight : PeriapsisHeight;
                double targetDist = (target.EScal < 1.0) ? target.ApoapsisHeight : target.PeriapsisHeight;

                double dist = (myDist + targetDist + rSOI) * (myDist + targetDist + rSOI);

                if ((Focus1 - target.Focus1).SquaredMagnitude() < dist) return true;
                if ((Focus1 - target.Focus2).SquaredMagnitude() < dist) return true;
                if ((Focus2 - target.Focus1).SquaredMagnitude() < dist) return true;
                if ((Focus2 - target.Focus2).SquaredMagnitude() < dist) return true;
                return false;
            }
            else
            {
                if (EScal > 1.0)
                {
                    double absA = Math.Abs(A);
                    double GM = SharedData.G * (DominantBody.Mass + Mass);
                    double n = Math.Sqrt(GM / (absA * absA * absA));

                    Vector eDir = ~E;
                    Vector relPos = Position - DominantBody.Position;
                    Vector relVel = Velocity - DominantBody.Velocity;
                    double hz = relPos.X * relVel.Y - relPos.Y * relVel.X;
                    Vector ePerp = hz >= 0 ? new Vector(-eDir.Y, eDir.X) : new Vector(eDir.Y, -eDir.X);

                    double x_local = relPos * eDir;
                    double coshH = EScal - x_local / absA;
                    coshH = Math.Max(coshH, 1.0 + 1e-10);
                    double Hnow = Math.Log(coshH + Math.Sqrt(coshH * coshH - 1));

                    int sampleNum = 10;
                    for (int k = 0; k < sampleNum; k++)
                    {
                        double H = Hnow + k * (6.0 - Hnow) / (sampleNum - 1);
                        double xFut = absA * (EScal - Math.Cosh(H));
                        double yFut = absA * Math.Sqrt(EScal * EScal - 1) * Math.Sinh(H);
                        Vector futPos = new Vector(DominantBody.Position.X + eDir.X * xFut + ePerp.X * yFut, DominantBody.Position.Y + eDir.Y * xFut + ePerp.Y * yFut);
                        double dist = !(futPos - target.Position);
                        if (dist < target.ApoapsisHeight + rSOI) return true;
                    }
                    return false;
                }
                if (ApoapsisHeight < target.PeriapsisHeight - rSOI ||
                    target.ApoapsisHeight < PeriapsisHeight - rSOI) return false;

                int samples = 32;
                double step = 2 * Math.PI / samples;
                for (int i = 0; i < samples; i++)
                {
                    double a1 = i * step;
                    double r1 = OrbitalRadiusAtWorldAngle(a1);
                    double r2 = target.OrbitalRadiusAtWorldAngle(a1);
                    if (Math.Abs(r1 - r2) < rSOI) return true;
                }
                return false;
            }
        }

        double SolveForKeplerH(double M)
        {
            double H = Math.Log(2 * Math.Abs(M) / EScal + 1.8);
            if (M < 0) H = -H;

            const double precision = 1e-8;

            for (int i = 0; i < 20; i++)
            {
                double f = EScal * Math.Sinh(H) - H - M;

                double fp = EScal * Math.Cosh(H) - 1;

                H -= f / fp;

                if (Math.Abs(f / fp) < precision)
                    break;
            }

            return H;
        }

        public Vector GetVelocityAtTime(double dTime)
        {
            if (EScal < 1.0)
            {
                Vector relPos = Position - DominantBody.Position;
                Vector relVel = Velocity - DominantBody.Velocity;
                double hz = relPos.X * relVel.Y - relPos.Y * relVel.X;

                double GM = SharedData.G * (DominantBody.Mass + Mass);
                double absA = Math.Abs(A);

                CalculateEccentricityAnomaly(Position);
                double Enow = EccentricAnomaly;
                if (hz < 0) Enow = -Enow;

                double Mnow = Enow - EScal * Math.Sin(Enow);
                double Mfut = Mnow + AvgAngSpeed * dTime;
                double Efut = SolveForKeplerE(Mfut);

                double denom = 1 - EScal * Math.Cos(Efut);
                double vx = -Math.Sqrt(GM / (absA)) * Math.Sin(Efut) / denom;
                double vy = Math.Sqrt(GM / (absA)) * Math.Sqrt(1 - EScal * EScal) * Math.Cos(Efut) / denom;

                if (hz < 0) vy = -vy;

                double worldVx = vx * CosAngle - vy * SinAngle;
                double worldVy = vx * SinAngle + vy * CosAngle;

                return new Vector(DominantBody.Velocity.X + worldVx, DominantBody.Velocity.Y + worldVy);
            }
            else
            {
                double absA = Math.Abs(A);
                double GM = SharedData.G * (DominantBody.Mass + Mass);
                double n = Math.Sqrt(GM / (absA * absA * absA));

                Vector eDir = ~E;
                Vector relPos = Position - DominantBody.Position;
                Vector relVel = Velocity - DominantBody.Velocity;
                double hz = relPos.X * relVel.Y - relPos.Y * relVel.X;

                Vector ePerp = hz >= 0 ? new Vector(-eDir.Y, eDir.X) : new Vector(eDir.Y, -eDir.X);

                double x_local = relPos * eDir;
                double y_local = relPos * ePerp;

                double coshH = EScal - x_local / absA;
                coshH = Math.Max(coshH, 1.0 + 1e-10);
                double Hnow = Math.Log(coshH + Math.Sqrt(coshH * coshH - 1));
                if (y_local < 0) Hnow = -Hnow;

                double Mnow = EScal * Math.Sinh(Hnow) - Hnow;
                double Mfut = Mnow + n * dTime;
                double Hfut = SolveForKeplerH(Mfut);

                double denom = EScal * Math.Cosh(Hfut) - 1;
                double vx = -Math.Sqrt(GM / absA) * Math.Sinh(Hfut) / denom;
                double vy = Math.Sqrt(GM / absA) * Math.Sqrt(EScal * EScal - 1) * Math.Cosh(Hfut) / denom;

                return new Vector(DominantBody.Velocity.X + eDir.X * vx + ePerp.X * vy, DominantBody.Velocity.Y + eDir.Y * vx + ePerp.Y * vy);
            }
        }

        public Vector GetPositionAtTime(double dTime)
        {
            if (EScal < 1.0)
            {
                Vector relPos = Position - DominantBody.Position;
                Vector relVel = Velocity - DominantBody.Velocity;
                double hz = relPos.X * relVel.Y - relPos.Y * relVel.X;
                double signedAngSpeed = hz >= 0 ? AvgAngSpeed : -AvgAngSpeed;

                CalculateEccentricityAnomaly(Position);
                double Enow = EccentricAnomaly;
                if (hz < 0) Enow = -Enow;

                double Mnow = Enow - EScal * Math.Sin(Enow);
                double Mfut = Mnow + AvgAngSpeed * dTime;
                double Efut = SolveForKeplerE(Mfut);

                double x = A * (Math.Cos(Efut) - EScal);
                double y = A * Math.Sqrt(1 - EScal * EScal) * Math.Sin(Efut);
                if (hz < 0) y = -y;

                double worldRelX = x * CosAngle - y * SinAngle;
                double worldRelY = x * SinAngle + y * CosAngle;
                return new Vector(DominantBody.Position.X + worldRelX, DominantBody.Position.Y + worldRelY);
            }
            else
            {
                double absA = Math.Abs(A);
                double GM = SharedData.G * (DominantBody.Mass + Mass);
                double n = Math.Sqrt(GM / (absA * absA * absA));

                Vector eDir = ~E;
                Vector relPos = Position - DominantBody.Position;
                Vector relVel = Velocity - DominantBody.Velocity;

                double hz = relPos.X * relVel.Y - relPos.Y * relVel.X;

                Vector ePerp = hz >= 0 ? new Vector(-eDir.Y, eDir.X) : new Vector(eDir.Y, -eDir.X);

                double x_local = relPos * eDir;
                double y_local = relPos * ePerp;

                double coshH = EScal - x_local / absA;
                coshH = Math.Max(coshH, 1.0 + 1e-10);
                double Hnow = Math.Log(coshH + Math.Sqrt(coshH * coshH - 1));
                if (y_local < 0) Hnow = -Hnow;

                double Mnow = EScal * Math.Sinh(Hnow) - Hnow;
                double Mfut = Mnow + n * dTime;
                double Hfut = SolveForKeplerH(Mfut);

                double xFut = absA * (EScal - Math.Cosh(Hfut));
                double yFut = absA * Math.Sqrt(EScal * EScal - 1) * Math.Sinh(Hfut);

                return new Vector(DominantBody.Position.X + eDir.X * xFut + ePerp.X * yFut, DominantBody.Position.Y + eDir.Y * xFut + ePerp.Y * yFut);
            }
        }
        double[] times = new double[100];
        public void GetIntersectionPosition(Celestial_Body target, double rSOI)
        {
            int steps = 20;
            if (EScal > 1.0 || target.EScal > 1.0) steps = 100;

            if (EScal < 1.0)
            {
                Vector relPos3 = Position - DominantBody.Position;
                Vector relVel3 = Velocity - DominantBody.Velocity;
                double hz3 = relPos3.X * relVel3.Y - relPos3.Y * relVel3.X;
                double signedSpeed = hz3 >= 0 ? AvgAngSpeed : -AvgAngSpeed;

                CalculateEccentricityAnomaly(Position);
                double Enow3 = hz3 >= 0 ? EccentricAnomaly : -EccentricAnomaly;
                double MnowE = Enow3 - EScal * Math.Sin(Enow3);
                for (int k = 0; k < steps; k++)
                {
                    double Mfut = MnowE + signedSpeed * (k * OrbitalPeriod / steps);
                    times[k] = k * OrbitalPeriod / steps; 
                }
            }
            else
            {
                double absA = Math.Abs(A);
                double GM = SharedData.G * (DominantBody.Mass + Mass);
                double n = Math.Sqrt(GM / (absA * absA * absA));

                Vector eDir2 = ~E;
                Vector relPos2 = Position - DominantBody.Position;
                Vector relVel2 = Velocity - DominantBody.Velocity;

                double hz2 = relPos2.X * relVel2.Y - relPos2.Y * relVel2.X;
                Vector ePerp2 = hz2 >= 0 ? new Vector(-eDir2.Y, eDir2.X) : new Vector(eDir2.Y, -eDir2.X);

                double x_local2 = relPos2 * eDir2;
                double y_local2 = relPos2 * ePerp2;

                double coshH2 = EScal - x_local2 / absA;
                coshH2 = Math.Max(coshH2, 1.0 + 1e-10);
                double Hnow2 = Math.Log(coshH2 + Math.Sqrt(coshH2 * coshH2 - 1));
                if (y_local2 < 0) Hnow2 = -Hnow2;
                double Mnow2 = EScal * Math.Sinh(Hnow2) - Hnow2;

                double Hstart = Hnow2;
                double Hend = 6.0;
                for (int k = 0; k < steps; k++)
                {
                    double H = Hstart + k * (Hend - Hstart) / (steps - 1);
                    double M = EScal * Math.Sinh(H) - H;
                    times[k] = (M - Mnow2) / n;
                }
            }

            Vector startShip = GetPositionAtTime(times[0]);
            Vector startTarget = target.GetPositionAtTime(times[0]);
            double prevDist = !(startShip - startTarget);
            bool intersected = false;

            for (int k = 1; k < steps; k++)
            {
                double t = times[k];
                double tPrev = times[k - 1];

                Vector shipPos = GetPositionAtTime(t);
                Vector targetPos = target.GetPositionAtTime(t);
                double dist = !(shipPos - targetPos);

                if (prevDist > rSOI && dist <= rSOI)
                {
                    double t0 = tPrev, t1 = t;
                    for (int i = 0; i < 20; i++)
                    {
                        double mid = (t0 + t1) * 0.5;
                        Vector sPos = GetPositionAtTime(mid);
                        Vector tPos = target.GetPositionAtTime(mid);
                        if (!(sPos - tPos) <= rSOI) t1 = mid;
                        else t0 = mid;
                    }
                    HasIntersection = true;
                    TimeToIntersection = t1;
                    Intersection = GetPositionAtTime(t1);
                    IntersectingBody = target;
                    intersected = true;
                    break;
                }
                prevDist = dist;
            }

            if (!intersected && IntersectingBody == target)
            {
                HasIntersection = false;
                IntersectingBody = null;
            }
        }
        public void CalculateHyperbolicAnomaly(Vector pos)
        {
            Vector r = pos - DominantBody.Position;

            double localX = r.X * CosAngle + r.Y * SinAngle;
            double localY = -r.X * SinAngle + r.Y * CosAngle;

            double absA = Math.Abs(A);
            double ratio = (localX / absA) + EScal;
            ratio = Math.Max(ratio, 1.0 + 1e-10);
            HyperbolicAnomaly = Math.Log(ratio + Math.Sqrt(ratio * ratio - 1));

            Vector relVel = Velocity - DominantBody.Velocity;
            if ((r * relVel) < 0) HyperbolicAnomaly = -HyperbolicAnomaly;

        }

        public void CalculateEccentricityAnomaly(Vector pos)
        {
            Vector r = pos - DominantBody.Position;

            double localX = r.X * CosAngle + r.Y * SinAngle;
            double localY = -r.X * SinAngle + r.Y * CosAngle;

            EccentricAnomaly = Math.Atan2(localY / (A * Math.Sqrt(1 - EScal * EScal)), (localX / A) + EScal);
        }

        double OrbitalRadiusAtWorldAngle(double worldAngle)
        {
            double nu = worldAngle - AngleRad;
            if (EScal >= 1.0)
            {
                double cosNu = Math.Cos(nu);
                if (1 + EScal * cosNu <= 0) return double.MaxValue;
                return Math.Abs(A) * (EScal * EScal - 1) / (1 + EScal * cosNu);
            }
            return A * (1 - EScal * EScal) / (1 + EScal * Math.Cos(nu));
        }

        public Vector TrueAnomalyToWorld(double nu)
        {
            double r = A * (1 - EScal * EScal) / (1 + EScal * Math.Cos(nu));
            double angleRad = Angle * Math.PI / 180.0;
            double localX = r * Math.Cos(nu);
            double localY = r * Math.Sin(nu);
            double worldX = DominantBody.Position.X + localX * Math.Cos(angleRad) - localY * Math.Sin(angleRad);
            double worldY = DominantBody.Position.Y + localX * Math.Sin(angleRad) + localY * Math.Cos(angleRad);
            return new Vector(worldX, worldY);
        }
        public bool IsOnScreen(Vector offset, double scale, float screenW, float screenH)
        {
            float bScreenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float bScreenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            float sRadius = SharedData.PutInScreenScaleClamp(Radius);

            bool visible = bScreenX + sRadius > 0 && bScreenX - sRadius < screenW && bScreenY + sRadius > 0 && bScreenY - sRadius < screenH;
            if (visible) return true;

            return false;
        }
        List<PointF> validPoints = new List<PointF>(200);

        public void UpdateTrailPath(Vector offset, double scale, int screenW, int screenH)
        {
            if (!TrailDirty) return;

            TrailPath.Reset();
            if (TrailCount < 2) return;

            float limit = 10000f;

            validPoints.Clear();

            for (int j = 0; j < TrailCount; j++)
            {
                int idx = (TrailHead - TrailCount + j + 200) % 200;

                float x = SharedData.PutInScreenPosScaleXClamp(Trail[idx].X);
                float y = SharedData.PutInScreenPosScaleYClamp(Trail[idx].Y);

                if (Math.Abs(x) < limit && Math.Abs(y) < limit)
                {
                    validPoints.Add(new PointF(x, y));
                }
                else if (validPoints.Count > 0)
                {
                    if (validPoints.Count > 1) TrailPath.AddLines(validPoints.ToArray());
                    validPoints.Clear();
                }
            }

            if (validPoints.Count > 1)
            {
                TrailPath.AddLines(validPoints.ToArray());
            }

            TrailDirty = false;
        }
        public void CalculateOrbit()
        {
            if ((!OrbitalDirty && !HyperbolaDirty) || DominantBody == null) return;
            Vector relativeVelocity = Velocity - DominantBody.Velocity;
            double relativeSpeed = !relativeVelocity;
            Vector relativeDistance = Position - DominantBody.Position;
            double distance = !relativeDistance;
            double GM = SharedData.G * (DominantBody.Mass + Mass);
            Focus1 = DominantBody.Position;
            A = 1 / (2 / distance - (relativeSpeed * relativeSpeed) / GM);
            E = (relativeDistance % ((relativeSpeed * relativeSpeed) - GM / distance) - relativeVelocity % (relativeDistance * relativeVelocity)) % (1 / GM);
            EScal = !E;

            double absA = Math.Abs(A);
            if (EScal < 1)
            {
                B = A * Math.Sqrt(1 - EScal * EScal);
                C = A * EScal;
                Focus2 = new Vector(DominantBody.Position.X - 2 * (~E).X * C, DominantBody.Position.Y - 2 * (~E).Y * C);
                if (EScal > 0.001)
                {
                    Apoapsis = DominantBody.Position - (~E % (A * (1 + EScal)));
                    Periapsis = DominantBody.Position + (~E % (Math.Abs(A) * (1 - EScal)));
                    TimeToPeriapsis = GetTimeToPeriapsis();
                    TimeToApoapsis = TimeToPeriapsis + OrbitalPeriod / 2.0;
                    if(TimeToApoapsis > OrbitalPeriod) TimeToApoapsis-= OrbitalPeriod;
                }
                ApoapsisHeight = A * (1 + !E);
                PeriapsisHeight = A * (1 - !E);
            }
            else
            {
                B = absA * Math.Sqrt(EScal * EScal - 1);
                if (EScal > 0)
                {
                    Periapsis = DominantBody.Position - (~E % (Math.Abs(A) * (1 - EScal)));
                    TimeToPeriapsis = GetTimeToPeriapsis();
                }
                PeriapsisHeight = A * (1 - !E);
            }
            double centerWorldX = DominantBody.Position.X - (~E).X * C;
            double centerWorldY = DominantBody.Position.Y - (~E).Y * C;
            OrbitCenter = new Vector(centerWorldX, centerWorldY);
            OrbitCenterScreen = new Vector(SharedData.PutInScreenPosScaleX(OrbitCenter.X), SharedData.PutInScreenPosScaleY(OrbitCenter.Y));

            Angle = Math.Atan2(E.Y, E.X) * (180 / Math.PI);
            AngleRad = Angle/(180 / Math.PI);
            AvgAngSpeed = Math.Sqrt((SharedData.G * (DominantBody.Mass + Mass)) / (absA * absA * absA));
            OrbitalPeriod = 2 * Math.PI * Math.Sqrt((A * A * A) / (SharedData.G * (DominantBody.Mass+Mass)));
            CosAngle = Math.Cos(AngleRad);
            SinAngle = Math.Sin(AngleRad);
            CosMAngle = Math.Cos(-AngleRad);
            SinMAngle = Math.Sin(-AngleRad);
            OrbitalDirty = false;
            HyperbolaDirty = false;
        }

        public void UpdateHyperbolaPath(Vector offset, double scale, int screenW, int screenH)
        {
            if (!HyperbolaDirty || DominantBody == null || A >= 0) return;

            HyperbolaPath.Reset();

            double absA = Math.Abs(A);
            double absB = absA * Math.Sqrt(EScal * EScal - 1);
            Vector eDir = ~E;
            Vector ePerp = new Vector(-eDir.Y, eDir.X);

            double rSOI = DominantBody.DominantBody != null ? Math.Pow(DominantBody.Mass / DominantBody.DominantBody.Mass, 0.4) * !(DominantBody.Position - DominantBody.DominantBody.Position) : double.MaxValue;

            float safeLimit = 50000f;
            List<PointF> pathPoints = new List<PointF>();

            for (float t = -3f; t <= 3f; t += 0.05f)
            {
                double x = absA * Math.Cosh(t);
                double y = absB * Math.Sinh(t);

                double wX = DominantBody.Position.X - eDir.X * x + ePerp.X * y + eDir.X * (absA * EScal);
                double wY = DominantBody.Position.Y - eDir.Y * x + ePerp.Y * y + eDir.Y * (absA * EScal);

                double distToDominant = Math.Sqrt((wX - DominantBody.Position.X) * (wX - DominantBody.Position.X) + (wY - DominantBody.Position.Y) * (wY - DominantBody.Position.Y));
                if (distToDominant > rSOI)
                {
                    if (pathPoints.Count > 1)
                    {
                        HyperbolaPath.AddLines(pathPoints.ToArray());
                        HyperbolaPath.StartFigure();
                    }
                    pathPoints.Clear();
                    continue;
                }

                float sX = SharedData.PutInScreenPosScaleXClamp(wX);
                float sY = SharedData.PutInScreenPosScaleYClamp(wY);

                if (Math.Abs(sX) < safeLimit && Math.Abs(sY) < safeLimit)
                {
                    pathPoints.Add(new PointF(sX, sY));
                }
                else if (pathPoints.Count > 1)
                {
                    HyperbolaPath.AddLines(pathPoints.ToArray());
                    pathPoints.Clear();
                    HyperbolaPath.StartFigure();
                }
            }

            if (pathPoints.Count > 1) HyperbolaPath.AddLines(pathPoints.ToArray());

            HyperbolaDirty = false;
        }
        public void DrawHyperbola(Graphics g, Vector offset, double scale, float screenW, float screenH)
        {
            if (DominantBody == null || A >= 0) return;

            UpdateHyperbolaPath(offset, scale, (int)screenW, (int)screenH);
            float focus1X = SharedData.PutInScreenPosScaleXClamp(Focus1.X);
            float focus1Y = SharedData.PutInScreenPosScaleYClamp(Focus1.Y);

            if (Periapsis.X != 0 && Periapsis.Y != 0 && EScal >0.01)
            {
                float periX = SharedData.PutInScreenPosScaleXClamp(Periapsis.X);
                float periY = SharedData.PutInScreenPosScaleYClamp(Periapsis.Y);

                g.DrawEllipse(OrbitPen, periX, periY, 6, 6);
                g.DrawString("Pe : " + SharedData.SizeScale(PeriapsisHeight), Control.DefaultFont, OrbitalBrush, periX -3 + 10, periY -3 - 10);
                g.DrawEllipse(OrbitPen, focus1X - 10, focus1Y - 10, 20, 20);
            }

            if (HyperbolaPath.PointCount > 1)
            {
                g.DrawPath(OrbitPen, HyperbolaPath);
            }
        }

        PointF[] screenEdges = new PointF[4];
        PointF[] orbitEdges = new PointF[4];

        float Cross(PointF O, PointF A, PointF B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        bool SegmentsIntersect(PointF A, PointF B, PointF C, PointF D)
        {
            float d1 = Cross(C, D, A);
            float d2 = Cross(C, D, B);
            float d3 = Cross(A, B, C);
            float d4 = Cross(A, B, D);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        PointF Ao = new PointF();
        PointF Bo = new PointF();
        PointF Co = new PointF();
        PointF Do = new PointF();

        void GetOrbitBoundingBox()
        {
            float xp = SharedData.PutInScreenPosScaleXClamp(Periapsis.X);
            float yp = SharedData.PutInScreenPosScaleYClamp(Periapsis.Y);
            float xa = SharedData.PutInScreenPosScaleXClamp(Apoapsis.X);
            float ya = SharedData.PutInScreenPosScaleYClamp(Apoapsis.Y);
            float bS = SharedData.PutInScreenScaleClamp(B);
            float aS = SharedData.PutInScreenScaleClamp(A);

            float tx = (ya - yp) * bS / (2*aS);
            float ty = (xa - xp) / (yp - ya);
            if (yp - ya == 0) ty = 0;

            float x1 = xp + tx;
            float x2 = xp - tx;
            float x3 = xa + tx;
            float x4 = xa - tx;

            float y1 = ty * x1 - ty * xp + yp;
            float y2 = ty * x2 - ty * xp + yp;
            float y3 = ty * x3 - ty * xa + ya;
            float y4 = ty * x4 - ty * xa + ya;

            Ao.X = x1; Ao.Y = y1;
            Bo.X = x2; Bo.Y = y2;
            Co.X = x3; Co.Y = y3;
            Do.X = x4; Do.Y = y4;

            orbitEdges[0] = Ao;
            orbitEdges[1] = Bo;
            orbitEdges[2] = Do;
            orbitEdges[3] = Co;
        }

        PointF S1 = new PointF(0, 0);
        PointF S2 = new PointF(SharedData.SW, 0);
        PointF S3 = new PointF(SharedData.SW, SharedData.SH);
        PointF S4 = new PointF(0, SharedData.SH);

        double CalcAreaMatrix(PointF A, PointF B, PointF C)
        {
            return Math.Abs(A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
        }

        bool CheckArea()
        {
            for(int i = 0; i< 4; i++)
            {
                double sum = 0.5 * CalcAreaMatrix(orbitEdges[0], orbitEdges[1], screenEdges[i]) + 0.5 * CalcAreaMatrix(orbitEdges[1], orbitEdges[2], screenEdges[i]) + 0.5 * CalcAreaMatrix(orbitEdges[2], orbitEdges[3], screenEdges[i]) + 0.5 * CalcAreaMatrix(orbitEdges[3], orbitEdges[0], screenEdges[i]);
                if (sum >= CalcAreaMatrix(orbitEdges[0], orbitEdges[1], orbitEdges[2])- CalcAreaMatrix(orbitEdges[0], orbitEdges[1], orbitEdges[2])/100 && sum <= CalcAreaMatrix(orbitEdges[0], orbitEdges[1], orbitEdges[2]) + CalcAreaMatrix(orbitEdges[0], orbitEdges[1], orbitEdges[2]) / 100) return true;
            }
            return false;
        }

        bool CheckIfShouldDrawOrbit(float aScreen, float bScreen)
        { 
            GetOrbitBoundingBox();

            screenEdges[0] = S1;
            screenEdges[1] = S2;
            screenEdges[2] = S3;
            screenEdges[3] = S4;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (SegmentsIntersect(screenEdges[i], screenEdges[(i + 1) % 4], orbitEdges[j], orbitEdges[(j + 1) % 4])) return true;
                }
            }

            if (orbitEdges[0].X > 0 && orbitEdges[0].X < SharedData.SW && orbitEdges[0].Y > 0 && orbitEdges[0].Y < SharedData.SH) return true;
            if (orbitEdges[1].X > 0 && orbitEdges[1].X < SharedData.SW && orbitEdges[1].Y > 0 && orbitEdges[1].Y < SharedData.SH) return true;
            if (orbitEdges[2].X > 0 && orbitEdges[2].X < SharedData.SW && orbitEdges[2].Y > 0 && orbitEdges[2].Y < SharedData.SH) return true;
            if (orbitEdges[3].X > 0 && orbitEdges[3].X < SharedData.SW && orbitEdges[3].Y > 0 && orbitEdges[3].Y < SharedData.SH) return true;

            return CheckArea();
        }

        public void DrawOrbit(Graphics g, Vector offset, double scale, float screenW, float screenH)
        {
            if (DominantBody == null || DominantBody.Mass < Mass) return;
            if (EScal >= 1.0)
            {
                DrawHyperbola(g, offset, scale, screenW, screenH);
                return;
            }

            //if (eScal >= 0.999) return;
            float aScreen = SharedData.PutInScreenScaleClamp(A);
            float bScreen = SharedData.PutInScreenScaleClamp(B);

            float focus1X = SharedData.PutInScreenPosScaleXClamp(Focus1.X);
            float focus1Y = SharedData.PutInScreenPosScaleYClamp(Focus1.Y);
            float focus2X = SharedData.PutInScreenPosScaleXClamp(Focus2.X);
            float focus2Y = SharedData.PutInScreenPosScaleYClamp(Focus2.Y);

            float periapsisX = SharedData.PutInScreenPosScaleXClamp(Periapsis.X);
            float periapsisY = SharedData.PutInScreenPosScaleYClamp(Periapsis.Y);
            float apoapsisX = SharedData.PutInScreenPosScaleXClamp(Apoapsis.X);
            float apoapsisY = SharedData.PutInScreenPosScaleYClamp(Apoapsis.Y);

            if (aScreen < 20 && bScreen < 20) return;
            if (aScreen > 1e6f || bScreen > 1e6f) return;
            if (!CheckIfShouldDrawOrbit(aScreen, bScreen)) return;

            if (Periapsis.X != 0 && Periapsis.Y != 0 && Apoapsis.X != 0 && Apoapsis.Y != 0 && EScal > 0.01)
            {
                g.DrawEllipse(OrbitPen, periapsisX-3, periapsisY-3, 6, 6);
                g.DrawString("Pe: " + SharedData.SizeScale(PeriapsisHeight), Control.DefaultFont, OrbitalBrush, periapsisX-3 + 10, periapsisY-3 - 10);
                g.DrawEllipse(OrbitPen, apoapsisX-3, apoapsisY-3, 6, 6);
                g.DrawString("Ap: " + SharedData.SizeScale(ApoapsisHeight), Control.DefaultFont, OrbitalBrush, apoapsisX - 3 + 10, apoapsisY - 3 - 10);
                g.DrawEllipse(OrbitPen, focus1X - 10, focus1Y - 10, 20, 20);
                g.DrawEllipse(OrbitPen, focus2X - 10, focus2Y - 10, 20, 20);
            }
            //SharedData.intersectionsPredicted++;
            g.TranslateTransform(SharedData.PutInScreenPosScaleXClamp(OrbitCenter.X), SharedData.PutInScreenPosScaleYClamp(OrbitCenter.Y));
            g.RotateTransform((float)Angle);
            g.DrawEllipse(OrbitPen, -aScreen, -bScreen, 2 * aScreen, 2 * bScreen);
            g.ResetTransform();
            
        }

        public void DrawSOI(Graphics g, float screenW, float screenH, double dr, SolidBrush sb)
        {
            float screenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            double dscreenR = SharedData.PutInScreenScaleClamp(dr);

            if (dscreenR > screenW * 1000 || dscreenR < 1 || screenX > 1e9 || screenX < -1e6 || screenY > 1e9 || screenY < -1e6) return;
            float screenR = (float)dscreenR;

            g.FillEllipse(sb, screenX - screenR, screenY - screenR, 2 * screenR, 2 * screenR);
        }

        abstract public void Draw(Graphics g, Vector offset, float screenW, float screenH);
        abstract public void CalcRadius();
        abstract public void DecideColor(double Smass);
        abstract public void RotateShip(Vector relVel, Vector relPos);
        abstract public void ThrottleShip();
    }

    class BlackHole : Celestial_Body
    {
        public BlackHole(double x, double y, double mass, Vector StartingVelocity, string name = "") : base(x, y, mass, StartingVelocity, name) 
        {
            Color = Color.Black;
            Pen.Color = Color.FromArgb(Color.A / 2, Color.White);
            Brush.Color = Color;
            TrailPen.Color = Color.FromArgb(40, Color);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                aDiskO?.Dispose();
                aDiskY?.Dispose();
                Whitepen?.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void CalcRadius()
        {
            Radius = (2 * SharedData.G * Mass) / (SharedData.c * SharedData.c);
        }
        SolidBrush aDiskO = new SolidBrush(Color.Orange);
        SolidBrush aDiskY = new SolidBrush(Color.Yellow);
        Pen Whitepen = new Pen(Color.White, 2);
        override public void Draw(Graphics g, Vector offset, float screenW, float screenH)
        {
            double screenXd = SharedData.PutInScreenPosScaleX(Position.X);
            double screenYd = SharedData.PutInScreenPosScaleY(Position.Y);
            double rd = SharedData.PutInScreenScale(Radius);

            if (double.IsNaN(screenXd) || double.IsInfinity(screenXd)) screenXd = screenW / 2.0f;
            if (double.IsNaN(screenYd) || double.IsInfinity(screenYd)) screenYd = screenH / 2.0f;
            if (double.IsNaN(rd) || double.IsInfinity(rd)) rd = double.MaxValue / 2.0f;

            if (rd > 50000)
            {
                double dx = screenXd - (screenW / 2.0);
                double dy = screenYd - (screenH / 2.0);
                double distCenterToScreen = Math.Sqrt(dx * dx + dy * dy);

                if (distCenterToScreen < rd + screenW)
                {
                    float screenDiagonal = (float)Math.Sqrt(screenW * screenW + screenH * screenH) / 2.0f;
                    if (distCenterToScreen + screenDiagonal < rd)
                    {
                        g.FillRectangle(Brush, 0, 0, screenW, screenH);
                        return;
                    }

                    float angleToCenter = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                    g.TranslateTransform(screenW / 2.0f, screenH / 2.0f);
                    g.RotateTransform(angleToCenter);
                    float surfaceDepth = (float)(rd - distCenterToScreen);
                    SharedData.ClampFloat(ref surfaceDepth);
                    g.FillRectangle(Brush, -surfaceDepth, -2 * screenH, screenW * 4, screenH * 4);
                    g.ResetTransform();
                    return;
                }
            }

            float screenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            float r = SharedData.PutInScreenScaleClamp(Radius);

            if (r < 1f) r = 1f;
            if (r > 100000) r = 100000;

            g.FillEllipse(aDiskY, screenX - 3 * r, screenY - r / 2.0f, r * 6, r);
            g.FillEllipse(aDiskY, screenX - 1.5f * r, screenY - r *1.5f, r * 3, r*3);

            g.FillEllipse(aDiskO, screenX - 2f * r, screenY - r/3.5f, r * 4f, r /(5/3f));
            g.FillEllipse(aDiskO, screenX - 1.25f * r, screenY - r *1.25f, r * 2.5f, r * 2.5f);

            g.FillEllipse(Brush, screenX - r, screenY - r, r * 2, r * 2);

            g.FillPie(aDiskY, screenX - 3 * r, screenY - r / 2.0f, r * 6, r, -180, -180);
            g.FillPie(aDiskO, screenX - 2f * r, screenY - r / 3.25f, r * 4f, r / (5 / 3f), -180, -180);

            g.DrawEllipse(Whitepen, screenX - 25, screenY - 25, 50, 50);
            g.DrawEllipse(Whitepen, screenX - 3, screenY - 3, 6, 6);
            g.DrawLine(Whitepen, screenX + 3, screenY, screenX + 25, screenY);
            g.DrawLine(Whitepen, screenX - 3, screenY, screenX - 25, screenY);
            g.DrawLine(Whitepen, screenX, screenY + 3, screenX, screenY - 25);
            g.DrawLine(Whitepen, screenX, screenY - 3, screenX, screenY + 25);

            if (r < 25)
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - TextWidth.Width / 2.0f, screenY - 50);
            }
            else
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - TextWidth.Width / 2.0f, screenY - r - 50);
            }
        }

        public override void DecideColor(double Smass) { }
        override public void RotateShip(Vector relVel, Vector relPos) { }
        override public void ThrottleShip() { }
    }

    class Star : Celestial_Body
    {
        public Star(double x, double y, double mass, Vector StartingVelocity, string name = "") : base(x, y, mass, StartingVelocity, name)
        {
            DecideColor(SharedData.SolarMass);
        }

        public override void CalcRadius()
        {
            Radius = SharedData.SolarRadius * Math.Pow(Mass / SharedData.SolarMass, 0.8);
        }

        override public void DecideColor(double Smass)
        {
            if (Mass / Smass >= 200) Color = Color.Red;
            else if (Mass / Smass >= 100) Color = Color.Orange;
            else if (Mass / Smass >= 20) Color = Color.Yellow;
            else if (Mass / Smass >= 10) Color = Color.Blue;
            else if (Mass / Smass >= 2) Color = Color.Cyan;
            else if (Mass / Smass >= 0.5) Color = Color.White;
            else if (Mass / Smass >= 0.25) Color = Color.Yellow;
            else if (Mass / Smass >= 0.1) Color = Color.Orange;
            else if (Mass / Smass >= 0.01) Color = Color.Red;
            Brush.Color = Color;   
            Pen.Color = Color;
            OrbitPen.Color = Color.FromArgb(150, Color);
            OrbitalBrush.Color = Color.FromArgb(150, Color);
            TrailPen.Color = Color.FromArgb(40, Color);
        }
        override public void Draw(Graphics g, Vector offset, float screenW, float screenH)
        {

            double screenXd = SharedData.PutInScreenPosScaleX(Position.X);
            double screenYd = SharedData.PutInScreenPosScaleY(Position.Y);
            double rd = SharedData.PutInScreenScale(Radius);

            if (double.IsNaN(screenXd) || double.IsInfinity(screenXd)) screenXd = screenW / 2.0f;
            if (double.IsNaN(screenYd) || double.IsInfinity(screenYd)) screenYd = screenH / 2.0f;
            if (double.IsNaN(rd) || double.IsInfinity(rd)) rd = double.MaxValue / 2.0f;

            if (rd > 50000)
            {
                double dx = screenXd - (screenW / 2.0);
                double dy = screenYd - (screenH / 2.0);
                double distCenterToScreen = Math.Sqrt(dx * dx + dy * dy);

                if (distCenterToScreen < rd + screenW)
                {
                    float screenDiagonal = (float)Math.Sqrt(screenW * screenW + screenH * screenH) / 2.0f;
                    if (distCenterToScreen + screenDiagonal < rd)
                    {
                        g.FillRectangle(Brush, 0, 0, screenW, screenH);
                        return;
                    }

                    float angleToCenter = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);      
                    g.TranslateTransform(screenW / 2.0f, screenH / 2.0f);
                    g.RotateTransform(angleToCenter);
                    float surfaceDepth = (float)(rd - distCenterToScreen);
                    SharedData.ClampFloat(ref surfaceDepth);
                    g.FillRectangle(Brush, -surfaceDepth, - 2* screenH, screenW*4, screenH*4);
                    g.ResetTransform();
                    return;
                }
            }

            float screenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            float r = SharedData.PutInScreenScaleClamp(Radius);

            if (r < 1f) r = 1f;
            if (r > 100000) r = 100000;

            g.FillEllipse(Brush, screenX - r, screenY - r, r * 2, r * 2);

            g.DrawEllipse(Pen, screenX - 25, screenY - 25, 50, 50);
            g.DrawEllipse(Pen, screenX - 3, screenY - 3, 6, 6);
            g.DrawLine(Pen, screenX + 3, screenY, screenX + 25, screenY);
            g.DrawLine(Pen, screenX - 3, screenY, screenX - 25, screenY);
            g.DrawLine(Pen, screenX, screenY + 3, screenX, screenY - 25);
            g.DrawLine(Pen, screenX, screenY - 3, screenX, screenY + 25);

            Size textWidth = TextRenderer.MeasureText(Name, Control.DefaultFont);
            if (r < 25)
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - 50);
            }
            else
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - r - 50);
            }
        }
        override public void RotateShip(Vector relVel, Vector relPos) { }
        override public void ThrottleShip() { }
    }

    class Planet : Celestial_Body
    {
        List<Presets.BeltParticle> viltrumParticles = new List<Presets.BeltParticle>();
        public Planet(double x, double y, double mass, Vector StartingVelocity, string name = "") : base(x, y, mass, StartingVelocity, name)
        {
            Color = Color.FromArgb(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256));
            Pen.Color = Color.FromArgb(Color.A / 2, Color);
            Brush.Color = Color;
            OrbitPen.Color = Color.FromArgb(150, Color);
            OrbitalBrush.Color = Color.FromArgb(150, Color);
            TrailPen.Color = Color.FromArgb(40, Color);
            if (name.ToUpper() == "SATURN")
                IsSaturn = true;
            if (name.ToUpper() == "VILTRUM")
                IsViltrum = true;
            if (name.ToUpper() == "URANUS")
                IsUranus = true;
            if (name.ToUpper() == "NEPTUNE")
                IsNeptune = true;
        }
        public Planet(double x, double y, double mass, Vector StartingVelocity, Color color, string name = "") : base(x, y, mass, StartingVelocity, name)
        {
            Color = color;
            Pen.Color = Color.FromArgb(color.A / 2, color);
            Brush.Color = color;
            OrbitPen.Color = Color.FromArgb(150, color);
            OrbitalBrush.Color = Color.FromArgb(150, color);
            TrailPen.Color = Color.FromArgb(40, color);
            if (name.ToUpper() == "SATURN")
                IsSaturn = true;
            if (name.ToUpper() == "VILTRUM")
                IsViltrum = true;
            if (name.ToUpper() == "URANUS")
                IsUranus = true;
            if (name.ToUpper() == "NEPTUNE")
                IsNeptune = true;
        }
        public Planet() { }

        public void DrawSaturnRings(Graphics g, SolidBrush brush, Vector centerPos, double screenW, double screenH)
        {
            Color orgColor = brush.Color;
            brush.Color = Color.FromArgb(100, Color.Tan);

            float screenX = SharedData.PutInScreenPosScaleXClamp(centerPos.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(centerPos.Y);

            float ringA = SharedData.PutInScreenScaleClamp(0.000914 * SharedData.AU);
            float cassiniDev = SharedData.PutInScreenScaleClamp(0.000816 * SharedData.AU);
            float ringB = SharedData.PutInScreenScaleClamp(0.000785 * SharedData.AU);
            float ringC = SharedData.PutInScreenScaleClamp(0.000614 * SharedData.AU);
            float empty = SharedData.PutInScreenScaleClamp(0.000498 * SharedData.AU);

            g.FillEllipse(brush, screenX - ringA, screenY - ringA, ringA * 2, ringA * 2);

            brush.Color = Color.Black;

            g.FillEllipse(brush, screenX - cassiniDev, screenY - cassiniDev, cassiniDev * 2, cassiniDev * 2);

            brush.Color = Color.FromArgb(200, Color.Tan);

            g.FillEllipse(brush, screenX - ringB, screenY - ringB, ringB * 2, ringB * 2);

            brush.Color = Color.FromArgb(200, Color.Black);

            g.FillEllipse(brush, screenX - ringC, screenY - ringC, ringC * 2, ringC * 2);

            brush.Color = Color.Black;

            g.FillEllipse(brush, screenX - empty, screenY - empty, empty * 2, empty * 2);

            brush.Color = orgColor;
        }

        public void DrawUranusRings(Graphics g, SolidBrush brush, Vector centerPos, double screenW, double screenH)
        {
            Color orgColor = brush.Color;
            brush.Color = Color.FromArgb(200, Color.LightCyan);

            float screenX = SharedData.PutInScreenPosScaleXClamp(centerPos.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(centerPos.Y);

            float ringA = SharedData.PutInScreenScaleClamp(51149000);
            float ringB = SharedData.PutInScreenScaleClamp(48300000);
            float ringC = SharedData.PutInScreenScaleClamp(44718000);
            float ringD = SharedData.PutInScreenScaleClamp(42571000);
            float empty = SharedData.PutInScreenScaleClamp(41837000);

            g.FillEllipse(brush, screenX - ringA, screenY - ringA, ringA * 2, ringA * 2);

            brush.Color = Color.FromArgb(50, Color.Black);

            g.FillEllipse(brush, screenX - ringB, screenY - ringB, ringB * 2, ringB * 2);

            brush.Color = Color.FromArgb(75, Color.Black);

            g.FillEllipse(brush, screenX - ringC, screenY - ringC, ringC * 2, ringC * 2);

            brush.Color = Color.FromArgb(100, Color.Black);

            g.FillEllipse(brush, screenX - ringD, screenY - ringD, ringD * 2, ringD * 2);

            brush.Color = Color.Black;

            g.FillEllipse(brush, screenX - empty, screenY - empty, empty * 2, empty * 2);

            brush.Color = orgColor;
        }

        public void DrawNeptuneRings(Graphics g, SolidBrush brush, Vector centerPos, double screenW, double screenH)
        {
            Color orgColor = brush.Color;
            brush.Color = Color.FromArgb(75, Color.White);

            float screenX = SharedData.PutInScreenPosScaleXClamp(centerPos.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(centerPos.Y);

            float ringA = SharedData.PutInScreenScaleClamp(62932000);
            float ringB = SharedData.PutInScreenScaleClamp(57200000);
            float ringC = SharedData.PutInScreenScaleClamp(53200000);
            float ringD = SharedData.PutInScreenScaleClamp(42900000);
            float empty = SharedData.PutInScreenScaleClamp(40900000);

            g.FillEllipse(brush, screenX - ringA, screenY - ringA, ringA * 2, ringA * 2);

            brush.Color = Color.FromArgb(100, Color.Black);

            g.FillEllipse(brush, screenX - ringB, screenY - ringB, ringB * 2, ringB * 2);

            brush.Color = Color.FromArgb(125, Color.Black);

            g.FillEllipse(brush, screenX - ringC, screenY - ringC, ringC * 2, ringC * 2);

            brush.Color = Color.FromArgb(150, Color.Black);

            g.FillEllipse(brush, screenX - ringD, screenY - ringD, ringD * 2, ringD * 2);

            brush.Color = Color.Black;

            g.FillEllipse(brush, screenX - empty, screenY - empty, empty * 2, empty * 2);

            brush.Color = orgColor;
        }

        public void DrawViltrumRings(Graphics g, SolidBrush brush, Vector centerPos, double screenW, double screenH, Vector offset)
        {
            Color orgColor = brush.Color;
            if (viltrumParticles.Count == 0)
                Presets.GenerateBelt(viltrumParticles, 7.35e-5, 6.68e-5, 1000, 0.5, 0.2, this, Presets.beltRnd);
            Presets.DrawBelt(g, viltrumParticles, brush, offset, screenW, screenH);
            brush.Color = orgColor;
        }

        public void SetColor(Color color)
        {
            this.Color = color;
            Pen.Color = Color.FromArgb(this.Color.A / 2, this.Color);
            Brush.Color = this.Color;
            OrbitPen.Color = Color.FromArgb(150, this.Color);
            OrbitalBrush.Color = Color.FromArgb(150, color);
        }

        public override void CalcRadius()
        {
            double density = 5000;
            if(Mass > 5e25)
            {
                density = 1300;
            }
            Radius = Math.Pow((3 * Mass) / (4 * Math.PI * density), 1.0 / 3.0);
        }

        override public void Draw(Graphics g, Vector offset, float screenW, float screenH)
        {
            Brush.Color = Color;
            double screenXd = SharedData.PutInScreenPosScaleX(Position.X);
            double screenYd = SharedData.PutInScreenPosScaleY(Position.Y);
            double rd = SharedData.PutInScreenScale(Radius);

            if (double.IsNaN(screenXd) || double.IsInfinity(screenXd)) screenXd = screenW / 2.0f;
            if (double.IsNaN(screenYd) || double.IsInfinity(screenYd)) screenYd = screenH / 2.0f;
            if (double.IsNaN(rd) || double.IsInfinity(rd)) rd = double.MaxValue / 2.0f;

            if (rd > 50000)
            {
                double dx = screenXd - (screenW / 2.0);
                double dy = screenYd - (screenH / 2.0);
                double distCenterToScreen = Math.Sqrt(dx * dx + dy * dy);

                if (distCenterToScreen < rd + screenW)
                {
                    float screenDiagonal = (float)Math.Sqrt(screenW * screenW + screenH * screenH) / 2.0f;
                    if (distCenterToScreen + screenDiagonal < rd)
                    {
                        g.FillRectangle(Brush, 0, 0, screenW, screenH);
                        return;
                    }

                    float angleToCenter = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                    g.TranslateTransform(screenW / 2.0f, screenH / 2.0f);
                    g.RotateTransform(angleToCenter);
                    float surfaceDepth = (float)(rd - distCenterToScreen);
                    SharedData.ClampFloat(ref surfaceDepth);
                    g.FillRectangle(Brush, -surfaceDepth,  -2*screenH, screenW * 4, screenH * 4);
                    g.ResetTransform();
                    return;
                }
            }

            float screenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            float r = SharedData.PutInScreenScaleClamp(Radius);

            if (r < 1f) r = 1f;
            if (r > 100000) r = 100000;

            if (IsSaturn)
                DrawSaturnRings(g, Brush, Position, screenW, screenH);
            else if (IsViltrum)
                DrawViltrumRings(g, Brush, Position, screenW, screenH, offset);
            else if (IsUranus)
                DrawUranusRings(g, Brush, Position, screenW, screenH);
            else if (IsNeptune)
                DrawNeptuneRings(g, Brush, Position, screenW, screenH);

            Brush.Color = Color;

            g.FillEllipse(Brush, screenX - r, screenY - r, r * 2, r * 2);

            g.DrawEllipse(Pen, screenX - 25, screenY - 25, 50, 50);
            g.DrawEllipse(Pen, screenX - 3, screenY - 3, 6, 6);
            g.DrawLine(Pen, screenX + 3, screenY, screenX + 25, screenY);
            g.DrawLine(Pen, screenX - 3, screenY, screenX - 25, screenY);
            g.DrawLine(Pen, screenX, screenY + 3, screenX, screenY - 25);
            g.DrawLine(Pen, screenX, screenY - 3, screenX, screenY + 25);
            Size textWidth = TextRenderer.MeasureText(Name, Control.DefaultFont);
            if(r<25)
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - 50);
            }
            else
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - r - 50);
            }
        }
        public override void DecideColor(double Smass) { }
        override public void RotateShip(Vector relVel, Vector relPos) { }
        override public void ThrottleShip() { }
    }
    class Spaceship : Celestial_Body
    {
        public Spaceship(double x, double y, double mass, Vector StartingVelocity, string name = "") : base(x, y, mass, StartingVelocity, name)
        {
            Color = Color.FromArgb(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256));
            Pen.Color = Color.FromArgb(Color.A / 2, Color);
            Brush.Color = Color;
            OrbitPen.Color = Color.FromArgb(150, Color);
            OrbitalBrush.Color = Color.FromArgb(150, Color);
            TrailPen.Color = Color.FromArgb(Color.A / 2, Color);
            CalcRadius();
        }

        public Spaceship(double x, double y, double mass, Vector StartingVelocity, Color color, string name = "") : base(x, y, mass, StartingVelocity, name)
        {
            Color = color; 
            Pen.Color = Color.FromArgb(color.A / 2, color);
            Brush.Color = color;
            OrbitPen.Color = Color.FromArgb(150, color);
            OrbitalBrush.Color = Color.FromArgb(150, color);
            TrailPen.Color = Color.FromArgb(40, color);
            CalcRadius();
        }

        public override void CalcRadius()
        {
            double density = 5000;
            if (Mass > 5e25)
            {
                density = 1300;
            }
            Radius = Math.Pow((3 * Mass) / (4 * Math.PI * density), 1.0 / 3.0);
        }

        override public void Draw(Graphics g, Vector offset, float screenW, float screenH)
        {
            float screenX = SharedData.PutInScreenPosScaleXClamp(Position.X);
            float screenY = SharedData.PutInScreenPosScaleYClamp(Position.Y);
            float r = SharedData.PutInScreenScaleClamp(Radius);

            if (r < 1f) r = 1f;
            if (r > 100000) r = 100000;

            PointF A = new PointF( -r, r);
            PointF B = new PointF(0, r);
            PointF C = new PointF( -r / 2.0f, 2 * r);
            PointF D = new PointF(r / 2.0f, 2 * r);
            PointF E = new PointF(r, r);
            PointF F = new PointF(r, - r);
            PointF G = new PointF(0, - 2 * r);
            PointF H = new PointF( -r,  - r);
            PointF[] pointFs = { A, B, C, D, B, E, F, G, H };

            g.TranslateTransform(screenX, screenY);
            g.RotateTransform(DirAngleSS);
            g.FillPolygon(Brush, pointFs);

            g.DrawEllipse(Pen, - 25,  - 25, 50, 50);
            g.DrawEllipse(Pen,  - 3,  - 3, 6, 6);
            g.DrawLine(Pen,  + 3, 0,  + 25, 0);
            g.DrawLine(Pen,  - 3, 0,  - 25, 0);
            g.DrawLine(Pen, 0,  + 3, 0,  - 25);
            g.DrawLine(Pen, 0,  - 3, 0,  25);
            g.DrawLine(Pen, 0, -5.0f*r, 0, r*5.0f);
            g.DrawLine(Pen, -5.0f * r, 0, r * 5.0f, 0);
            
            g.DrawPie(Pen, -4 * r, -4 * r, 8 * r, 8 * r, -90 - (DirAngleSS + 90), (DirAngleSS + 90));

            string ang = DirAngleSS.ToString("#0");
            Size textWidth = TextRenderer.MeasureText(ang, Control.DefaultFont);
            g.DrawString(ang + "°", Control.DefaultFont, Brush, -textWidth.Width/2, -5.0f * r - 20);

            g.ResetTransform();

            textWidth = TextRenderer.MeasureText(Name, Control.DefaultFont);
            if (r < 25)
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - 50);
            }
            else
            {
                g.DrawString(Name, Control.DefaultFont, Brush, screenX - textWidth.Width / 2.0f, screenY - r - 50);
            }

            g.DrawLine(Pen, screenX, screenY-4.0f * r, screenX, screenY+ r * 4.0f);
            g.DrawLine(Pen, screenX-4.0f * r, screenY, screenX + r * 4.0f, screenY);

        }

        override public void RotateShip(Vector relPos, Vector relVel) 
        {
            if (Prograde) DirAngleSS = (float)(Math.Atan2(relVel.X, -relVel.Y) * (180/ Math.PI));
            else if (Retrograde) DirAngleSS = (float)(Math.Atan2(-relVel.X, relVel.Y) * (180 / Math.PI));
            else if (Antiradial) DirAngleSS = (float)(Math.Atan2(relPos.X, -relPos.Y) * (180 / Math.PI));
            else if (Radial) DirAngleSS = (float)(Math.Atan2(-relPos.X, relPos.Y) * (180 / Math.PI));
            if (DirAngleSS < 0) DirAngleSS += 360;
            if (DirAngleSS >= 360) DirAngleSS -= 360;
        }
        override public void ThrottleShip() { }
        public override void DecideColor(double Smass) { }
    }

}
