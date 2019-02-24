using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class StatList
    {
        private static Dictionary<string, Stats> stats;
        private static Stats baseStats = new Stats(35, 20, 20, 6, 8); //30, 20, 11, 6, 8

        //get Moveset based on Classname
        public static Stats getStats(string className, uint level)
        {
            var multipliers = new Stats(100, 100, 100, 100, 100);
            if (stats.ContainsKey(className))
            {
                multipliers = stats[className];
            }

            var actualStats = new Stats(
                (uint)(baseStats.maxHP * multipliers.maxHP / 100 * Math.Sqrt(1 + level / 2)),
                (uint)(baseStats.maxPP * multipliers.maxPP / 100 * Math.Sqrt(1 + level / 2)),
                (uint)(baseStats.Atk * multipliers.Atk / 100 * Math.Sqrt(1 + level / 2)),
                (uint)(baseStats.Def * multipliers.Def / 100 * Math.Sqrt(1 + level / 2)),
                (uint)(baseStats.Spd * multipliers.Spd / 100 * Math.Sqrt(1 + level / 2)));
            return actualStats;   
        }

        static StatList() {
            string json = File.ReadAllText("Resources/stats.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            stats = data.ToObject<Dictionary<string, Stats>>();
        }

        public static void Save()
        {
            stats = new Dictionary<string, Stats>();
            stats.Add("Squire", new Stats(110, 80, 110, 100, 110));
            stats.Add("Knight", new Stats(130, 90, 120, 110, 120));

            string data = JsonConvert.SerializeObject(stats, Formatting.Indented);
            File.WriteAllText("Resources/stats.json", data);
        }

        public static ElementalStats getElementalStats(Element element)
        {
            ElementalStats elstats = new ElementalStats(100, 100, 100, 100, 100, 100, 100, 100);
            switch (element)
            {
                case (Element.Venus):
                    elstats.VenusAtk = 140;
                    elstats.VenusRes = 140;
                    elstats.MarsRes = 70;
                    break;
                case (Element.Mars):
                    elstats.MarsAtk = 140;
                    elstats.MarsRes = 140;
                    elstats.MercuryRes = 70;
                    break;
                case (Element.Jupiter):
                    elstats.JupiterAtk = 140;
                    elstats.JupiterRes = 140;
                    elstats.VenusRes = 70;
                    break;
                case (Element.Mercury):
                    elstats.MercuryAtk = 140;
                    elstats.MercuryRes = 140;
                    elstats.JupiterRes = 70;
                    break;
            }
            return elstats;
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

        internal uint leastRes()
        {
            return (new[] { VenusRes, MarsRes, JupiterRes, MercuryRes}).Min();
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

        public override string ToString()
        {
            return $"`HP: {maxHP} Atk: {Atk} Spd: {Spd}`\n` PP: {maxPP} Def: {Def} `";
        }
    }
}
