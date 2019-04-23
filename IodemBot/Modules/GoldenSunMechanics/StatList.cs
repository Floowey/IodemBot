using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class StatList
    {
        private static Dictionary<string, Stats> stats;
        private static Stats baseStats = new Stats(35, 20, 20, 6, 8); //30, 20, 11, 6, 8

        static StatList()
        {
            string json = File.ReadAllText("Resources/stats.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            stats = data.ToObject<Dictionary<string, Stats>>();
        }
    }

    public struct ElementalStats
    {
        public uint VenusAtk { get; set; }
        public uint VenusRes { get; set; }
        public uint MarsAtk { get; set; }
        public uint MarsRes { get; set; }
        public uint JupiterAtk { get; set; }
        public uint JupiterRes { get; set; }
        public uint MercuryAtk { get; set; }
        public uint MercuryRes { get; set; }

        public ElementalStats(uint venusAtk, uint venusRes, uint marsAtk, uint marsDef, uint jupiterAtk, uint jupiterDef, uint mercuryAtk, uint mercuryDef) : this()
        {
            VenusAtk = venusAtk;
            VenusRes = venusRes;
            MarsAtk = marsAtk;
            MarsRes = marsDef;
            JupiterAtk = jupiterAtk;
            JupiterRes = jupiterDef;
            MercuryAtk = mercuryAtk;
            MercuryRes = mercuryDef;
        }

        public override string ToString()
        {
            return $"`VnPow: {VenusAtk} MrPow: {MarsAtk} JpPow: {JupiterAtk} McPow: {MercuryAtk}`\n" +
                $"`VnRes: {VenusRes} MrRes: {MarsRes} JpRes: {JupiterRes} McRes: {MercuryRes}`";
        }

        internal uint leastRes()
        {
            return (new[] { VenusRes, MarsRes, JupiterRes, MercuryRes }).Min();
        }

        internal uint highestRes()
        {
            return (new[] { VenusRes, MarsRes, JupiterRes, MercuryRes }).Max();
        }

        internal uint GetPower(Element e)
        {
            switch (e)
            {
                case Element.Venus: return VenusAtk;
                case Element.Mars: return MarsAtk;
                case Element.Jupiter: return JupiterAtk;
                case Element.Mercury: return MercuryAtk;
                default: return 100;
            }
        }

        internal uint GetRes(Element e)
        {
            switch (e)
            {
                case Element.Venus: return VenusRes;
                case Element.Mars: return MarsRes;
                case Element.Jupiter: return JupiterRes;
                case Element.Mercury: return MercuryRes;
                default: return 100;
            }
        }
    }

    public class Stats
    {
        public uint maxHP { get; set; }
        [JsonIgnore] public uint HP { get; set; }
        public uint maxPP { get; set; }
        [JsonIgnore] public uint PP { get; set; }
        public uint Atk { get; set; }
        public uint Def { get; set; }
        public uint Spd { get; set; }

        public Stats(uint maxHP, uint maxPP, uint atk, uint def, uint spd)
        {
            this.maxHP = maxHP;
            this.maxPP = maxPP;
            HP = maxHP;
            PP = maxPP;
            Atk = atk;
            Def = def;
            Spd = spd;
        }

        public static Stats operator *(Stats s1, double factor)
        {
            return new Stats((uint)(s1.maxHP * factor), (uint)(s1.maxPP * factor), (uint)(s1.Atk * factor), (uint)(s1.Def * factor), (uint)(s1.Spd * factor));
        }

        public override string ToString()
        {
            return $"`HP: {maxHP} Atk: {Atk} Agi: {Spd}`\n` PP: {maxPP} Def: {Def} `";
        }
    }
}