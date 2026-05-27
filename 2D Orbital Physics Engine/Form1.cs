using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging.Effects;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace _2D_Orbital_Physics_Engine
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MouseWheel += OnMouseWheel;

            trackBar1.MouseWheel += (sender, e) => ((HandledMouseEventArgs)e).Handled = true;

        }

        Graphics g;
        double ZoomFactor = 1.3;
        double dt = 0;
        double timeScale = 10000;
        Pen pen = new Pen(Color.White);
        SolidBrush brush = new SolidBrush(Color.White);
        Celestial_Body focus;
        bool Controling = false;
        bool focused = false;
        int actionInd = -1;

        void RemoveBody(Celestial_Body body)
        {
            body.Dispose();
            SharedData.bodies.Remove(body);
            label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
        }
        void ControllingThrottle()
        {
            if (SmallScale)
            {
                if (throttling)
                {
                    focus.Throttle += 0.5;
                    if(focus.Landed)
                    {
                        focus.Landed = false;
                        focus.LifitngOff = true;
                    }
                }
                else if (dethrottling)
                {
                    focus.Throttle -= 0.5;
                }

                if (focus.Throttle > 100)
                    focus.Throttle = 100;

                if (focus.Throttle < 0)
                    focus.Throttle = 0;
            }
            else focus.Throttle = 0;
        }

        void ShowInformation()
        {
            if (shouldSetName)
            {
                textBox7.Text = focus.Name;
                textBox8.Text = focus.Name;
                shouldSetName = false;
            }
            textBox1.Text = "m = " + focus.Mass.ToString("E2") + " kg";
            if (focus.DominantBody != null)
                textBox3.Text = "v = " + SharedData.SizeScale(!(focus.Velocity - focus.DominantBody.Velocity)) + "/s";
            else
                textBox3.Text = "v = " + SharedData.SizeScale(!focus.Velocity) + "/s";

            textBox2.Text = "r = " + SharedData.SizeScale(focus.Radius);
            textBox6.Text = "X: " + SharedData.SizeScale(focus.Position.X) + ", Y: " + SharedData.SizeScale(focus.Position.Y);
            textBox4.Text = "e = " + (!focus.E).ToString("N2");

            if (focus.DominantBody != null && !(focus.DominantBody.Position - focus.Position) < focus.DominantBody.Radius * 100)
                textBox5.Text = "Ap: " + SharedData.SizeScale((focus.EScal < 1.0) ? focus.ApoapsisHeight : 0) + " ( " + SharedData.SizeScale((focus.EScal < 1.0) ? focus.ApoapsisHeight - focus.DominantBody.Radius : 0) + " )";
            else if (focus.DominantBody != null)
                textBox5.Text = "Ap: " + SharedData.SizeScale((focus.EScal < 1.0) ? focus.ApoapsisHeight : 0);
            else
                textBox5.Text = "Ap: " + SharedData.SizeScale(0);

            if (focus.DominantBody != null && !(focus.DominantBody.Position - focus.Position) < focus.DominantBody.Radius * 100)
                textBox14.Text = "Pe: " + SharedData.SizeScale(focus.PeriapsisHeight) + " ( " + SharedData.SizeScale(focus.PeriapsisHeight - focus.DominantBody.Radius) + " )";
            else if (focus.DominantBody != null)
                textBox14.Text = "Pe: " + SharedData.SizeScale(focus.PeriapsisHeight);
            else
                textBox14.Text = "Pe: " + SharedData.SizeScale(0);

            textBox9.Text = "T( Pe ): " + SharedData.TimeScale(focus.TimeToPeriapsis);
            textBox10.Text = "T( Ap ): " + SharedData.TimeScale(focus.TimeToApoapsis);

            if (focus.DominantBody != null && !(focus.DominantBody.Position - focus.Position) < focus.DominantBody.Radius * 100)
                textBox11.Text = "Dist: " + SharedData.SizeScale(!(focus.DominantBody.Position - focus.Position)) + " ( " + SharedData.SizeScale(!(focus.DominantBody.Position - focus.Position) - focus.DominantBody.Radius);
            else if (focus.DominantBody != null)
                textBox11.Text = "Dist: " + SharedData.SizeScale(!(focus.DominantBody.Position - focus.Position));
            else textBox11.Text = "Dist: " + SharedData.SizeScale(0);
        }

        double CalcOrbitalVelocity(double distance, double mass)
        {
            return Math.Sqrt(SharedData.G * (mass / distance));
        }

        void PutInFocus(Celestial_Body focusBody)
        {
            focus = focusBody;
            shouldSetName = true;
            if (focus.GetType() == typeof(Spaceship))
            {
                Controling = true;
                groupBox3.Visible = true;
                focused = true;
                if (focus.Prograde) customRadioButton1.Checked = true;
                else if (focus.Retrograde) customRadioButton2.Checked = true;
                else if (focus.Radial) customRadioButton3.Checked = true;
                else if (focus.Antiradial) customRadioButton4.Checked = true;
                else if (focus.Free) customRadioButton5.Checked = true;
            }
            else
            {
                Controling = false;
                groupBox3.Visible = false;
                focused = true;
            }
            if (placeInOrbit && double.TryParse(textBox12.Text, out double result) && result > 0)
            {
                if (defaultUnit < 3)
                    spawnDistance = result * Math.Pow(1000, defaultUnit) + focus.Radius;
                else
                {
                    if (defaultUnit == 3) spawnDistance = result * SharedData.AU + focus.Radius;
                    else if (defaultUnit == 4) spawnDistance = result * SharedData.LightYear + focus.Radius;
                }
            }
        }
        int CheckIfClicked(Vector MouseClick)
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                double screenX = SharedData.PutInScreenPosScaleX(SharedData.bodies[i].Position.X);
                double screenY = SharedData.PutInScreenPosScaleY(SharedData.bodies[i].Position.Y);
                double dist = !(new Vector(screenX - MouseClick.X, screenY - MouseClick.Y));
                if (SharedData.PutInScreenScale(SharedData.bodies[i].Radius) <= 25)
                {
                    if (dist < 25)
                    {
                        return i;
                    }
                }
                else
                {
                    if (dist < SharedData.PutInScreenScale(SharedData.bodies[i].Radius))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        void KeepLanded(Celestial_Body body)
        {
            body.Velocity = body.LandedBody.Velocity;
            Vector angPos = ~(body.Position - body.LandedBody.Position);
            body.Position = body.LandedBody.Position + ~(body.Position - body.LandedBody.Position) % body.LandedBody.Radius;
            body.DirAngleSS = (float)(Math.Atan2(angPos.X, -angPos.Y) * (180 / Math.PI));
            body.Throttle = 0;
            body.Free = true;
            if (focus == body && !customRadioButton5.Checked) customRadioButton5.Checked = true;
        }

        void EAT()
        {
            for (int i = 0; i < SharedData.bodies.Count - 1; i++)
            {
                for (int j = i + 1; j < SharedData.bodies.Count; j++)
                {
                    double dxEat = SharedData.bodies[i].Position.X - SharedData.bodies[j].Position.X;
                    double dyEat = SharedData.bodies[i].Position.Y - SharedData.bodies[j].Position.Y;
                    double maxR = SharedData.bodies[i].Radius + SharedData.bodies[j].Radius;
                    if (Math.Abs(dxEat) > maxR || Math.Abs(dyEat) > maxR) continue;
                    double distSqr = dxEat * dxEat + dyEat * dyEat;

                    if (SharedData.bodies[i] is Spaceship ^ SharedData.bodies[j] is Spaceship)
                    {
                        if (SharedData.bodies[i] is Spaceship si && si.LifitngOff)
                        {
                            if(distSqr > (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius) * (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius)) si.LifitngOff = false;      
                        }
                        else if (SharedData.bodies[j] is Spaceship sj && sj.LifitngOff)
                        {
                            if (distSqr > (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius) * (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius)) sj.LifitngOff = false;
                        }
                    }

                    if (distSqr <= (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius) * (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius))
                    {
                        if (SharedData.bodies[i] is Spaceship ^ SharedData.bodies[j] is Spaceship)
                        {
                            if (SharedData.bodies[i] is Spaceship si && !si.LifitngOff)
                            {
                                si.Landed = true;
                                si.LandedBody = SharedData.bodies[j];
                            }
                            else if (SharedData.bodies[j] is Spaceship sj && !sj.LifitngOff)
                            {
                                sj.Landed = true;
                                sj.LandedBody = SharedData.bodies[i];
                            }
                            continue;
                        }
                        if (SharedData.bodies[i].Mass > SharedData.bodies[j].Mass)
                        {
                            SharedData.bodies[i].Mass += SharedData.bodies[j].Mass;
                            SharedData.bodies[i].CalcRadius();
                            if (SharedData.bodies[i].GetType() == typeof(Star))
                            {
                                SharedData.bodies[i].DecideColor(SharedData.SolarMass);
                            }
                            if (SharedData.bodies[j] == focus)
                            {
                                PutInFocus(SharedData.bodies[i]);
                                focused = true;
                            }
                            SharedData.bodies[j].HasIntersection = false;
                            SharedData.bodies[i].HasIntersection = false;
                            if (SharedData.bodies[j] is Spaceship s)
                            {
                                customRadioButton5.Checked = true;
                            }
                            RemoveBody(SharedData.bodies[j]);
                            i--;
                            break;
                        }
                        else
                        {
                            SharedData.bodies[j].Mass += SharedData.bodies[i].Mass;
                            SharedData.bodies[j].CalcRadius();
                            if (SharedData.bodies[j].GetType() == typeof(Star))
                            {
                                SharedData.bodies[j].DecideColor(SharedData.SolarMass);
                            }
                            if (SharedData.bodies[i] == focus)
                            {
                                PutInFocus(SharedData.bodies[j]);
                                focused = true;
                            }
                            SharedData.bodies[j].HasIntersection = false;
                            SharedData.bodies[i].HasIntersection = false;
                            if (SharedData.bodies[i] is Spaceship s)
                            {
                                customRadioButton5.Checked = true;
                            }
                            RemoveBody(SharedData.bodies[i]);
                            i--;
                            break;
                        }

                    }
                }
            }
        }

        double CalcGForce(double mass1, double mass2, double distSq)
        {
            return (SharedData.G * mass1 * mass2) / distSq;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SharedData.SW = ClientRectangle.Width;
            SharedData.SH = ClientRectangle.Height;
            timer1.Start();
            dt = (timer1.Interval / 1000.0) * timeScale;
            TimeScaleLabel();
            newBody = new Planet();

            double w1 = 1.0 / (2.0 - cbrt2);
            double w0 = -cbrt2 * w1;
            yoshidaC = [w1 / 2.0, (w0 + w1) / 2.0, (w0 + w1) / 2.0, w1 / 2.0];
            yoshidaD = [w1, w0, w1];

            groupBox1.Location = new Point(30, SharedData.SH - 40 - groupBox1.Height);

            groupBox2.Location = new Point(SharedData.SW / 2 - groupBox2.Size.Width / 2, SharedData.SH / 2 - groupBox2.Size.Height / 2);

            groupBox3.Location = new Point(SharedData.SW / 2 - groupBox2.Size.Width / 2, SharedData.SH - 40 - groupBox2.Size.Height / 2);

            groupBox4.Location = new Point(SharedData.SW / 2 + groupBox2.Size.Width / 2 + 10, SharedData.SH / 2 - groupBox2.Size.Height / 2);
            groupBox5.Location = new Point(SharedData.SW / 2 + groupBox2.Size.Width / 2 + 10, SharedData.SH / 2 - groupBox2.Size.Height / 2);
            groupBox11.Location = new Point(SharedData.SW / 2 + groupBox2.Size.Width / 2 + 10, SharedData.SH / 2 - groupBox2.Size.Height / 2);
            groupBox6.Location = new Point(SharedData.SW / 2 + groupBox2.Size.Width / 2 + 10, SharedData.SH / 2 - groupBox2.Size.Height / 2);
            groupBox7.Location = new Point(SharedData.SW / 2 + groupBox2.Size.Width / 2 + 10, SharedData.SH / 2 - groupBox2.Size.Height / 2);

            customRadioButton1.Symbol = 0;
            customRadioButton2.Symbol = 1;
            customRadioButton3.Symbol = 2; 
            customRadioButton4.Symbol = 3;
            customRadioButton5.Symbol = 4;

            /////////////////////////
            ///System Presets
            comboBox1.Items.Add("Complete Solar System");
            comboBox1.Items.Add("Simple Solar System");
            comboBox1.Items.Add("Earth System");
            comboBox1.Items.Add("Mars System");
            comboBox1.Items.Add("Jupiter System");
            comboBox1.Items.Add("Saturn System");
            comboBox1.Items.Add("Uranus System");
            comboBox1.Items.Add("Neptune System");
            comboBox1.Items.Add("Pluto System");

            /////////////////////////
            ///Body Presets
            comboBox2.Items.Add("Sun");
            comboBox2.Items.Add("Mercury");
            comboBox2.Items.Add("Venus");
            comboBox2.Items.Add("Earth");
            comboBox2.Items.Add("Moon");
            comboBox2.Items.Add("Mars");
            comboBox2.Items.Add("Jupiter");
            comboBox2.Items.Add("Saturn");
            comboBox2.Items.Add("Uranus");
            comboBox2.Items.Add("Neptune");
            comboBox2.Items.Add("Pluto");
            comboBox2.Items.Add("Sgr A* (BH)");
            comboBox2.Items.Add("Spaceship");
            comboBox2.Items.Add("Viltrum");

            comboBox3.Items.Add("Sun");
            comboBox3.Items.Add("Mercury");
            comboBox3.Items.Add("Venus");
            comboBox3.Items.Add("Earth");
            comboBox3.Items.Add("Moon");
            comboBox3.Items.Add("Mars");
            comboBox3.Items.Add("Jupiter");
            comboBox3.Items.Add("Saturn");
            comboBox3.Items.Add("Uranus");
            comboBox3.Items.Add("Neptune");
            comboBox3.Items.Add("Pluto");
            comboBox3.Items.Add("Sgr A* (BH)");
            comboBox3.Items.Add("Spaceship");
            comboBox3.Items.Add("Viltrum");

            comboBox4.Items.Add("m");
            comboBox4.Items.Add("km");
            comboBox4.Items.Add("Mm");
            comboBox4.Items.Add("AU");
            comboBox4.Items.Add("LY");
            comboBox4.SelectedIndex = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Celestial_Body b in SharedData.bodies) b.Dispose();
            SharedData.bodies.Clear();
            SharedData.Offset = new Vector();
            focus = null;
            focused = false;
            placeInOrbit = false;
            checkBox1.Checked = false;
            nextIntersectionUpdate = 0;
            SharedData.totalElapsedTime = 63338889600.0;
            SharedData.Scale = SharedData.AU / 300;
            Presets.mainBelt.Clear();
            Presets.kuiperBelt.Clear();
            trackBar1.Value = 1;
            trackBarValue = 1;
            SharedData.FocusPosition = new Vector();
            if (comboBox1.SelectedIndex == 0)
            {
                Presets.SpawnCompleteSolarSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                Presets.SpawnSolarSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 2.0f)
            {
                Presets.SpawnEarthSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 3)
            {
                Presets.SpawnMarsSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 4)
            {
                Presets.SpawnJupiterSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 5)
            {
                Presets.SpawnSaturnSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 6)
            {
                Presets.SpawnUranusSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 7)
            {
                Presets.SpawnNeptuneSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
            else if (comboBox1.SelectedIndex == 8)
            {
                Presets.SpawnPlutoSystem();
                PutInFocus(SharedData.bodies[0]);
                focused = true;
                label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
            }
        }

        double SOI(Celestial_Body body, Celestial_Body parent)
        {
            if (body == null || parent == null) return 0;
            double dist = !(body.Position - parent.Position);
            if (dist == 0) return 0;
            return dist * Math.Pow(body.Mass / parent.Mass, 2.0 / 5.0);
        }
        double precisionFactor = 20.0;
        double dynamicFactor = 20.0;
        double nextIntersectionUpdate = 0;
        double nextIdT = 0;
        Celestial_Body newBody = null;
        Celestial_Body oldDom = null;
        double cbrt2 = Math.Pow(2.0, 1.0 / 3.0);
        double[] yoshidaC;
        double[] yoshidaD;

        Vector ComputeAcc(double mass, double distance, Vector position)
        {
            return position % ((SharedData.G * mass) / (distance * distance * distance));
        }

        void ComputeAllAcc()
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                SharedData.bodies[i].Acceleration = new Vector();
            }

            if (SharedData.bodies.Count > 1)
            {
                for (int i = 0; i < SharedData.bodies.Count - 1; i++)
                {
                    for (int j = i + 1; j < SharedData.bodies.Count; j++)
                    {
                        if (SharedData.bodies[i].Landed || SharedData.bodies[j].Landed)
                        {
                            if (SharedData.bodies[i].Landed)
                            {
                                KeepLanded(SharedData.bodies[i]);
                            }
                            if(SharedData.bodies[j].Landed)
                            {
                                KeepLanded(SharedData.bodies[j]);             
                            }
                            continue;
                        }
                        Vector r = SharedData.bodies[j].Position - SharedData.bodies[i].Position;
                        double distSq = r.SquaredMagnitude();
                        if (distSq < (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius) * (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius))
                            distSq = (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius) * (SharedData.bodies[i].Radius + SharedData.bodies[j].Radius);
                        double dist = Math.Sqrt(distSq);
                        SharedData.bodies[i].Acceleration += ComputeAcc(SharedData.bodies[j].Mass, dist, r);
                        SharedData.bodies[j].Acceleration -= ComputeAcc(SharedData.bodies[i].Mass, dist, r);
                    }
                }
            }
        }

        void UpdateFactor(double elapsedMs, int substepCount)
        {
            double targetMs = 16.0;
            double ratio = elapsedMs / targetMs;

            if (ratio > 2.0)
                dynamicFactor = Math.Min(dynamicFactor * ratio, precisionFactor * 100);
            else if (ratio > 1.0)
                dynamicFactor = Math.Min(dynamicFactor * (1.0 + (ratio - 1.0) * 0.5), precisionFactor * 100);
            else if (ratio < 0.3 && dynamicFactor > precisionFactor)
            {
                dynamicFactor *= Math.Max(0.5, ratio + 0.3);
                if (dynamicFactor < precisionFactor) dynamicFactor = precisionFactor;
            }
            else if (ratio < 0.7 && dynamicFactor > precisionFactor)
            {
                dynamicFactor *= 0.95;
                if (dynamicFactor < precisionFactor) dynamicFactor = precisionFactor;
            }

            double lowSubsteps = precisionFactor * 0.1;
            double highSubsteps = precisionFactor * 20.0;

            if (substepCount < lowSubsteps && dynamicFactor > precisionFactor)
            {
                dynamicFactor *= 0.9;
                if (dynamicFactor < precisionFactor) dynamicFactor = precisionFactor;
            }
            if (substepCount > highSubsteps)
                dynamicFactor = Math.Min(dynamicFactor * 1.5, precisionFactor * 100);
        }

        double DecideSubDT(double targetDt, double totalTimeThisFrame)
        {
            double maxAccMag = 0;
            foreach (var b in SharedData.bodies)
            {
                double mag = !b.Acceleration;
                if (mag > maxAccMag) maxAccMag = mag;
            }

            double subDt = (maxAccMag > 0) ? dynamicFactor / Math.Sqrt(maxAccMag) : targetDt;
            if (subDt > (targetDt - totalTimeThisFrame)) subDt = targetDt - totalTimeThisFrame;
            if (subDt < 0.000001) subDt = 0.000001;
            return subDt;
        }

        void UpdateTrail()
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                SharedData.bodies[i].Trail[SharedData.bodies[i].TrailHead] = SharedData.bodies[i].Position;
                SharedData.bodies[i].TrailHead = (SharedData.bodies[i].TrailHead + 1) % 200;
                if (SharedData.bodies[i].TrailCount < 200) SharedData.bodies[i].TrailCount++;
                SharedData.bodies[i].TrailDirty = true;
            }
        }

        void Yoshida(double subDt)
        {
            for (int stage = 0; stage < 3; stage++)
            {
                double c = yoshidaC[stage];
                double d = yoshidaD[stage];

                for (int i = 0; i < SharedData.bodies.Count; i++)
                    SharedData.bodies[i].Position += SharedData.bodies[i].Velocity % (c * subDt);

                ComputeAllAcc();

                for (int i = 0; i < SharedData.bodies.Count; i++)
                {
                    SharedData.bodies[i].Velocity += SharedData.bodies[i].Acceleration % (d * subDt);

                    if (SharedData.bodies[i] is Spaceship ship && ship.Throttle > 0)
                    {
                        double thrustAcc = ship.Thrust/3.0 * ship.InvMass * (ship.Throttle / 100.0);
                        double angleRad = (ship.DirAngleSS - 90) * Math.PI / 180.0;
                        SharedData.bodies[i].Velocity.X += Math.Cos(angleRad) * thrustAcc * (d * subDt);
                        SharedData.bodies[i].Velocity.Y += Math.Sin(angleRad) * thrustAcc * (d * subDt);
                        if(SharedData.bodies[i].Velocity.X <= 0 && SharedData.bodies[i].Velocity.Y <= 0)
                        {
                            if (focus == SharedData.bodies[i] && !customRadioButton5.Checked) customRadioButton5.Checked = true;
                        }
                    }
                }
            }
            for (int i = 0; i < SharedData.bodies.Count; i++)
                SharedData.bodies[i].Position += SharedData.bodies[i].Velocity % (yoshidaC[3] * subDt);
        }

        void DecideDominantBody(Celestial_Body body)
        {
            oldDom = body.DominantBody;
            body.DominantBody = null;
            double minSOI = double.MaxValue;
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                if (body == SharedData.bodies[i]) continue;
                double rSOI = 0;
                if (SharedData.bodies[i].DominantBody != null) rSOI = SOI(SharedData.bodies[i], SharedData.bodies[i].DominantBody);
                if (rSOI <= 0) continue;
                double dist = !(body.Position - SharedData.bodies[i].Position);
                if (dist < rSOI && rSOI < minSOI && SharedData.bodies[i].Mass > body.Mass)
                {
                    minSOI = rSOI;
                    body.DominantBody = SharedData.bodies[i];
                    if (oldDom != body.DominantBody) body.HasIntersection = false;
                }
            }
            if (body.DominantBody == null)
            {
                double maxGForce = 0;
                for (int i = 0; i < SharedData.bodies.Count; i++)
                {
                    if (body == SharedData.bodies[i]) continue;
                    Vector r = SharedData.bodies[i].Position - body.Position;
                    double distSq = r.SquaredMagnitude();
                    double gForce = CalcGForce(body.Mass, SharedData.bodies[i].Mass, distSq);
                    if (gForce > maxGForce && SharedData.bodies[i].Mass > body.Mass)
                    {
                        maxGForce = gForce;
                        body.DominantBody = SharedData.bodies[i];
                        if (oldDom != body.DominantBody) body.HasIntersection = false;
                    }
                }
            }
        }

        void IntersectionPrediction()
        {
            if (SharedData.PredictIntersections && SharedData.totalElapsedTime > nextIntersectionUpdate)
            {
                nextIntersectionUpdate = SharedData.totalElapsedTime + nextIdT;
                for (int i = 0; i < SharedData.bodies.Count; i++)
                {
                    for (int j = 0; j < SharedData.bodies.Count; j++)
                    {
                        if (!(SharedData.bodies[i] == SharedData.bodies[j] || SharedData.bodies[i].DominantBody == SharedData.bodies[j] || SharedData.bodies[j].DominantBody == SharedData.bodies[i] || SharedData.bodies[j].DominantBody == null || SharedData.bodies[i].DominantBody == null || SharedData.bodies[i].DominantBody != SharedData.bodies[j].DominantBody))
                        {
                            if (SharedData.bodies[j].IsAncestorOf(SharedData.bodies[i].DominantBody)) continue;
                            if (!SharedData.bodies[i].CanPossiblyIntersect(SharedData.bodies[j], SOI(SharedData.bodies[j], SharedData.bodies[j].DominantBody))) continue;
                            SharedData.bodies[i].GetIntersectionPosition(SharedData.bodies[j], SOI(SharedData.bodies[j], SharedData.bodies[j].DominantBody));
                        }
                    }
                }
                foreach (var body in SharedData.bodies)
                {
                    if (body.HasIntersection && body.TimeToIntersection < 3600 * 24 * 30)
                        nextIntersectionUpdate = SharedData.totalElapsedTime;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label29.Text = "Simulation Time: " + SharedData.SecondsToDate(SharedData.totalElapsedTime);

            if (focus != null && Controling) ControllingThrottle();

            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (dt == 0) { Invalidate(); return; }

            int substepCount = 0;
            double totalTimeThisFrame = 0;
            double targetDt = (timer1.Interval / 1000.0) * timeScale;
            while (totalTimeThisFrame < targetDt)
            {
                substepCount++;

                double subDt = DecideSubDT(targetDt, totalTimeThisFrame);

                Yoshida(subDt);

                UpdateTrail();

                totalTimeThisFrame += subDt;
            }

            watch.Stop();
            SharedData.totalElapsedTime += totalTimeThisFrame;

            UpdateFactor(watch.ElapsedMilliseconds, substepCount);

            if (SharedData.DrawOrbits && SharedData.bodies.Count > 1)
            {
                for (int i = 0; i < SharedData.bodies.Count; i++)
                {
                    if (SharedData.totalElapsedTime > nextIntersectionUpdate)
                    {
                        DecideDominantBody(SharedData.bodies[i]);
                    }

                    if (SharedData.bodies[i].DominantBody != null)
                    {
                        SharedData.bodies[i].CalculateOrbit();
                        SharedData.bodies[i].OrbitalDirty = true;
                        SharedData.bodies[i].HyperbolaDirty = true;
                    }
                }
            }

            IntersectionPrediction();

            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                if (SharedData.bodies[i] is Spaceship ship && !ship.Free && ship.DominantBody != null)
                {
                    Vector relativeVel = ship.Velocity - ship.DominantBody.Velocity;
                    Vector relativePos = ship.Position - ship.DominantBody.Position;

                    ship.RotateShip(relativePos, relativeVel);
                }
            }
            if (focused)
            {
                SharedData.FocusPosition.X = focus.Position.X;
                SharedData.FocusPosition.Y = focus.Position.Y;
                SharedData.Offset.X = 0;
                SharedData.Offset.Y = 0;

                if (!groupBox1.Visible) groupBox1.Visible = true;
                ShowInformation();
            }
            else groupBox1.Visible = false;

            EAT();
            Invalidate();
        }

        bool IsOnScreen(float x, float y)
        {
            if (x < 0) return false;
            if (x > SharedData.SW) return false;
            if (y < 0) return false;
            if (y > SharedData.SH) return false;
            return true;

        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
        Celestial_Body preview;
        SolidBrush SOIBrush = new SolidBrush(Color.FromArgb(25, Color.White));


        Pen[] gridPens =
            [new Pen(Color.FromArgb(30, Color.White), 2f),
            new Pen(Color.FromArgb(20, Color.White), 1f),
            new Pen(Color.FromArgb(15, Color.White), 1f),
            new Pen(Color.FromArgb(10, Color.White), 1f)];

        SolidBrush gridNumBrush = new SolidBrush(Color.FromArgb(30, Color.White));

        void DrawGrid(Graphics g)
        {
            double cx = SharedData.PutInScreenPosScaleXDouble(0); 
            double cy = SharedData.PutInScreenPosScaleYDouble(0);

            g.DrawLine(gridPens[0], 0, SharedData.ClampFloat((float)cy), SharedData.SW, SharedData.ClampFloat((float)cy));
            g.DrawLine(gridPens[0], SharedData.ClampFloat((float)cx), 0, SharedData.ClampFloat((float)cx), SharedData.SH);

            double worldSpacing = SharedData.AU;
            while (worldSpacing / SharedData.Scale > 100) worldSpacing /= 5.0;
            while (worldSpacing / SharedData.Scale < 100) worldSpacing *= 5.0;
            double spacing = worldSpacing / SharedData.Scale;

            DrawGrid(g, cx, cy, spacing, 1);
        }

        void DrawGrid(Graphics g, double cx, double cy, double spacing, int depth)
        {
            if (depth > gridPens.Length - 1) return;
            if (spacing < 30) return;

            Pen pen = gridPens[depth];

            double offX = cx % spacing;
            double offY = cy % spacing;
            if (offX < 0) offX += spacing;
            if (offY < 0) offY += spacing;

            for (double x = offX; x < SharedData.SW; x += spacing)
            {
                g.DrawLine(pen, (float)x, 0, (float)x, SharedData.SH);
                if (depth == 1)
                {
                    double worldX = SharedData.PutInWorldPosScaleX(x);
                    string cord = SharedData.SizeScale(worldX);
                    Size cordS = TextRenderer.MeasureText(cord, DefaultFont);
                    g.DrawString(SharedData.SizeScale(worldX), DefaultFont, gridNumBrush, (float)x + 2, (float)cy + 2);
                    g.DrawString(SharedData.SizeScale(worldX), DefaultFont, gridNumBrush, (float)x + 2, 2);
                    g.DrawString(SharedData.SizeScale(worldX), DefaultFont, gridNumBrush, (float)x + 2, SharedData.SH - cordS.Height-2);
                }
            }

            for (double y = offY; y < SharedData.SH; y += spacing)
            {
                g.DrawLine(pen, 0, (float)y, SharedData.SW, (float)y);
                if (depth == 1)
                {
                    double worldY = SharedData.PutInWorldPosScaleY(y);
                    string cord = SharedData.SizeScale(worldY);
                    Size cordS = TextRenderer.MeasureText(cord, DefaultFont);
                    g.DrawString(SharedData.SizeScale(worldY), DefaultFont, gridNumBrush, (float)cx + 2, (float)y + 2);
                    g.DrawString(SharedData.SizeScale(worldY), DefaultFont, gridNumBrush, 2, (float)y + 2);
                    g.DrawString(SharedData.SizeScale(worldY), DefaultFont, gridNumBrush, SharedData.SW -cordS.Width-2, (float)y + 2);
                }
            }

            DrawGrid(g, cx, cy, spacing / 2.0, depth + 1);
        }

        void DrawScaleLine(Graphics g)
        {
            g.DrawLine(pen, SharedData.SW - 30, SharedData.SH - 50, SharedData.SW - 330, SharedData.SH - 50);
            g.DrawLine(pen, SharedData.SW - 30, SharedData.SH - 60, SharedData.SW - 30, SharedData.SH - 40);
            g.DrawLine(pen, SharedData.SW - 330, SharedData.SH - 60, SharedData.SW - 330, SharedData.SH - 40);
            string sizeScale = SharedData.SizeScale();
            Size textWidth = TextRenderer.MeasureText(sizeScale, DefaultFont);
            g.DrawString(SharedData.SizeScale(), DefaultFont, brush, SharedData.SW - 330 - textWidth.Width / 2.0f, SharedData.SH - 80);
        }
        string startSpeed = "0";
        void DrawPreviewOrbit(Graphics g)
        {
            if (isMakingVelocity && validMass && focused)
            {
                double worldX = SharedData.PutInWorldPosScaleX(MoveStart.X);
                double worldY = SharedData.PutInWorldPosScaleY(MoveStart.Y);
                preview = SharedData.CreateBody(worldX, worldY, bodyMass, StartingVelocity, Color.FromArgb(120, Color.White));
                preview.DominantBody = focus;
                preview.OrbitalDirty = true;
                preview.HyperbolaDirty = true;
                preview.CalculateOrbit();
                preview.OrbitalDirty = true;
                preview.HyperbolaDirty = true;
                preview.DrawOrbit(g, SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH);
                preview.Draw(g, SharedData.Offset, SharedData.SW, SharedData.SH);
                g.DrawLine(pen, MoveStart, currLocation);
                g.DrawString(startSpeed + " m/s", DefaultFont, brush, MoveStart);

                foreach (Celestial_Body target in SharedData.bodies)
                {
                    if (target == preview || target == focus || target.DominantBody == null) continue;
                    double rSOI = SOI(target, target.DominantBody);
                    if (rSOI <= 0) continue;
                    preview.GetIntersectionPosition(target, rSOI);
                    if (preview.HasIntersection)
                    {
                        float ix = SharedData.PutInScreenPosScaleXClamp(preview.Intersection.X);
                        float iy = SharedData.PutInScreenPosScaleYClamp(preview.Intersection.Y);
                        g.DrawEllipse(Pens.White, ix - 6, iy - 6, 12, 12);
                        g.DrawString("Intersection", DefaultFont, Brushes.White, ix + 10, iy - 10);
                    }
                }
            }
        }
        void DrawFocusSOI(Graphics g)
        {
            if (focus != null && focus.DominantBody != null)
            {
                float rSOI = (float)SOI(focus, focus.DominantBody);
                focus.DrawSOI(g, SharedData.SW, SharedData.SH, rSOI, SOIBrush);
                if (focus.DominantBody.DominantBody != null)
                {
                    rSOI = (float)SOI(focus.DominantBody, focus.DominantBody.DominantBody);
                    focus.DominantBody.DrawSOI(g, SharedData.SW, SharedData.SH, rSOI, SOIBrush);
                }
            }
        }
        void DrawMouseDistanceFromFocus(Graphics g)
        {
            if (focused && focus != null)
            {
                Vector distance = new Vector(currLocation.X, currLocation.Y) - new Vector(SharedData.SW / 2.0, SharedData.SH / 2.0);
                double dist = !distance;
                if(dist < SharedData.PutInScreenScale(focus.Radius * 100) && dist > SharedData.PutInScreenScale(focus.Radius))
                    g.DrawString(SharedData.SizeScale(SharedData.PutInWorldScale(dist)) + " ( " + SharedData.SizeScale(SharedData.PutInWorldScale(dist) - focus.Radius) + " )", DefaultFont, brush, currLocation.X + 15, currLocation.Y - 5);
                else
                    g.DrawString(SharedData.SizeScale(SharedData.PutInWorldScale(dist)), DefaultFont, brush, currLocation.X + 15, currLocation.Y - 5);
            }
        }

        void DrawMouseCords(Graphics g)
        {
            if(!focused)
            {
                g.DrawString(SharedData.SizeScale(SharedData.PutInWorldPosScaleX(currLocation.X)) + ", " + SharedData.SizeScale(SharedData.PutInWorldPosScaleY(currLocation.Y)), DefaultFont, brush, currLocation.X + 15, currLocation.Y - 5);
            }
        }
        void DrawBodies(Graphics g)
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                if (SharedData.bodies[i].IsOnScreen(SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH))
                    SharedData.bodies[i].Draw(g, SharedData.Offset, SharedData.SW, SharedData.SH);

                int count = SharedData.bodies[i].TrailCount;
                for (int j = 0; j < count; j++)
                {
                    int idx = (SharedData.bodies[i].TrailHead - count + j + 200) % 200;
                    SharedData.bodies[i].TrailPoints[j] = new PointF(SharedData.PutInScreenPosScaleXClamp(SharedData.bodies[i].Trail[idx].X), SharedData.PutInScreenPosScaleYClamp(SharedData.bodies[i].Trail[idx].Y));
                }
                if (count > 1 && !SharedData.bodies[i].Landed)
                {
                    SharedData.bodies[i].UpdateTrailPath(SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH);
                    g.DrawPath(SharedData.bodies[i].TrailPen, SharedData.bodies[i].TrailPath);
                }
                if (SharedData.bodies.Count > 1 && SharedData.DrawOrbits && !SharedData.bodies[i].Landed)
                {
                    SharedData.bodies[i].DrawOrbit(g, SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH);
                    SharedData.bodies[i].DrawPostSOIOrbit(g, SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH);
                }
            }
        }
        void DrawThrottleBar(Graphics g)
        {
            if (Controling && focused)
            {
                brush.Color = Color.DimGray;
                g.FillRectangle(brush, SharedData.SW - 72, SharedData.SH / 2.0f - 352, 44, 704);
                brush.Color = Color.LightGray;
                g.FillRectangle(brush, SharedData.SW - 68, SharedData.SH / 2.0f - 348, 35, 695);
                pen.Color = Color.Black;
                g.DrawRectangle(pen, SharedData.SW - 68, SharedData.SH / 2.0f - 348, 35, 695);
                brush.Color = Color.Red;
                g.FillRectangle(brush, SharedData.SW - 66, SharedData.SH / 2.0f + 346 - (float)focus.Throttle * 6.92f, 32, (float)focus.Throttle * 6.92f);
                pen.Color = Color.White;
                brush.Color = Color.White;
                g.DrawString(focus.Throttle.ToString("#0") + "%", DefaultFont, brush, SharedData.SW - 100, SharedData.SH / 2.0f + 340 - (float)focus.Throttle * 6.92f);
            }
        }

        void DrawIntersections(Graphics g)
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                if (!(SharedData.bodies[i] == null || SharedData.bodies[i].DominantBody == null || !SharedData.bodies[i].HasIntersection))
                {
                    float screenX = SharedData.PutInScreenPosScaleXClamp(SharedData.bodies[i].Intersection.X);
                    float screenY = SharedData.PutInScreenPosScaleYClamp(SharedData.bodies[i].Intersection.Y);

                    g.FillEllipse(brush, screenX - 10, screenY - 10, 20, 20);
                    g.DrawString(SharedData.TimeScale(SharedData.bodies[i].TimeToIntersection), DefaultFont, brush, screenX + 30, screenY + 30);
                }
            }
        }
        Pen ghostPen = new Pen(Color.FromArgb(100, Color.Red), 2.0f);
        Celestial_Body ghost;
        Celestial_Body ghostTarget;
        void DrawPostIntersectionOrbit(Graphics g)
        {
            for (int i = 0; i < SharedData.bodies.Count; i++)
            {
                if (SharedData.bodies[i] == null || !SharedData.bodies[i].HasIntersection || SharedData.bodies[i].DominantBody == null) continue;

                if (SharedData.bodies[i].IntersectingBody == null) continue;

                double dt2 = SharedData.bodies[i].OrbitalPeriod * 1e-4;
                if (dt2 < 1e-6) dt2 = 1e-6;
                Vector p0 = SharedData.bodies[i].GetPositionAtTime(SharedData.bodies[i].TimeToIntersection - dt2);
                Vector p1 = SharedData.bodies[i].GetPositionAtTime(SharedData.bodies[i].TimeToIntersection + dt2);
                Vector velAtIntersect = (p1 - p0) % (1.0 / (2.0 * dt2));

                Vector posAtIntersect = SharedData.bodies[i].IntersectingBody.GetPositionAtTime(SharedData.bodies[i].TimeToIntersection);
                ghost = SharedData.CreateBody(SharedData.bodies[i].Intersection.X, SharedData.bodies[i].Intersection.Y, SharedData.bodies[i].Mass, velAtIntersect, Color.Red);

                dt2 = 0.01;
                p0 = SharedData.bodies[i].IntersectingBody.GetPositionAtTime(SharedData.bodies[i].TimeToIntersection - dt2);
                p1 = SharedData.bodies[i].IntersectingBody.GetPositionAtTime(SharedData.bodies[i].TimeToIntersection + dt2);
                velAtIntersect = (p1 - p0) % (1.0 / (2.0 * dt2));

                ghostTarget = SharedData.CreateBody(posAtIntersect.X, posAtIntersect.Y, SharedData.bodies[i].IntersectingBody.Mass, velAtIntersect, Color.FromArgb(150, Color.White), SharedData.bodies[i].IntersectingBody.Name, SharedData.bodies[i].IntersectingBody.IsSaturn, SharedData.bodies[i].IntersectingBody.IsViltrum, SharedData.bodies[i].IntersectingBody.IsUranus, SharedData.bodies[i].IntersectingBody.IsNeptune);

                ghost.DominantBody = ghostTarget;
                ghostTarget.DominantBody = SharedData.bodies[i].IntersectingBody.DominantBody;
                ghost.OrbitalDirty = true;
                ghost.HyperbolaDirty = true;
                ghost.CalculateOrbit();
                ghost.OrbitalDirty = true;
                ghost.HyperbolaDirty = true;

                ghost.OrbitPen = ghostPen;
                ghost.DrawOrbit(g, SharedData.Offset, SharedData.Scale, SharedData.SW, SharedData.SH);
                ghostTarget.Draw(g, SharedData.Offset, SharedData.SW, SharedData.SH);
                if (ghostTarget.DominantBody != null)
                    ghostTarget.DrawSOI(g, SharedData.SW, SharedData.SH, SOI(ghostTarget, ghostTarget.DominantBody), SOIBrush);

            }
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            g = e.Graphics;

            g.SetClip(ClientRectangle);

            if (SharedData.drawGrid)
                DrawGrid(g);

            Presets.DrawBelt(g, Presets.mainBelt, brush, SharedData.Offset, SharedData.SW, SharedData.SH);
            Presets.DrawBelt(g, Presets.kuiperBelt, brush, SharedData.Offset, SharedData.SW, SharedData.SH);

            DrawBodies(g);
            DrawFocusSOI(g);
            DrawPreviewOrbit(g);

            if (validMass)
            {
                float radius = SharedData.PutInScreenScaleClamp(SharedData.CalculateRadius(bodyMass));
                g.FillEllipse(SOIBrush, currLocation.X - radius, currLocation.Y - radius, radius * 2.0f, radius * 2.0f);
            }
            DrawScaleLine(g);
            DrawMouseDistanceFromFocus(g);
            DrawMouseCords(g);
            DrawThrottleBar(g);
            if (SharedData.PredictIntersections)
            {
                DrawIntersections(g);
                DrawPostIntersectionOrbit(g);
            }
            g.ResetClip();
        }

        /////////////////////////////////////////////
        ///Drag And Drop, Starting Velocity

        bool isMoving = false;
        Point MoveStart = Point.Empty;

        Vector StartingVelocity = new Vector();
        bool isMakingVelocity = false;
        Point currLocation = new Point();
        bool justFocused = false;

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!paused)
            {
                if (e.Button == MouseButtons.Right)
                {
                    actionInd = CheckIfClicked(new Vector(e.X, e.Y));
                    if (actionInd == -1)
                    {
                        button11.Text = "FOCUS";
                        groupBox8.Visible = false;
                        shouldOpenSpawnMenu = true;
                    }
                    else
                    {
                        shouldOpenActionMenu = true;
                        shouldOpenSpawnMenu = false;
                    }

                    if (focused)
                    {
                        SharedData.Offset.X = -(SharedData.FocusPosition.X / SharedData.Scale);
                        SharedData.Offset.Y = -(SharedData.FocusPosition.Y / SharedData.Scale);
                        SharedData.FocusPosition.X = 0;
                        SharedData.FocusPosition.Y = 0;
                    }

                    isMoving = true;
                    MoveStart = e.Location;
                    isMakingVelocity = false;

                }
                else if (e.Button == MouseButtons.Middle)
                {
                    groupBox8.Visible = false;
                    groupBox9.Visible = false;
                    groupBox10.Visible = false;
                    int index = CheckIfClicked(new Vector(e.X, e.Y));
                    if (index >= 0)
                    {
                        if (SharedData.bodies[index] == focus && focused)
                        {
                            SharedData.Scale = focus.Radius * 2.0f / 300.0;
                        }
                        else
                        {
                            PutInFocus(SharedData.bodies[index]);
                        }
                        justFocused = true;
                        focused = true;
                    }
                }
                else if (e.Button == MouseButtons.Left)
                {
                    if (focus != null && focused)
                    {
                        StartingVelocity.X = focus.Velocity.X;
                        StartingVelocity.Y = focus.Velocity.Y;
                        isMakingVelocity = true;
                        MoveStart = e.Location;
                    }
                    else
                    {
                        StartingVelocity.X = 0;
                        StartingVelocity.Y = 0;
                        isMakingVelocity = true;
                        MoveStart = e.Location;
                    }
                }
            }
        }
        bool shouldOpenActionMenu = true;
        bool shouldOpenSpawnMenu = true;
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!paused)
            {
                if (isMoving)
                {
                    button11.Text = "FOCUS";
                    groupBox8.Visible = false;
                    groupBox9.Visible = false;
                    groupBox10.Visible = false;
                    actionInd = -1;
                    shouldOpenActionMenu = false;
                    double oldOffX = SharedData.Offset.X;
                    double oldOffY = SharedData.Offset.Y;
                    SharedData.Offset.X += e.X - MoveStart.X;
                    SharedData.Offset.Y += e.Y - MoveStart.Y;
                    MoveStart = e.Location;
                    if (SharedData.Offset.X != oldOffX || SharedData.Offset.Y != oldOffY)
                    {
                        focused = false;
                        placeInOrbit = false;
                        checkBox1.Checked = false;
                        Controling = false;
                        groupBox3.Visible = false;
                        shouldOpenSpawnMenu = false;
                    }
                    foreach (var b in SharedData.bodies)
                    {
                        b.TrailDirty = true;
                        b.HyperbolaDirty = true;
                    }
                }
                if (isMakingVelocity && validMass)
                {
                    Vector RelativeVelocity = new Vector();
                    double referenceVel = 10000;
                    if (focused)
                    {
                        double worldX = SharedData.PutInWorldPosScaleX(MoveStart.X);
                        double worldY = SharedData.PutInWorldPosScaleY(MoveStart.Y);
                        double placingDist = !(new Vector(worldX - focus.Position.X, worldY - focus.Position.Y));
                        if (placingDist > 0)
                            referenceVel = CalcOrbitalVelocity(placingDist, focus.Mass);
                    }
                    double velScale = referenceVel / 200.0;

                    if (focused)
                    {
                        StartingVelocity.X = focus.Velocity.X + (e.X - MoveStart.X) * velScale;
                        StartingVelocity.Y = focus.Velocity.Y + (e.Y - MoveStart.Y) * velScale;
                        RelativeVelocity = StartingVelocity - focus.Velocity;
                    }
                    else
                    {
                        StartingVelocity.X = (e.X - MoveStart.X) * velScale;
                        StartingVelocity.Y = (e.Y - MoveStart.Y) * velScale;
                        RelativeVelocity = StartingVelocity;
                    }

                    double relVel = !RelativeVelocity;
                    startSpeed = relVel.ToString("N2");
                }
            }
            currLocation = e.Location;
        }

        double bodyMass = 0;
        double spawnDistance = 0;
        bool validMass = false;
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!paused)
            {
                isMoving = false;
                if (isMakingVelocity && !justFocused && e.Button == MouseButtons.Left)
                {
                    if (validMass && !placeInOrbit)
                    {
                        newBody.Acceleration = new Vector();
                        Vector worldClicked = new Vector(SharedData.PutInWorldPosScaleX(MoveStart.X), SharedData.PutInWorldPosScaleY(MoveStart.Y));
                        newBody = ChooseBody(worldClicked, StartingVelocity, currInd);
                        if (newBody == null) newBody = SharedData.CreateBody(worldClicked.X, worldClicked.Y, bodyMass, StartingVelocity);
                        if (pendingName != "") newBody.Name = pendingName;
                        SharedData.bodies.Add(newBody);
                        if (focused)
                        {
                            Vector r = focus.Position - newBody.Position;
                            double distSq = r.SquaredMagnitude();
                            double dist = Math.Sqrt(distSq);
                            newBody.Acceleration = ComputeAcc(focus.Mass, dist, r);
                        }
                        dynamicFactor = precisionFactor;
                        foreach (var body in SharedData.bodies)
                            body.Initialized = false;
                        label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
                    }
                    if (validMass && placeInOrbit && focused)
                    {
                        Vector position = new Vector(focus.Position.X + spawnDistance, focus.Position.Y);
                        Vector distV = position - focus.Position;
                        Vector distVPerp = new Vector(-distV.Y, distV.X);
                        double dist = !(distV);
                        Vector velocity = focus.Velocity + (~(distVPerp) % CalcOrbitalVelocity(dist, focus.Mass + bodyMass));
                        newBody = ChooseBody(position, velocity, currInd);
                        if (newBody == null) newBody = SharedData.CreateBody(position.X, position.Y, bodyMass, velocity);
                        if (pendingName != "") newBody.Name = pendingName;
                        label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
                        newBody.DominantBody = focus;
                        SharedData.bodies.Add(newBody);
                        Vector r = focus.Position - newBody.Position;
                        double distSq = r.SquaredMagnitude();
                        dist = Math.Sqrt(distSq);
                        newBody.Acceleration = ComputeAcc(focus.Mass, dist, r);
                        dynamicFactor = precisionFactor;
                        foreach (var body in SharedData.bodies)
                            body.Initialized = false;
                    }
                }
                isMakingVelocity = false;
                justFocused = false;
            }
        }
        ////////////////////////////////////////////////
        ///Zoom
        void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (!paused)
            {
                if (SharedData.Scale > 0.05)
                {
                    double factor = 0;
                    if (e.Delta < 0)
                    {
                        factor = ZoomFactor;
                    }
                    else if (e.Delta > 0)
                    {
                        factor = 1.0 / ZoomFactor;
                    }

                    double mouseWorldX = SharedData.PutInWorldPosScaleX(e.X);
                    double mouseWorldY = SharedData.PutInWorldPosScaleY(e.Y);

                    SharedData.Scale *= factor;

                    if (!focused)
                    {
                        SharedData.Offset.X = e.X - SharedData.SW / 2.0 - SharedData.PutInScreenScale(mouseWorldX - SharedData.FocusPosition.X);
                        SharedData.Offset.Y = e.Y - SharedData.SH / 2.0 - SharedData.PutInScreenScale(mouseWorldY - SharedData.FocusPosition.Y);
                    }
                    else
                    {
                        SharedData.Offset.X = 0;
                        SharedData.Offset.Y = 0;
                    }
                }
                else
                {
                    double factor = ZoomFactor;
                    if (e.Delta < 0)
                    {
                        double mouseWorldX = SharedData.PutInWorldPosScaleX(e.X);
                        double mouseWorldY = SharedData.PutInWorldPosScaleY(e.Y);

                        SharedData.Scale *= factor;

                        if (!focused)
                        {
                            SharedData.Offset.X = e.X - SharedData.SW / 2.0 - SharedData.PutInScreenScale(mouseWorldX - SharedData.FocusPosition.X);
                            SharedData.Offset.Y = e.Y - SharedData.SH / 2.0 - SharedData.PutInScreenScale(mouseWorldY - SharedData.FocusPosition.Y);
                        }
                        else
                        {
                            SharedData.Offset.X = 0;
                            SharedData.Offset.Y = 0;
                        }
                    }
                }
                foreach (var b in SharedData.bodies)
                {
                    b.TrailDirty = true;
                    b.HyperbolaDirty = true;
                    b.OrbitalDirty = true;
                }
            }
        }

        ////////////////////////////////////////////////
        ///Time Scale Slider
        bool SmallScale = false;
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            nextIntersectionUpdate -= nextIdT;
            if (!SmallScale)
            {
                if (trackBar1.Value == 0) timeScale = 0;
                else if (trackBar1.Value == 1)
                {
                    timeScale = 10000;
                    nextIdT = 10;
                }
                else if (trackBar1.Value == 2.0f)
                {
                    timeScale = 100000;
                    nextIdT = 10;
                }
                else if (trackBar1.Value == 3)
                {
                    timeScale = 1000000;
                    nextIdT = 100;
                }
                else if (trackBar1.Value == 4)
                {
                    timeScale = 10000000;
                    nextIdT = 1000;
                }
                else if (trackBar1.Value == 5)
                {
                    timeScale = 25000000;
                    nextIdT = 1000;
                }
                else if (trackBar1.Value == 6)
                {
                    timeScale = 50000000;
                    nextIdT = 1000;
                }
                else if (trackBar1.Value == 7)
                {
                    timeScale = 100000000;
                    nextIdT = 1000;
                }
            }
            else
            {
                if (trackBar1.Value == 0) timeScale = 0;
                else if (trackBar1.Value == 1)
                {
                    timeScale = 1;
                    nextIdT = 0.0001;
                }
                else if (trackBar1.Value == 2.0f)
                {
                    timeScale = 2.0f;
                    nextIdT = 0.0001;
                }
                else if (trackBar1.Value == 3)
                {
                    timeScale = 5;
                    nextIdT = 0.0001;
                }
                else if (trackBar1.Value == 4)
                {
                    timeScale = 10;
                    nextIdT = 0.001;
                }
                else if (trackBar1.Value == 5)
                {
                    timeScale = 100;
                    nextIdT = 0.1;
                }
                else if (trackBar1.Value == 6)
                {
                    timeScale = 1000;
                    nextIdT = 1;
                }
                else if (trackBar1.Value == 7)
                {
                    timeScale = 5000;
                    nextIdT = 1;
                }
            }
            foreach (var b in SharedData.bodies)
            {
                b.OrbitalDirty = true;
                b.HyperbolaDirty = true;
            }
            foreach (var body in SharedData.bodies)
                body.Initialized = false;
            dt = (timer1.Interval / 1000.0) * timeScale;
            if (timeScale >= 1000000) SharedData.UseAnalytic = true;
            else SharedData.UseAnalytic = false;
            SharedData.UseAnalytic = true;
        }
        void TimeScaleLabel()
        {
            if (!SmallScale)
            {
                label9.Text = "100,000,000x";
                label10.Text = "50,000,000x";
                label11.Text = "25,000,000x";
                label12.Text = "10,000,000x";
                label13.Text = "1,000,000x";
                label14.Text = "100,000x";
                label15.Text = "10,000x";
                label16.Text = "0x";
            }
            else
            {
                label9.Text = "5,000x";
                label10.Text = "1,000x";
                label11.Text = "100x";
                label12.Text = "10x";
                label13.Text = "5x";
                label14.Text = "2x";
                label15.Text = "1x";
                label16.Text = "0x";
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!paused)
            {
                Vector clicked = new Vector(e.X, e.Y);
                actionInd = CheckIfClicked(clicked);
                if (actionInd != -1 && e.Button == MouseButtons.Right && shouldOpenActionMenu)
                {
                    groupBox8.Location = new Point(currLocation.X, currLocation.Y);
                    button11.Text = "FOCUS";
                    groupBox9.Visible = false;
                    groupBox10.Visible = false;
                    groupBox8.Visible = true;
                    textBox8.Text = SharedData.bodies[actionInd].Name;
                }
                else if (actionInd == -1 && e.Button == MouseButtons.Right && shouldOpenSpawnMenu)
                {
                    if (focused)
                    {
                        groupBox9.Location = new Point(currLocation.X, currLocation.Y);
                        groupBox8.Visible = false;
                        button11.Text = "FOCUS";
                        groupBox9.Visible = true;
                        currInd = comboBox2.SelectedIndex;
                        validMass = false;
                    }
                    else
                    {
                        groupBox10.Location = new Point(currLocation.X, currLocation.Y);
                        button11.Text = "FOCUS";
                        groupBox8.Visible = false;
                        groupBox10.Visible = true;
                        currInd = comboBox3.SelectedIndex;
                        validMass = false;
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Celestial_Body b in SharedData.bodies) b.Dispose();
            SharedData.bodies.Clear();
            SharedData.Offset = new Vector();
            focus = null;
            focused = false;
            nextIntersectionUpdate = 0;
            placeInOrbit = false;
            checkBox1.Checked = false;
            SharedData.totalElapsedTime = 0;
            SharedData.Scale = SharedData.AU / 300;
            Presets.mainBelt.Clear();
            Presets.kuiperBelt.Clear();
            SharedData.FocusPosition = new Vector();
            label23.Text = "Bodies: " + SharedData.bodies.Count.ToString();
        }

        ////////////////////////
        ///Pause Menu

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            pressed = false;
            throttling = false;
            dethrottling = false;
        }
        bool paused = false;
        bool pressed = false;
        int trackBarValue = -1;
        bool throttling = false;
        bool dethrottling = false;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!pressed && e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                groupBox2.Visible = !groupBox2.Visible;
                if (!paused) trackBarValue = trackBar1.Value;
                paused = !paused;
                if (paused) trackBar1.Value = 0;
                else trackBar1.Value = trackBarValue;
                trackBar1.Enabled = !trackBar1.Enabled;
                groupBox4.Visible = false;
                groupBox5.Visible = false;
                groupBox6.Visible = false;
                groupBox7.Visible = false;
                groupBox8.Visible = false;
                button11.Text = "FOCUS";
                groupBox9.Visible = false;
                groupBox10.Visible = false;
                groupBox11.Visible = false;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (focus != null)
                {
                    int fInd = SharedData.bodies.IndexOf(focus);
                    if (!focused)
                    {
                        PutInFocus(SharedData.bodies[fInd]);
                        focused = true;
                    }
                    else if (fInd != SharedData.bodies.Count - 1)
                    {
                        PutInFocus(SharedData.bodies[fInd + 1]);
                        focused = true;
                    }
                    else if (fInd == SharedData.bodies.Count - 1)
                    {
                        PutInFocus(SharedData.bodies[0]);
                        focused = true;
                    }
                }
                else
                {
                    if (SharedData.bodies.Count > 0)
                    {
                        PutInFocus(SharedData.bodies[0]);
                        focused = true;
                    }
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (focus != null)
                {
                    int fInd = SharedData.bodies.IndexOf(focus);
                    if (!focused)
                    {
                        PutInFocus(SharedData.bodies[fInd]);
                        focused = true;
                    }
                    else if (fInd != 0)
                    {
                        PutInFocus(SharedData.bodies[fInd - 1]);
                        focused = true;
                    }
                    else if (fInd == 0)
                    {
                        PutInFocus(SharedData.bodies[SharedData.bodies.Count - 1]);
                        focused = true;
                    }
                }
                else
                {
                    if (SharedData.bodies.Count > 0)
                    {
                        PutInFocus(SharedData.bodies[0]);
                        focused = true;
                    }
                }
            }
            else if ((e.KeyCode == Keys.D) && focused && Controling && focus.Free)
            {
                focus.DirAngleSS += 1;
                if (focus.DirAngleSS > 360) focus.DirAngleSS = 0;
            }
            else if ((e.KeyCode == Keys.A) && focused && Controling && focus.Free)
            {
                focus.DirAngleSS -= 1;
                if (focus.DirAngleSS < 0) focus.DirAngleSS = 360;
            }
            else if (e.KeyCode == Keys.ShiftKey && focused && Controling)
            {
                throttling = true;
            }
            else if (e.KeyCode == Keys.ControlKey && focused && Controling)
            {
                dethrottling = true;
            }
            else if (!pressed && e.KeyCode == Keys.X && focused && Controling)
            {
                focus.Throttle = 0;
            }
            else if (!pressed && e.KeyCode == Keys.Z && focused && Controling)
            {
                focus.Throttle = 100;
            }
            pressed = true;
        }

        int currInd = -1;

        Celestial_Body ChooseBody(Vector pos, Vector vel, int ind)
        {
            if (ind == 0)
            {
                return BodyPresets.SpawnSun(pos, vel);
            }
            else if (ind == 1)
            {
                return BodyPresets.SpawnMercury(pos, vel);
            }
            else if (ind == 2.0f)
            {
                return BodyPresets.SpawnVenus(pos, vel);
            }
            else if (ind == 3)
            {
                return BodyPresets.SpawnEarth(pos, vel);
            }
            else if (ind == 4)
            {
                return BodyPresets.SpawnMoon(pos, vel);
            }
            else if (ind == 5)
            {
                return BodyPresets.SpawnMars(pos, vel);
            }
            else if (ind == 6)
            {
                return BodyPresets.SpawnJupiter(pos, vel);
            }
            else if (ind == 7)
            {
                return BodyPresets.SpawnSaturn(pos, vel);
            }
            else if (ind == 8)
            {
                return BodyPresets.SpawnUranus(pos, vel);
            }
            else if (ind == 9)
            {
                return BodyPresets.SpawnNeptune(pos, vel);
            }
            else if (ind == 10)
            {
                return BodyPresets.SpawnPluto(pos, vel);
            }
            else if (ind == 11)
            {
                return BodyPresets.SpawnSagittariusA(pos, vel);
            }
            else if (ind == 12)
            {
                return BodyPresets.SpawnSpaceship(pos, vel);
            }
            else if (ind == 13)
            {
                return BodyPresets.SpawnViltrum(pos, vel);
            }
            return null;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            currInd = comboBox2.SelectedIndex;
            pendingName = "";
            textBox13.Text = pendingName;
            textBox15.Text = pendingName;
        }
        double pendingMass = 0;
        private void comboBox2_TextUpdate(object sender, EventArgs e)
        {
            pendingName = "";
            textBox13.Text = pendingName;
            textBox15.Text = pendingName;
            label6.Visible = false;
            if (double.TryParse(comboBox2.Text, out double result) && result > 0)
            {
                pendingMass = result;
                currInd = -1;
            }
            else if (!comboBox2.Items.Contains(comboBox2.Text))
            {
                label6.Visible = true;
                validMass = false;
                currInd = -1;
            }

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
            {
                SharedData.PredictIntersections = true;
                wasChecked = true;
            }
            else
            {
                SharedData.PredictIntersections = false;
                wasChecked = false;
            }
        }
        bool wasChecked = true;

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
            {
                SharedData.DrawOrbits = true;
                checkBox5.Enabled = true;
                if (!wasChecked) SharedData.PredictIntersections = false;
                else SharedData.PredictIntersections = true;
            }

            else
            {
                SharedData.DrawOrbits = false;
                checkBox5.Enabled = false;
                SharedData.PredictIntersections = false;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked) SmallScale = true;
            else SmallScale = false;
            trackBar1.Value = 0;
            trackBarValue = 0;
            TimeScaleLabel();
        }

        private void snapNumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SharedData.OrbitDrawSize = (double)snapNumericUpDown1.Value;
        }

        private void snapNumericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            precisionFactor = (double)snapNumericUpDown2.Value;
        }

        private void trackBar1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void customRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (customRadioButton1.Checked) focus.Prograde = true;
            else focus.Prograde = false;
        }

        private void customRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (customRadioButton2.Checked) focus.Retrograde = true;
            else focus.Retrograde = false;
        }

        private void customRadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (customRadioButton3.Checked) focus.Radial = true;
            else focus.Radial = false;
        }

        private void customRadioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (customRadioButton4.Checked) focus.Antiradial = true;
            else focus.Antiradial = false;
        }

        private void customRadioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (customRadioButton5.Checked) focus.Free = true;
            else focus.Free = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!groupBox4.Visible && !groupBox5.Visible && !groupBox6.Visible && !groupBox7.Visible)
                groupBox4.Visible = true;
            else
            {
                groupBox4.Visible = false;
                groupBox5.Visible = false;
                groupBox6.Visible = false;
                groupBox7.Visible = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            groupBox5.Visible = true;
            groupBox4.Visible = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            groupBox4.Visible = true;
            groupBox5.Visible = false;

        }

        private void button5_Click(object sender, EventArgs e)
        {
            groupBox5.Visible = false;
            groupBox11.Visible = true;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            groupBox11.Visible = true;
            groupBox6.Visible = false;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            groupBox7.Visible = true;
            groupBox6.Visible = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            groupBox7.Visible = false;
            groupBox6.Visible = true;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            groupBox4.Visible = false;
        }

        private void customRadioButton1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void customRadioButton2_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void customRadioButton3_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void customRadioButton4_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void customRadioButton5_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }
        bool shouldSetName = true;

        private void button13_Click(object sender, EventArgs e)
        {
            SharedData.bodies[actionInd].Name = textBox8.Text;
            shouldSetName = true;
        }
        private void button11_Click(object sender, EventArgs e)
        {
            if (SharedData.bodies[actionInd] == focus && focused)
            {
                SharedData.Scale = focus.Radius * 2.0f / 300.0;
                button11.Text = "CENTER";
            }
            else
            {
                PutInFocus(SharedData.bodies[actionInd]);
                button11.Text = "CENTER";
            }
        }
        private void button12_Click(object sender, EventArgs e)
        {
            if (SharedData.bodies[actionInd] is Spaceship s)
            {
                customRadioButton5.Checked = true;
            }
            if (SharedData.bodies[actionInd] == focus)
            {
                focus = null;
                focused = false;
                placeInOrbit = false;
                checkBox1.Checked = false;
            }
            RemoveBody(SharedData.bodies[actionInd]);
            groupBox8.Visible = false;
            button11.Text = "FOCUS";
            return;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            groupBox8.Visible = false;
            button11.Text = "FOCUS";
        }

        private void button15_Click(object sender, EventArgs e)
        {
            groupBox2.Visible = false;
            paused = false;
            trackBar1.Value = trackBarValue;
            trackBar1.Enabled = true;
            groupBox4.Visible = false;
            groupBox5.Visible = false;
            groupBox6.Visible = false;
            groupBox7.Visible = false;
        }

        private void button17_Click(object sender, EventArgs e)
        {
            groupBox4.Visible = false;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            groupBox6.Visible = false;
        }

        private void button18_Click(object sender, EventArgs e)
        {
            groupBox5.Visible = false;
        }

        private void button19_Click(object sender, EventArgs e)
        {
            groupBox7.Visible = false;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            groupBox9.Visible = false;
        }
        private void button21_Click(object sender, EventArgs e)
        {
            groupBox9.Visible = false;
            if (currInd == 0)
            {
                bodyMass = SharedData.SolarMass;
            }
            else if (currInd == 1)
            {
                bodyMass = SharedData.MercuryMass;
            }
            else if (currInd == 2.0f)
            {
                bodyMass = SharedData.VenusMass;
            }
            else if (currInd == 3)
            {
                bodyMass = SharedData.EarthMass;
            }
            else if (currInd == 4)
            {
                bodyMass = SharedData.MoonMass;
            }
            else if (currInd == 5)
            {
                bodyMass = SharedData.MarsMass;
            }
            else if (currInd == 6)
            {
                bodyMass = SharedData.JupiterMass;
            }
            else if (currInd == 7)
            {
                bodyMass = SharedData.SaturnMass;
            }
            else if (currInd == 8)
            {
                bodyMass = SharedData.UranusMass;
            }
            else if (currInd == 9)
            {
                bodyMass = SharedData.NeptuneMass;
            }
            else if (currInd == 10)
            {
                bodyMass = SharedData.PlutoMass;
            }
            else if (currInd == 11)
            {
                bodyMass = SharedData.SagittariusAMass;
            }
            else if (currInd == 12)
            {
                bodyMass = SharedData.SpaceshipMass;
            }
            else if (currInd == 13)
            {
                bodyMass = SharedData.ViltrumMass;
            }
            if (currInd == -1) bodyMass = pendingMass;
            validMass = true;
            if (placeInOrbit && double.TryParse(textBox12.Text, out double result) && result > 0)
            {
                if (defaultUnit < 3)
                    spawnDistance = result * Math.Pow(1000, defaultUnit) + focus.Radius;
                else
                {
                    if (defaultUnit == 3) spawnDistance = result * SharedData.AU + focus.Radius;
                    else if (defaultUnit == 4) spawnDistance = result * SharedData.LightYear + focus.Radius;
                }
            }
        }

        bool placeInOrbit = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                placeInOrbit = true;
                textBox12.Enabled = true;
            }
            else
            {
                placeInOrbit = false;
                textBox12.Enabled = false;
            }
        }
        private void button22_Click(object sender, EventArgs e)
        {
            groupBox10.Visible = false;
            if (currInd == 0)
            {
                bodyMass = SharedData.SolarMass;
            }
            else if (currInd == 1)
            {
                bodyMass = SharedData.MercuryMass;
            }
            else if (currInd == 2.0f)
            {
                bodyMass = SharedData.VenusMass;
            }
            else if (currInd == 3)
            {
                bodyMass = SharedData.EarthMass;
            }
            else if (currInd == 4)
            {
                bodyMass = SharedData.MoonMass;
            }
            else if (currInd == 5)
            {
                bodyMass = SharedData.MarsMass;
            }
            else if (currInd == 6)
            {
                bodyMass = SharedData.JupiterMass;
            }
            else if (currInd == 7)
            {
                bodyMass = SharedData.SaturnMass;
            }
            else if (currInd == 8)
            {
                bodyMass = SharedData.UranusMass;
            }
            else if (currInd == 9)
            {
                bodyMass = SharedData.NeptuneMass;
            }
            else if (currInd == 10)
            {
                bodyMass = SharedData.PlutoMass;
            }
            else if (currInd == 11)
            {
                bodyMass = SharedData.SagittariusAMass;
            }
            else if (currInd == 12)
            {
                bodyMass = SharedData.SpaceshipMass;
            }
            else if (currInd == 13)
            {
                bodyMass = SharedData.ViltrumMass;
            }
            if (currInd == -1) bodyMass = pendingMass;
            validMass = true;
        }

        private void button23_Click(object sender, EventArgs e)
        {
            groupBox10.Visible = false;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            currInd = comboBox3.SelectedIndex;
            pendingName = "";
            textBox13.Text = pendingName;
            textBox15.Text = pendingName;
        }

        private void comboBox3_TextUpdate(object sender, EventArgs e)
        {
            pendingName = "";
            textBox13.Text = pendingName;
            textBox15.Text = pendingName;
            label6.Visible = false;
            if (double.TryParse(comboBox3.Text, out double result) && result > 0)
            {
                pendingMass = result;
                currInd = -1;
            }
            else if (!comboBox3.Items.Contains(comboBox3.Text))
            {
                label6.Visible = true;
                validMass = false;
                currInd = -1;
            }
        }
        string pendingName = "";
        private void textBox13_TextChanged(object sender, EventArgs e)
        {
            pendingName = textBox13.Text;
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            pendingName = textBox15.Text;
        }

        int defaultUnit = 0;
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            defaultUnit = comboBox4.SelectedIndex;
            if (placeInOrbit && double.TryParse(textBox12.Text, out double result) && result > 0)
            {
                if (defaultUnit < 3)
                    spawnDistance = result * Math.Pow(1000, defaultUnit) + focus.Radius;
                else
                {
                    if (defaultUnit == 3) spawnDistance = result * SharedData.AU + focus.Radius;
                    else if (defaultUnit == 4) spawnDistance = result * SharedData.LightYear + focus.Radius;
                }
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            groupBox11.Visible = false;
            groupBox6.Visible = true;
        }

        private void button25_Click(object sender, EventArgs e)
        {
            groupBox11.Visible = false;
            groupBox5.Visible = true;
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked) SharedData.drawGrid = true;
            else SharedData.drawGrid = false;
        }
    }
    public class SnapNumericUpDown : NumericUpDown
    {
        public override void UpButton() =>
            Value = Math.Min(Value % 10 == 0 ? Value + 10 : Math.Ceiling(Value / 10) * 10, Maximum);

        public override void DownButton() =>
            Value = Math.Max(Value % 10 == 0 ? Value - 10 : Math.Floor(Value / 10) * 10, Minimum);
    }
    public class CustomRadioButton : RadioButton
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CheckColor { get; set; } = Color.Green;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int RadioSize { get; set; } = 50;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Symbol { get; set; }

        public CustomRadioButton()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(Parent.BackColor);

            int rectSize = RadioSize;
            Rectangle radioRect = new Rectangle(0, 0,RadioSize, RadioSize);

            using (Pen p = new Pen(Color.DimGray, 2.0f))
            {
                g.FillEllipse(new SolidBrush(Color.IndianRed), radioRect);
                g.DrawEllipse(p, 2.0f, 2.0f, rectSize-4, rectSize-4);
            }

            if (Checked)
            {
                Rectangle innerRect = radioRect;
                innerRect.Inflate(-4, -4);

                using (SolidBrush b = new SolidBrush(CheckColor))
                {
                    g.FillEllipse(b, innerRect);
                }

                using (Pen glow = new Pen(Color.FromArgb(100, CheckColor), 2.0f))
                {
                    g.DrawEllipse(glow, radioRect);
                }
            }

            Rectangle textRect = new Rectangle(rectSize + 8, 0, Width - rectSize - 8, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            if(Symbol == 0)
            {
                using (Pen p = new Pen(Color.GreenYellow, 3.0f))
                {
                    g.DrawEllipse(p, 17.5f, 17.5f, 15, 15);
                    g.DrawEllipse(p, 24.5f, 24.5f, 1, 1);
                    g.DrawLine(p, 10f, 25f, 18f, 25f);
                    g.DrawLine(p, 40f, 25f, 32f, 25f);
                    g.DrawLine(p, 25f, 10f, 25f, 18f);
                }
            }
            else if (Symbol == 1)
            {
                using (Pen p = new Pen(Color.GreenYellow, 3.0f))
                {
                    g.DrawEllipse(p, 17.5f, 17.5f, 15, 15);
                    g.DrawLine(p, 19.25f, 19.25f, 30, 30f);
                    g.DrawLine(p, 30.75f, 19.25f, 20.25f, 30f);
                    g.DrawLine(p, 13f, 34.75f, 20f, 28.75f);
                    g.DrawLine(p, 37f, 34.75f, 30f, 28.75f);
                    g.DrawLine(p, 25f, 10f, 25f, 18f);
                }
            }
            else if (Symbol == 2)
            {
                using (Pen p = new Pen(Color.Cyan, 3.0f))
                {
                    g.DrawEllipse(p, 17.5f, 17.5f, 15, 15);
                    g.DrawLine(p, 19.25f, 19.25f, 22.5f, 22.5f);
                    g.DrawLine(p, 30.75f, 19.25f, 27.5f, 22.5f);
                    g.DrawLine(p, 30f, 30f, 26.75f, 26.75f);
                    g.DrawLine(p, 20.25f, 30f, 23.5f, 26.75f);
                }
            }
            else if (Symbol == 3)
            {
                using (Pen p = new Pen(Color.Cyan, 3.0f))
                {
                    g.DrawEllipse(p, 17.5f, 17.5f, 15, 15);
                    g.DrawEllipse(p, 24.5f, 24.5f, 1, 1);
                    g.DrawLine(p, 19.25f, 19.25f, 16f, 16f);
                    g.DrawLine(p, 30.75f, 19.25f, 34f, 16f);
                    g.DrawLine(p, 30f, 30f, 34f, 34f);
                    g.DrawLine(p, 20.25f, 30f, 16f, 34f);
                }
            }
            else if (Symbol == 4)
            {
                using (Pen p = new Pen(Color.Orange, 3.0f))
                {
                    g.DrawEllipse(p, 17.5f, 17.5f, 15, 15);
                    g.DrawLine(p, 20, 25, 30, 25);
                }
            }
        }
    }
}

