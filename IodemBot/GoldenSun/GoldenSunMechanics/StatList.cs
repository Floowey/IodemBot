using System;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace IodemBot.Modules.GoldenSunMechanics
{
    public struct ElementalStats
    {
        public int VenusAtk { get; set; }
        public int VenusRes { get; set; }
        public int MarsAtk { get; set; }
        public int MarsRes { get; set; }
        public int JupiterAtk { get; set; }
        public int JupiterRes { get; set; }
        public int MercuryAtk { get; set; }
        public int MercuryRes { get; set; }

        [JsonConstructor]
        public ElementalStats(int venusAtk = 100, int venusRes = 100, int marsAtk = 100, int marsDef = 100,
            int jupiterAtk = 100, int jupiterDef = 100, int mercuryAtk = 100, int mercuryDef = 100) : this()
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

        public ElementalStats(ElementalStats newStats)
        {
            VenusAtk = newStats.VenusAtk;
            VenusRes = newStats.VenusRes;
            MarsAtk = newStats.MarsAtk;
            MarsRes = newStats.MarsRes;
            JupiterAtk = newStats.JupiterAtk;
            JupiterRes = newStats.JupiterRes;
            MercuryAtk = newStats.MercuryAtk;
            MercuryRes = newStats.MercuryRes;
        }

        public static ElementalStats operator +(ElementalStats s1, ElementalStats s2)
        {
            return new ElementalStats(s1.VenusAtk + s2.VenusAtk, s1.VenusRes + s2.VenusRes, s1.MarsAtk + s2.MarsAtk,
                s1.MarsRes + s2.MarsRes, s1.JupiterAtk + s2.JupiterAtk, s1.JupiterRes + s2.JupiterRes,
                s1.MercuryAtk + s2.MercuryAtk, s1.MercuryRes + s2.MercuryRes);
        }

        public static ElementalStats operator *(ElementalStats s, float k)
        {
            return new ElementalStats((int)(s.VenusAtk * k), (int)(s.VenusRes * k), (int)(s.MarsAtk * k),
                (int)(s.MarsRes * k), (int)(s.JupiterAtk * k), (int)(s.JupiterRes * k), (int)(s.MercuryAtk * k),
                (int)(s.MercuryRes * k));
        }

        public override string ToString()
        {
            return (
                    $"{(VenusAtk != 0 || VenusRes != 0 ? $"{Emotes.GetIcon(Element.Venus)} `{VenusAtk} | {VenusRes}` " : "")}" +
                    $"{(MercuryAtk != 0 || MercuryRes != 0 ? $"{Emotes.GetIcon(Element.Mercury)} `{MercuryAtk} | {MercuryRes}` " : "")}" +
                    "\n" +
                    $"{(MarsAtk != 0 || MarsRes != 0 ? $"{Emotes.GetIcon(Element.Mars)} `{MarsAtk} | {MarsRes}` " : "")}" +
                    $"{(JupiterAtk != 0 || JupiterRes != 0 ? $"{Emotes.GetIcon(Element.Jupiter)} `{JupiterAtk} | {JupiterRes}` " : "")}")
                .Trim();
            //return $"`VnPow: {VenusAtk} MrPow: {MarsAtk} JpPow: {JupiterAtk} McPow: {MercuryAtk}`\n" +
            //    $"`VnRes: {VenusRes} MrRes: {MarsRes} JpRes: {JupiterRes} McRes: {MercuryRes}`";
        }

        public string NonZerosToString()
        {
            return (
                    $"{(VenusAtk != 0 || VenusRes != 0 ? $"{Emotes.GetIcon(Element.Venus)} `{VenusAtk} | {VenusRes}` " : "")}" +
                    $"{(MarsAtk != 0 || MarsRes != 0 ? $"{Emotes.GetIcon(Element.Mars)} `{MarsAtk} | {MarsRes}` " : "")}" +
                    $"{(JupiterAtk != 0 || JupiterRes != 0 ? $"{Emotes.GetIcon(Element.Jupiter)} `{JupiterAtk} | {JupiterRes}` " : "")}" +
                    $"{(MercuryAtk != 0 || MercuryRes != 0 ? $"{Emotes.GetIcon(Element.Mercury)} `{MercuryAtk} | {MercuryRes}` " : "")}")
                .Trim();
        }

        internal int LowestRes()
        {
            return new[] { VenusRes, MarsRes, JupiterRes, MercuryRes }.Min();
        }

        internal int HighestRes()
        {
            return new[] { VenusRes, MarsRes, JupiterRes, MercuryRes }.Max();
        }

        internal int GetPower(Element e)
        {
            return e switch
            {
                Element.Venus => VenusAtk,
                Element.Mars => MarsAtk,
                Element.Jupiter => JupiterAtk,
                Element.Mercury => MercuryAtk,
                _ => 100
            };
        }

        internal int GetRes(Element e)
        {
            return e switch
            {
                Element.Venus => VenusRes,
                Element.Mars => MarsRes,
                Element.Jupiter => JupiterRes,
                Element.Mercury => MercuryRes,
                _ => 100
            };
        }
    }

    public class Stats
    {
        [JsonConstructor]
        public Stats(int maxHp = 10, int maxPp = 10, int atk = 10, int def = 10, int spd = 10)
        {
            MaxHP = maxHp;
            MaxPP = maxPp;
            HP = maxHp;
            PP = maxPp;
            Atk = atk;
            Def = def;
            Spd = spd;
        }

        public Stats(Stats newStats)
        {
            MaxHP = newStats.MaxHP;
            MaxPP = newStats.MaxPP;
            HP = newStats.HP;
            PP = newStats.PP;
            Atk = newStats.Atk;
            Def = newStats.Def;
            Spd = newStats.Spd;
        }

        public int MaxHP { get; set; }
        [JsonIgnore] public int HP { get; set; }
        public int MaxPP { get; set; }
        [JsonIgnore] public int PP { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }

        public static Stats operator *(Stats s1, double factor)
        {
            return new Stats((int)(s1.MaxHP * factor), (int)(s1.MaxPP * factor), (int)(s1.Atk * factor),
                (int)(s1.Def * factor), (int)(s1.Spd * factor));
        }

        public static Stats operator *(Stats s1, Stats s2)
        {
            return new Stats(s1.MaxHP * s2.MaxHP, s1.MaxPP * s2.MaxPP, s1.Atk * s2.Atk, s1.Def * s2.Def,
                s1.Spd * s2.Spd);
        }

        public static Stats operator +(Stats s1, Stats s2)
        {
            return new Stats(s1.MaxHP + s2.MaxHP, s1.MaxPP + s2.MaxPP, s1.Atk + s2.Atk, s1.Def + s2.Def,
                s1.Spd + s2.Spd);
        }

        public static Stats operator -(Stats s1, Stats s2)
        {
            return new Stats(Math.Max(0, s1.MaxHP - s2.MaxHP), Math.Max(0, s1.MaxPP - s2.MaxPP),
                Math.Max(0, s1.Atk - s2.Atk), Math.Max(0, s1.Def - s2.Def), Math.Max(0, s1.Spd - s2.Spd));
        }

        public override string ToString()
        {
            return $"`{$"HP: {MaxHP} Atk: {Atk} Agi: {Spd}`\n`PP: {MaxPP} Def: {Def}".Trim()}`";
        }

        public string NonZerosToString()
        {
            return "`" + ($"{(MaxHP != 0 ? $"HP: {MaxHP} " : "")}" +
                          $"{(MaxPP != 0 ? $" PP: {MaxPP}" : "")}" +
                          $"{(Atk != 0 ? $" Atk: {Atk}" : "")}" +
                          $"{(Def != 0 ? $" Def: {Def}" : "")}" +
                          $"{(Spd != 0 ? $" Agi: {Spd}" : "")}").Trim() + "`";
        }

        public string MultipliersToString()
        {
            return "`" + $"{(MaxHP != 100 ? $"HP: x{(double)MaxHP / 100} " : "")}" +
                   $"{(MaxPP != 100 ? $"PP: x{(double)MaxPP / 100} " : "")}" +
                   $"{(Atk != 100 ? $"Atk: x{(double)Atk / 100} " : "")}" +
                   $"{(Def != 100 ? $"Def: x{(double)Def / 100} " : "")}" +
                   $"{(Spd != 100 ? $"Agi: x{(double)Spd / 100}" : "")}".Trim() + "`";
        }
    }
}