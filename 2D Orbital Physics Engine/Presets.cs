using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace _2D_Orbital_Physics_Engine
{
    public static class Presets
    {
        public struct BeltParticle
        {
            public double r;
            public double startAngle;
            public double period;
            public int bright;
            public Celestial_Body parent;
        }
        public static Random beltRnd { get; set; } = new Random(42);
        public static List<BeltParticle> mainBelt { get; set; } = new List<BeltParticle>();
        public static List<BeltParticle> kuiperBelt { get; set; } = new List<BeltParticle>();

        static (Vector pos, Vector vel) OrbitalElements(double parentMass, double a, double e, double omega, double nu, Vector parentPos, Vector parentVel)
        {
            double omegaRad = omega * Math.PI / 180.0;
            double nuRad = nu * Math.PI / 180.0;

            double p = a * (1 - e * e);
            double r = p / (1 + e * Math.Cos(nuRad));

            double xLocal = r * Math.Cos(nuRad);
            double yLocal = r * Math.Sin(nuRad);

            double x = xLocal * Math.Cos(omegaRad) - yLocal * Math.Sin(omegaRad);
            double y = -(xLocal * Math.Sin(omegaRad) + yLocal * Math.Cos(omegaRad));

            double GM = SharedData.G * parentMass;
            double sqrtGMp = Math.Sqrt(GM / p);
            double vxLocal = -sqrtGMp * Math.Sin(nuRad);
            double vyLocal = sqrtGMp * (e + Math.Cos(nuRad));

            double vx = vxLocal * Math.Cos(omegaRad) - vyLocal * Math.Sin(omegaRad);
            double vy = -(vxLocal * Math.Sin(omegaRad) + vyLocal * Math.Cos(omegaRad));

            return (new Vector(parentPos.X + x, parentPos.Y + y), new Vector(parentVel.X + vx, parentVel.Y + vy));
        }

        public static void GenerateBelt(List<BeltParticle> belt, double innerAU, double outerAU, int count, double meanK, double sigmaK, Celestial_Body parent, Random rnd)
        {
            belt.Clear();
            for (int i = 0; i < count; i++)
            {
                double u1 = 1.0 - rnd.NextDouble();
                double u2 = 1.0 - rnd.NextDouble();
                double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

                double mean = innerAU + (outerAU - innerAU) * meanK;  
                double sigma = (outerAU - innerAU) * sigmaK;            

                double au = mean + gaussian * sigma;               
                double r = au * SharedData.AU;
                double period = 2 * Math.PI * Math.Sqrt((r * r * r) / (SharedData.G * SharedData.SolarMass));
                belt.Add(new BeltParticle
                {
                    r = r,
                    startAngle = rnd.NextDouble() * 2 * Math.PI,
                    period = period,
                    bright = rnd.Next(60, 150),
                    parent = parent,
                });
            }
        }
        public static void DrawBelt(Graphics g, List<Presets.BeltParticle> belt, SolidBrush brush, Vector Offset, double screenW, double screenH)
        {
            Color savedColor = brush.Color;
            foreach (var p in belt)
            {
                double angle = p.startAngle - (SharedData.totalElapsedTime / p.period) * 2 * Math.PI;

                double worldX = p.parent.Position.X + p.r * Math.Cos(angle);
                double worldY = p.parent.Position.Y + p.r * Math.Sin(angle);
                float screenX = SharedData.PutInScreenPosScaleXClamp(worldX);
                float screenY = SharedData.PutInScreenPosScaleYClamp(worldY);

                if (screenX < 0 || screenX > screenW) continue;
                if (screenY < 0 || screenY > screenH) continue;
                brush.Color = Color.FromArgb(p.bright, p.bright, p.bright);
                g.FillRectangle(brush, screenX, screenY, 1, 1);
                brush.Color = savedColor;
            }
        }

        public static void SpawnCompleteSolarSystem()
        {
            SharedData.bodies.Clear();
            beltRnd = new Random(42);
            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.SolarMass, new Vector(), "Sun"));
            GenerateBelt(mainBelt, 2.2, 3.2, 3000, 0.5, 0.2, SharedData.bodies[0], beltRnd);
            GenerateBelt(kuiperBelt, 30.0, 50.0, 5000, 0.5, 0.2, SharedData.bodies[0], beltRnd);

            var (posMer, velMer) = OrbitalElements(SharedData.SolarMass, 0.387 * SharedData.AU, 0.2056, 29.1, 110.19, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMer.X, posMer.Y, SharedData.MercuryMass, velMer, Color.Gray, "Mercury"));

            var (posVen, velVen) = OrbitalElements(SharedData.SolarMass, 0.723 * SharedData.AU, 6.755e-3, 5.51e1, 1.284e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posVen.X, posVen.Y, SharedData.VenusMass, velVen, Color.Yellow, "Venus"));

            //////////////////////////////////////////
            /// Earth System
            var (posEar, velEar) = OrbitalElements(SharedData.SolarMass, SharedData.AU, 1.61e-2, 3.02e2, 4.67e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEar.X, posEar.Y, SharedData.EarthMass, velEar, Color.SkyBlue, "Earth"));

            var (posMoo, velMoo) = OrbitalElements(SharedData.EarthMass, 2.567e-3 * SharedData.AU, 3.47e-2, 9.81e1, 4.317e1, posEar, velEar);
            SharedData.bodies.Add(SharedData.CreateBody(posMoo.X, posMoo.Y, SharedData.MoonMass, velMoo, Color.LightGray, "Moon"));

            //////////////////////////////////////////
            /// Mars System
            var (posMar, velMar) = OrbitalElements(SharedData.SolarMass, 1.524 * SharedData.AU, 9.33e-2, 2.87e2, 1.423e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMar.X, posMar.Y, SharedData.MarsMass, velMar, Color.Red, "Mars"));

            var (posPho, velPho) = OrbitalElements(SharedData.MarsMass, 6.27e-5 * SharedData.AU, 1.5e-2, 2.01e2, 1.22e2, posMar, velMar);
            SharedData.bodies.Add(SharedData.CreateBody(posPho.X, posPho.Y, SharedData.PhobosMass, velPho, Color.Orange, "Phobos"));

            var (posDei, velDei) = OrbitalElements(SharedData.MarsMass, 1.57e-4 * SharedData.AU, 3.17e-4, 2.58e2, 3.045e1, posMar, velMar);
            SharedData.bodies.Add(SharedData.CreateBody(posDei.X, posDei.Y, SharedData.DeimosMass, velDei, Color.DarkOrange, "Deimos"));

            //////////////////////////////////////////

            var (posHal, velHal) = OrbitalElements(SharedData.SolarMass, 1.785e1 * SharedData.AU, 9.667e-1, 1.112e2, 1.746e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posHal.X, posHal.Y, SharedData.HalleyMass, velHal, Color.DarkCyan, "Halley's Comet"));

            var (posBpp, velBpp) = OrbitalElements(SharedData.SolarMass, 1.832e2 * SharedData.AU, 9.949e-1, 1.307e2, 1.591e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posBpp.X, posBpp.Y, SharedData.HaleBoppMass, velBpp, Color.LightCyan, "Comet Hale-Bopp"));

            var (posEnck, velEnck) = OrbitalElements(SharedData.SolarMass, 2.217 * SharedData.AU, 8.471e-1, 1.865e2, 1.642e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEnck.X, posEnck.Y, SharedData.EnckeMass, velEnck, Color.Cyan, "Comet Encke"));

            var (posCer, velCer) = OrbitalElements(SharedData.SolarMass, 2.76 * SharedData.AU, 7.96e-2, 7.3e1, 2.74e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posCer.X, posCer.Y, SharedData.CeresMass, velCer, Color.SlateGray, "Ceres"));

            var (posVes, velVes) = OrbitalElements(SharedData.SolarMass, 2.36 * SharedData.AU, 8.92e-2, 1.5e2, 7.7e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posVes.X, posVes.Y, SharedData.VestaMass, velVes, Color.Gray, "Vesta"));

            var (posPal, velPal) = OrbitalElements(SharedData.SolarMass, 2.771 * SharedData.AU, 2.307e-1, 3.103e2, 2.416e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posPal.X, posPal.Y, SharedData.PallasMass, velPal, Color.Gray, "Pallas"));

            var (posHyg, velHyg) = OrbitalElements(SharedData.SolarMass, 3.137 * SharedData.AU, 1.176e-1, 3.131e2, 1.537e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posHyg.X, posHyg.Y, SharedData.HygieaMass, velHyg, Color.SlateGray, "Hygiea"));

            var (posEro, velEro) = OrbitalElements(SharedData.SolarMass, 1.458 * SharedData.AU, 2.229e-1, 1.786e2, 2.542e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEro.X, posEro.Y, SharedData.ErosMass, velEro, Color.LightGray, "Eros"));

            //////////////////////////////////////////
            /// Jupiter System
            var (posJup, velJup) = OrbitalElements(SharedData.SolarMass, 5.2 * SharedData.AU, 4.9e-2, 2.74e2, 2.61e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posJup.X, posJup.Y, SharedData.JupiterMass, velJup, Color.Beige, "Jupiter"));

            var (posIo, velIo) = OrbitalElements(SharedData.JupiterMass, 2.821e-3 * SharedData.AU, 3.555e-3, 3.246e1, 1.89e2, posJup, velJup);
            SharedData.bodies.Add(SharedData.CreateBody(posIo.X, posIo.Y, SharedData.IoMass, velIo, Color.GreenYellow, "Io"));

            var (posEur, velEur) = OrbitalElements(SharedData.JupiterMass, 4.487e-3 * SharedData.AU, 9.418e-3, 2.046e2, 9.077e1, posJup, velJup);
            SharedData.bodies.Add(SharedData.CreateBody(posEur.X, posEur.Y, SharedData.EuropaMass, velEur, Color.Wheat, "Europa"));

            var (posGan, velGan) = OrbitalElements(SharedData.JupiterMass, 7.158e-3 * SharedData.AU, 2.157e-3, 3.206e2, 2.955e2, posJup, velJup);
            SharedData.bodies.Add(SharedData.CreateBody(posGan.X, posGan.Y, SharedData.GanymedeMass, velGan, Color.LightGray, "Ganymede"));

            var (posCal, velCal) = OrbitalElements(SharedData.JupiterMass, 1.258e-2 * SharedData.AU, 7.112e-3, 1.945e1, 5.847e1, posJup, velJup);
            SharedData.bodies.Add(SharedData.CreateBody(posCal.X, posCal.Y, SharedData.CallistoMass, velCal, Color.LightYellow, "Callisto"));

            //////////////////////////////////////////
            /// Saturn System
            var (posSat, velSat) = OrbitalElements(SharedData.SolarMass, 9.535 * SharedData.AU, 5.370e-2, 3.385e2, 6.266e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posSat.X, posSat.Y, SharedData.SaturnMass, velSat, Color.BurlyWood, "Saturn", true));

            var (posEnc, velEnc) = OrbitalElements(SharedData.SaturnMass, 1.593e-3 * SharedData.AU, 3.524e-3, 6.731e1, 1.374e2, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posEnc.X, posEnc.Y, SharedData.EnceladusMass, velEnc, Color.LightCyan, "Enceladus"));

            var (posTit, velTit) = OrbitalElements(SharedData.SaturnMass, 8.168e-3 * SharedData.AU, 2.875e-2, 1.684e2, 2.389e2, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posTit.X, posTit.Y, SharedData.TitanMass, velTit, Color.LightYellow, "Titan"));

            var (posMim, velMim) = OrbitalElements(SharedData.SaturnMass, 1.243e-3 * SharedData.AU, 1.914e-2, 1.885e2, 2.585e2, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posMim.X, posMim.Y, SharedData.MimasMass, velMim, Color.LightGray, "Mimas"));

            var (posIap, velIap) = OrbitalElements(SharedData.SaturnMass, 2.378e-2 * SharedData.AU, 2.803e-2, 2.287e2, 4.879, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posIap.X, posIap.Y, SharedData.IapetusMass, velIap, Color.SandyBrown, "Iapetus"));

            var (posRhe, velRhe) = OrbitalElements(SharedData.SaturnMass, 3.524e-3 * SharedData.AU, 1.102e-3, 1.721e2, 3.201e2, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posRhe.X, posRhe.Y, SharedData.RheaMass, velRhe, Color.Gray, "Rhea"));

            var (posDio, velDio) = OrbitalElements(SharedData.SaturnMass, 2.524e-3 * SharedData.AU, 2.557e-3, 8.053e1, 4.963e1, posSat, velSat);
            SharedData.bodies.Add(SharedData.CreateBody(posDio.X, posDio.Y, SharedData.DioneMass, velDio, Color.SlateGray, "Dione"));

            //////////////////////////////////////////
            /// Uranus System
            var (posUra, velUra) = OrbitalElements(SharedData.SolarMass, 1.922e1 * SharedData.AU, 4.572e-2, 9.892e1, 1.753e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posUra.X, posUra.Y, SharedData.UranusMass, velUra, Color.Cyan, "Uranus", false, false, true));

            var (posTita, velTita) = OrbitalElements(SharedData.UranusMass, 2.916e-3 * SharedData.AU, 2.449e-3, 2.541e2, 5.564e1, posUra, velUra);
            SharedData.bodies.Add(SharedData.CreateBody(posTita.X, posTita.Y, SharedData.TitaniaMass, velTita, Color.LightSlateGray, "Titania"));

            var (posObe, velObe) = OrbitalElements(SharedData.UranusMass, 3.900e-3 * SharedData.AU, 1.910e-3, 1.544e2, 3.605e1, posUra, velUra);
            SharedData.bodies.Add(SharedData.CreateBody(posObe.X, posObe.Y, SharedData.OberonMass, velObe, Color.LightSalmon, "Oberon"));

            var (posUmb, velUmb) = OrbitalElements(SharedData.UranusMass, 1.777e-3 * SharedData.AU, 3.290e-3, 5.023, 7.865e1, posUra, velUra);
            SharedData.bodies.Add(SharedData.CreateBody(posUmb.X, posUmb.Y, SharedData.UmbrielMass, velUmb, Color.DarkGray, "Umbriel"));

            var (posAri, velAri) = OrbitalElements(SharedData.UranusMass, 1.276e-3 * SharedData.AU, 1.163e-3, 8.662e1, 1.819e2, posUra, velUra);
            SharedData.bodies.Add(SharedData.CreateBody(posAri.X, posAri.Y, SharedData.ArielMass, velAri, Color.Gray, "Ariel"));

            var (posMir, velMir) = OrbitalElements(SharedData.UranusMass, 8.682e-4 * SharedData.AU, 1.288e-3, 5.019e1, 2.164e2, posUra, velUra);
            SharedData.bodies.Add(SharedData.CreateBody(posMir.X, posMir.Y, SharedData.MirandaMass, velMir, Color.LightGray, "Miranda"));

            //////////////////////////////////////////
            /// Neptune System
            var (posNep, velNep) = OrbitalElements(SharedData.SolarMass, 3.018e1 * SharedData.AU, 8.711e-3, 2.463e2, 3.036e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posNep.X, posNep.Y, SharedData.NeptuneMass, velNep, Color.Blue, "Neptune", false, false, false, true));

            var (posTri, velTri) = OrbitalElements(SharedData.NeptuneMass, 2.371e-3 * SharedData.AU, 2.777e-5, 2.060e2, 3.337e2, posNep, velNep);
            SharedData.bodies.Add(SharedData.CreateBody(posTri.X, posTri.Y, SharedData.TritonMass, velTri, Color.LightGray, "Triton"));

            var (posPro, velPro) = OrbitalElements(SharedData.NeptuneMass, 7.866e-4 * SharedData.AU, 7.078e-4, 2.160e2, 3.476e2, posNep, velNep);
            SharedData.bodies.Add(SharedData.CreateBody(posPro.X, posPro.Y, SharedData.ProteusMass, velPro, Color.Gray, "Proteus"));

            var (posNer, velNer) = OrbitalElements(SharedData.NeptuneMass, 3.688e-2 * SharedData.AU, 7.471e-1, 2.973e2, 2.166e2, posNep, velNep);
            SharedData.bodies.Add(SharedData.CreateBody(posNer.X, posNer.Y, SharedData.NereidMass, velNer, Color.DarkGray, "Nereid"));

            //////////////////////////////////////////
            /// Pluto System
            var (posPlu, velPlu) = OrbitalElements(SharedData.SolarMass, 4.014e1 * SharedData.AU, 2.577e-1, 1.162e2, 4.154e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posPlu.X, posPlu.Y, SharedData.PlutoMass, velPlu, Color.DarkOrange, "Pluto"));

            var (posCha, velCha) = OrbitalElements(SharedData.PlutoMass, 1.309e-4 * SharedData.AU, 1.605e-4, 1.726e2, 1.171e2, posPlu, velPlu);
            SharedData.bodies.Add(SharedData.CreateBody(posCha.X, posCha.Y, SharedData.CharonMass, velCha, Color.LightGray, "Charon"));

            var (posNix, velNix) = OrbitalElements(SharedData.PlutoMass, 8.480e-4 * SharedData.AU, 5.989e-1, 2.838e2, 3.562e2, posPlu, velPlu);
            SharedData.bodies.Add(SharedData.CreateBody(posNix.X, posNix.Y, SharedData.NixMass, velNix, Color.DarkSlateGray, "Nix"));

            var (posHyd, velHyd) = OrbitalElements(SharedData.PlutoMass, 3.614e-4 * SharedData.AU, 2.062e-1, 3.038e2, 2.110e2, posPlu, velPlu);
            SharedData.bodies.Add(SharedData.CreateBody(posHyd.X, posHyd.Y, SharedData.HydraMass, velHyd, Color.SlateGray, "Hydra"));

            var (posKer, velKer) = OrbitalElements(SharedData.PlutoMass, 3.967e-4 * SharedData.AU, 1.505e-1, 3.119e2, 8.317e1, posPlu, velPlu);
            SharedData.bodies.Add(SharedData.CreateBody(posKer.X, posKer.Y, SharedData.KerberosMass, velKer, Color.DarkGray, "Kerberos"));

            var (posSty, velSty) = OrbitalElements(SharedData.PlutoMass, 6.711e-4 * SharedData.AU, 5.566e-1, 3.011e2, 8.015, posPlu, velPlu);
            SharedData.bodies.Add(SharedData.CreateBody(posSty.X, posSty.Y, SharedData.StyxMass, velSty, Color.LightGray, "Styx"));

            //////////////////////////////////////////

            var (posHau, velHau) = OrbitalElements(SharedData.SolarMass, 4.321e1 * SharedData.AU, 1.929e-1, 2.390e2, 1.951e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posHau.X, posHau.Y, SharedData.HaumeaMass, velHau, Color.RosyBrown, "Haumea"));

            var (posMak, velMak) = OrbitalElements(SharedData.SolarMass, 4.551e1 * SharedData.AU, 1.590e-1, 2.953e2, 1.581e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMak.X, posMak.Y, SharedData.MakemakeMass, velMak, Color.PaleVioletRed, "Makemake"));

            var (posEri, velEri) = OrbitalElements(SharedData.SolarMass, 6.780e1 * SharedData.AU, 4.381e-1, 1.515e2, 1.880e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEri.X, posEri.Y, SharedData.ErisMass, velEri, Color.GhostWhite, "Eris"));

            var (posSed, velSed) = OrbitalElements(SharedData.SolarMass, 4.876e2 * SharedData.AU, 8.437e-1, 3.114e2, 3.146e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posSed.X, posSed.Y, SharedData.SednaMass, velSed, Color.RosyBrown, "Sedna"));

            var (posVoy1, velVoy1) = OrbitalElements(SharedData.SolarMass, -3.217 * SharedData.AU, 3.749, 3.384e2, 9.923e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posVoy1.X, posVoy1.Y, SharedData.Voyager1Mass, velVoy1, Color.Gold, "Voayger 1"));

            var (posVoy2, velVoy2) = OrbitalElements(SharedData.SolarMass, -4.017 * SharedData.AU, 6.288, 1.300e2, 8.251e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posVoy2.X, posVoy2.Y, SharedData.Voyager2Mass, velVoy2, Color.Gold, "Voayger 2"));
        }

        public static void SpawnSolarSystem()
        {
            SharedData.bodies.Clear();
            beltRnd = new Random(42);
            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.SolarMass, new Vector(), "Sun"));
            GenerateBelt(mainBelt, 2.2, 3.2, 3000, 0.5, 0.2, SharedData.bodies[0], beltRnd);
            GenerateBelt(kuiperBelt, 30.0, 50.0, 5000, 0.5, 0.2, SharedData.bodies[0], beltRnd);

            var (posMer, velMer) = OrbitalElements(SharedData.SolarMass, 0.387 * SharedData.AU, 0.2056, 29.1, 110.19, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMer.X, posMer.Y, SharedData.MercuryMass, velMer, Color.Gray, "Mercury"));

            var (posVen, velVen) = OrbitalElements(SharedData.SolarMass, 0.723 * SharedData.AU, 6.755e-3, 5.51e1, 1.284e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posVen.X, posVen.Y, SharedData.VenusMass, velVen, Color.Yellow, "Venus"));

            //////////////////////////////////////////
            /// Earth System
            var (posEar, velEar) = OrbitalElements(SharedData.SolarMass, SharedData.AU, 1.61e-2, 3.02e2, 4.67e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEar.X, posEar.Y, SharedData.EarthMass, velEar, Color.SkyBlue, "Earth"));

            var (posMoo, velMoo) = OrbitalElements(SharedData.EarthMass, 2.567e-3 * SharedData.AU, 3.47e-2, 9.81e1, 4.317e1, posEar, velEar);
            SharedData.bodies.Add(SharedData.CreateBody(posMoo.X, posMoo.Y, SharedData.MoonMass, velMoo, Color.LightGray, "Moon"));

            //////////////////////////////////////////
            /// Mars System
            var (posMar, velMar) = OrbitalElements(SharedData.SolarMass, 1.524 * SharedData.AU, 9.33e-2, 2.87e2, 1.423e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMar.X, posMar.Y, SharedData.MarsMass, velMar, Color.Red, "Mars"));

            //////////////////////////////////////////
            /// Jupiter System
            var (posJup, velJup) = OrbitalElements(SharedData.SolarMass, 5.2 * SharedData.AU, 4.9e-2, 2.74e2, 2.61e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posJup.X, posJup.Y, SharedData.JupiterMass, velJup, Color.Beige, "Jupiter"));

            //////////////////////////////////////////
            /// Saturn System
            var (posSat, velSat) = OrbitalElements(SharedData.SolarMass, 9.535 * SharedData.AU, 5.370e-2, 3.385e2, 6.266e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posSat.X, posSat.Y, SharedData.SaturnMass, velSat, Color.BurlyWood, "Saturn", true));

            //////////////////////////////////////////

            var (posUra, velUra) = OrbitalElements(SharedData.SolarMass, 1.922e1 * SharedData.AU, 4.572e-2, 9.892e1, 1.753e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posUra.X, posUra.Y, SharedData.UranusMass, velUra, Color.Cyan, "Uranus", false, false, true));

            //////////////////////////////////////////
            /// Neptune System
            var (posNep, velNep) = OrbitalElements(SharedData.SolarMass, 3.018e1 * SharedData.AU, 8.711e-3, 2.463e2, 3.036e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posNep.X, posNep.Y, SharedData.NeptuneMass, velNep, Color.Blue, "Neptune", false, false, false, true));

            //////////////////////////////////////////
            /// Pluto System
            var (posPlu, velPlu) = OrbitalElements(SharedData.SolarMass, 4.014e1 * SharedData.AU, 2.577e-1, 1.162e2, 4.154e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posPlu.X, posPlu.Y, SharedData.PlutoMass, velPlu, Color.DarkOrange, "Pluto"));
        }

        public static void SpawnEarthSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.EarthMass, new Vector(), Color.SkyBlue, "Earth"));

            var (posMoo, velMoo) = OrbitalElements(SharedData.EarthMass, 2.567e-3 * SharedData.AU, 3.47e-2, 9.81e1, 4.317e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMoo.X, posMoo.Y, SharedData.MoonMass, velMoo, Color.LightGray, "Moon"));
        }

        public static void SpawnMarsSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.MarsMass, new Vector(), Color.Red, "Mars"));

            var (posPho, velPho) = OrbitalElements(SharedData.MarsMass, 6.27e-5 * SharedData.AU, 1.5e-2, 2.01e2, 1.22e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posPho.X, posPho.Y, SharedData.PhobosMass, velPho, Color.Orange, "Phobos"));

            var (posDei, velDei) = OrbitalElements(SharedData.MarsMass, 1.57e-4 * SharedData.AU, 3.17e-4, 2.58e2, 3.045e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posDei.X, posDei.Y, SharedData.DeimosMass, velDei, Color.DarkOrange, "Deimos"));
        }

        public static void SpawnJupiterSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.JupiterMass, new Vector(), Color.Beige, "Jupiter"));

            var (posIo, velIo) = OrbitalElements(SharedData.JupiterMass, 2.821e-3 * SharedData.AU, 3.555e-3, 3.246e1, 1.89e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posIo.X, posIo.Y, SharedData.IoMass, velIo, Color.GreenYellow, "Io"));

            var (posEur, velEur) = OrbitalElements(SharedData.JupiterMass, 4.487e-3 * SharedData.AU, 9.418e-3, 2.046e2, 9.077e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEur.X, posEur.Y, SharedData.EuropaMass, velEur, Color.Wheat, "Europa"));

            var (posGan, velGan) = OrbitalElements(SharedData.JupiterMass, 7.158e-3 * SharedData.AU, 2.157e-3, 3.206e2, 2.955e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posGan.X, posGan.Y, SharedData.GanymedeMass, velGan, Color.LightGray, "Ganymede"));

            var (posCal, velCal) = OrbitalElements(SharedData.JupiterMass, 1.258e-2 * SharedData.AU, 7.112e-3, 1.945e1, 5.847e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posCal.X, posCal.Y, SharedData.CallistoMass, velCal, Color.LightYellow, "Callisto"));
        }

        public static void SpawnSaturnSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.SaturnMass, new Vector(), Color.BurlyWood, "Saturn", true));

            var (posEnc, velEnc) = OrbitalElements(SharedData.SaturnMass, 1.593e-3 * SharedData.AU, 3.524e-3, 6.731e1, 1.374e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posEnc.X, posEnc.Y, SharedData.EnceladusMass, velEnc, Color.LightCyan, "Enceladus"));

            var (posTit, velTit) = OrbitalElements(SharedData.SaturnMass, 8.168e-3 * SharedData.AU, 2.875e-2, 1.684e2, 2.389e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posTit.X, posTit.Y, SharedData.TitanMass, velTit, Color.LightYellow, "Titan"));

            var (posMim, velMim) = OrbitalElements(SharedData.SaturnMass, 1.243e-3 * SharedData.AU, 1.914e-2, 1.885e2, 2.585e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMim.X, posMim.Y, SharedData.MimasMass, velMim, Color.LightGray, "Mimas"));

            var (posIap, velIap) = OrbitalElements(SharedData.SaturnMass, 2.378e-2 * SharedData.AU, 2.803e-2, 2.287e2, 4.879, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posIap.X, posIap.Y, SharedData.IapetusMass, velIap, Color.SandyBrown, "Iapetus"));

            var (posRhe, velRhe) = OrbitalElements(SharedData.SaturnMass, 3.524e-3 * SharedData.AU, 1.102e-3, 1.721e2, 3.201e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posRhe.X, posRhe.Y, SharedData.RheaMass, velRhe, Color.LightSlateGray, "Rhea"));

            var (posDio, velDio) = OrbitalElements(SharedData.SaturnMass, 2.524e-3 * SharedData.AU, 2.557e-3, 8.053e1, 4.963e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posDio.X, posDio.Y, SharedData.DioneMass, velDio, Color.SlateGray, "Dione"));
        }

        public static void SpawnUranusSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.UranusMass, new Vector(), Color.Cyan, "Uranus", false, false, true));

            var (posTita, velTita) = OrbitalElements(SharedData.UranusMass, 2.916e-3 * SharedData.AU, 2.449e-3, 2.541e2, 5.564e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posTita.X, posTita.Y, SharedData.TitaniaMass, velTita, Color.LightSlateGray, "Titania"));

            var (posObe, velObe) = OrbitalElements(SharedData.UranusMass, 3.900e-3 * SharedData.AU, 1.910e-3, 1.544e2, 3.605e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posObe.X, posObe.Y, SharedData.OberonMass, velObe, Color.LightSalmon, "Oberon"));

            var (posUmb, velUmb) = OrbitalElements(SharedData.UranusMass, 1.777e-3 * SharedData.AU, 3.290e-3, 5.023, 7.865e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posUmb.X, posUmb.Y, SharedData.UmbrielMass, velUmb, Color.DarkGray, "Umbriel"));

            var (posAri, velAri) = OrbitalElements(SharedData.UranusMass, 1.276e-3 * SharedData.AU, 1.163e-3, 8.662e1, 1.819e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posAri.X, posAri.Y, SharedData.ArielMass, velAri, Color.Gray, "Ariel"));

            var (posMir, velMir) = OrbitalElements(SharedData.UranusMass, 8.682e-4 * SharedData.AU, 1.288e-3, 5.019e1, 2.164e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posMir.X, posMir.Y, SharedData.MirandaMass, velMir, Color.LightGray, "Miranda"));
        }

        public static void SpawnNeptuneSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.NeptuneMass, new Vector(), Color.Blue, "Neptune", false, false, false, true));

            var (posTri, velTri) = OrbitalElements(SharedData.NeptuneMass, 2.371e-3 * SharedData.AU, 2.777e-5, 2.060e2, 3.337e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posTri.X, posTri.Y, SharedData.TritonMass, velTri, Color.LightGray, "Triton"));

            var (posPro, velPro) = OrbitalElements(SharedData.NeptuneMass, 7.866e-4 * SharedData.AU, 7.078e-4, 2.160e2, 3.476e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posPro.X, posPro.Y, SharedData.ProteusMass, velPro, Color.Gray, "Proteus"));

            var (posNer, velNer) = OrbitalElements(SharedData.NeptuneMass, 3.688e-2 * SharedData.AU, 7.471e-1, 2.973e2, 2.166e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posNer.X, posNer.Y, SharedData.NereidMass, velNer, Color.DarkGray, "Nereid"));
        }
        public static void SpawnPlutoSystem()
        {
            SharedData.bodies.Clear();

            SharedData.bodies.Add(SharedData.CreateBody(0, 0, SharedData.PlutoMass, new Vector(), Color.DarkOrange, "Pluto"));

            var (posCha, velCha) = OrbitalElements(SharedData.PlutoMass, 1.309e-4 * SharedData.AU, 1.605e-4, 1.726e2, 1.171e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posCha.X, posCha.Y, SharedData.CharonMass, velCha, Color.LightGray, "Charon"));

            var (posNix, velNix) = OrbitalElements(SharedData.PlutoMass, 8.480e-4 * SharedData.AU, 5.989e-1, 2.838e2, 3.562e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posNix.X, posNix.Y, SharedData.NixMass, velNix, Color.DarkSlateGray, "Nix"));

            var (posHyd, velHyd) = OrbitalElements(SharedData.PlutoMass, 3.614e-4 * SharedData.AU, 2.062e-1, 3.038e2, 2.110e2, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posHyd.X, posHyd.Y, SharedData.HydraMass, velHyd, Color.SlateGray, "Hydra"));

            var (posKer, velKer) = OrbitalElements(SharedData.PlutoMass, 3.967e-4 * SharedData.AU, 1.505e-1, 3.119e2, 8.317e1, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posKer.X, posKer.Y, SharedData.KerberosMass, velKer, Color.DarkGray, "Kerberos"));

            var (posSty, velSty) = OrbitalElements(SharedData.PlutoMass, 6.711e-4 * SharedData.AU, 5.566e-1, 3.011e2, 8.015, new Vector(), new Vector());
            SharedData.bodies.Add(SharedData.CreateBody(posSty.X, posSty.Y, SharedData.StyxMass, velSty, Color.LightGray, "Styx"));
        }
    }
}
