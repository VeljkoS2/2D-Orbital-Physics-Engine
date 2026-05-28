using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2D_Orbital_Physics_Engine
{
    public static class BodyPresets
    {
        public static Celestial_Body SpawnSun(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.SolarMass, vel, "Sun");
        }
        public static Celestial_Body SpawnMercury(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.MercuryMass, vel, Color.Gray, "Mercury");
        }
        public static Celestial_Body SpawnVenus(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.VenusMass, vel, Color.Yellow, "Venus");
        }
        public static Celestial_Body SpawnEarth(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.EarthMass, vel, Color.SkyBlue, "Earth");
        }
        public static Celestial_Body SpawnMoon(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.MoonMass, vel, Color.LightGray, "Moon");
        }
        public static Celestial_Body SpawnMars(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.MarsMass, vel, Color.Red, "Mars");
        }
        public static Celestial_Body SpawnJupiter(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.JupiterMass, vel, Color.Beige, "Jupiter");
        }
        public static Celestial_Body SpawnSaturn(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.SaturnMass, vel, Color.BurlyWood, "Saturn");
        }
        public static Celestial_Body SpawnUranus(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.UranusMass, vel, Color.Cyan, "Uranus");
        }
        public static Celestial_Body SpawnNeptune(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.NeptuneMass, vel, Color.Blue, "Neptune");
        }
        public static Celestial_Body SpawnPluto(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.PlutoMass, vel, Color.DarkOrange, "Pluto");
        }
        public static Celestial_Body SpawnSagittariusA(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.SagittariusAMass, vel, "Sagittarius A");
        }
        public static Celestial_Body SpawnSpaceship(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.SpaceshipMass, vel, "Spaceship");
        }
        public static Celestial_Body SpawnViltrum(Vector pos, Vector vel)
        {
            return SharedData.CreateBody(pos.X, pos.Y, SharedData.ViltrumMass, vel, Color.DarkCyan, "Viltrum");
        }
    }
}
