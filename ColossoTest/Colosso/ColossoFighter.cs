using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public abstract class ColossoFighter : IComparable<ColossoFighter>
    {
        public ColossoBattle battle;
        public ColossoBattle.Team party;
        public ColossoBattle.Team enemies;
        public string name;
        public Stats stats;
        public ElementalStats elstats;
        public Move[] moves;

        public List<Buff> buffs = new List<Buff>();

        public Move selected; //change to int maybe?
        public bool hasSelected = false;

        //public Team team;
        //public Team enemies;
        //public int Target;

        //Move gets Property Target, which is an enum Target {Self, OwnSingle, OwnAll, OtherSingle, OtherSpread}
        //Move.use() gets called by passing ColossoFighter User, int targetPos, Target gets selected depending on the denum
        //That way PvP is possible, Team.dealDamage, Team.heal are possible and Spread Moves work.
        //Also make ColossoBattle non-static


        protected ColossoFighter(string name, Stats stats, ElementalStats elstats, Move[] moves)
        {
            this.name = name;
            this.stats = stats;
            this.elstats = elstats;
            this.moves = moves;
        }

        public bool IsAlive()
        {
            return stats.HP > 0;
        }

        public abstract List<string> dealDamage(uint damage);
        public void heal(uint healHP)
        {
            if (stats.HP > 0)
            {
                stats.HP = Math.Min(stats.HP + healHP, stats.maxHP);
            }
        }
        public void revive(uint percentage)
        {
            if (stats.HP == 0)
            {
                stats.HP = stats.maxHP * percentage / 100;
            }
        }

        public void applyBuff(Buff buff)
        {
            buffs.Add(buff);
        }

        public double MultiplyBuffs(string stat)
        {
            var mult = buffs.Where(b => b.stat.Equals(stat) && b.multiplier > 0).Aggregate(1.0, (p, s) => p *= s.multiplier);
            return mult;
        }

        public abstract void StartTurn();

        public List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            if (!IsAlive())
            {
                return turnLog;
            }

            if (selected.name != "Defend")
            {
                turnLog.AddRange(selected.Use(this));
            } else
            {
                turnLog.Add($"{this.name} defends.");
            }

            //turnLog.Add("\n");
            return turnLog;
        }

        public abstract void EndTurn();

        public bool select(string emote)
        {
            string[] numberEmotes = new string[] {"\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };
            var trySelected = moves.Where(m => m.emote == emote).FirstOrDefault();
            if (trySelected == null)
            {
                if (numberEmotes.Contains(emote) && selected != null)
                {
                    selected.targetNr = Array.IndexOf(numberEmotes, emote)-1;
                    hasSelected = true;
                } else
                {
                    return false;
                }
            } else
            {
                selected = trySelected;
            }

            if (selected.targetType == Target.self || selected.targetType == Target.ownAll ||selected.targetType == Target.otherAll)
            {
                hasSelected = true;
            }

            return true;
        }

        public void select(int targetNr)
        {
            selected.targetNr = targetNr;
        }

        public void selectRandom()
        {
            var s = (uint)(new Random()).Next(0, moves.Count());
            selected = moves[s];
            selected.targetNr = 0;
            if (selected is OffensivePsynergy || selected is Attack){
                selected.targetNr = (new Random()).Next(0, battle.getTeam(enemies).Count());
            }
            hasSelected = true;
            //select(s, t);
        }

        public int CompareTo(ColossoFighter obj)
        {
            if (obj == null) return 1;

            if (stats.Spd > obj.stats.Spd) return 1;
            if (stats.Spd == obj.stats.Spd) return 0;
            return -1;
        }
    }

    public struct Buff
    {
        public string stat;
        public double multiplier;
        public uint turns;

        public Buff(string stat, double multiplier, uint turns)
        {
            this.stat = stat;
            this.multiplier = multiplier;
            this.turns = turns;
        }
    }
}
