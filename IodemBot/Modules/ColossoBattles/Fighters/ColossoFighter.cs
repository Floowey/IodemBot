using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public enum Condition { Down, Poison, Venom, Seal, Stun, DeathCurse, Haunt, ItemCurse, Flinch, Delusion, Sleep}
    public abstract class ColossoFighter : IComparable<ColossoFighter>, ICloneable
    {
        [JsonIgnore] public ColossoBattle battle;
        [JsonIgnore] public ColossoBattle.Team party;
        [JsonIgnore] public ColossoBattle.Team enemies;
        public string name;
        public string imgUrl;
        public Stats stats;
        public ElementalStats elstats;
        public Move[] moves;
        [JsonIgnore] private Random rnd = Global.random;
        [JsonIgnore] private readonly List<Condition> Conditions = new List<Condition>();

        [JsonIgnore] public List<Buff> Buffs = new List<Buff>();

        [JsonIgnore] public Move selected;
        [JsonIgnore] public bool hasSelected = false;

        internal ColossoFighter(string name, string imgUrl, Stats stats, ElementalStats elstats, Move[] moves)
        {
            this.name = name;
            this.imgUrl = imgUrl;
            this.stats = stats;
            this.stats.HP = stats.maxHP;
            this.stats.PP = stats.maxPP;
            this.elstats = elstats;
            this.moves = moves;
        }

        public bool IsAlive()
        {
            return !Conditions.Contains(Condition.Down);
        }

        public string getMoves()
        {
            var relevantMoves = moves.Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy) m).ConvertAll(p => $"{p.emote} {p.name} `{p.PPCost}`");
            return string.Join(" - ", relevantMoves);
        }

        public virtual List<string> dealDamage(uint damage, string punctuation = "!")
        {
            var log = new List<string>
            {
                $"{name} takes {damage} damage{punctuation}"
            };
            if (damage < stats.HP)
            {
                stats.HP -= damage;
            }
            else
            {
                stats.HP = 0;
                log.Add($":x: {name} goes down.");
                AddCondition(Condition.Down);
            }
            return log;
        }

        public void AddCondition(Condition con)
        {
            if (!Conditions.Contains(con))
            {
                Conditions.Add(con);
            }
        }

        public bool HasCondition(Condition con)
        {
            return Conditions.Contains(con);
        }

        public void RemoveAllConditions()
        {
            Conditions.RemoveAll(c => c != Condition.Down && c != Condition.ItemCurse && c!= Condition.Haunt);
        }

        public void RemoveCondition(Condition con)
        {
            if (Conditions.Contains(con))
            {
                Conditions.Remove(con);
            }
        }

        public List<string> heal(uint healHP)
        {
            List<string> log = new List<string>();
            if (stats.HP > 0)
            {
                stats.HP = Math.Min(stats.HP + healHP, stats.maxHP);
                if (stats.HP == stats.maxHP)
                {
                    log.Add($"{name}'s HP was fully restored!");
                }
                else
                {
                    log.Add($"{name} recovers {healHP} HP!");
                }
            } else
            {
                log.Add($"{name} is unaffected");
            }
            return log;
            
        }
        public List<string> revive(uint percentage)
        {
            List<string> log = new List<string>();
            if (!IsAlive())
            {
                stats.HP = stats.maxHP * percentage / 100;
                log.Add($"{name} is back on their feet.");
                RemoveCondition(Condition.Down);
            } else
            {
                log.Add($"{name} is unaffected");
            }
            return log;
        }

        public void applyBuff(Buff buff)
        {
            Buffs.Add(buff);
        }

        public double MultiplyBuffs(string stat)
        {
            var mult = Buffs.Where(b => b.stat.Equals(stat, StringComparison.InvariantCultureIgnoreCase) && b.multiplier > 0).Aggregate(1.0, (p, s) => p *= s.multiplier);
            //Console.WriteLine($"{name} {buffs.Count}  {mult}");
            if (mult > 2.0) mult = 2.0;
            if (mult < 0.4) mult = 0.4;
            return mult;
        }

        public virtual void StartTurn() {
            if (selected is Defend)
            {
                var a = selected.Use(this);
            }
        }

        public List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            if (!IsAlive())
            {
                return turnLog;
            }

            if (!(selected is Defend))
            {
                turnLog.AddRange(selected.Use(this));
            } else
            {
                turnLog.Add($"<:Defend:536919830507552768> {this.name} defends.");
            }
            return turnLog;
        }

        public string ConditionsToString()
        {
            StringBuilder s = new StringBuilder();
            if (HasCondition(Condition.DeathCurse)) s.Append("");
            if (HasCondition(Condition.Delusion)) s.Append("");
            if (HasCondition(Condition.Down)) s.Append("<:curse:538074679492083742>");
            if (HasCondition(Condition.Flinch)) s.Append("");
            if (HasCondition(Condition.Haunt)) s.Append("");
            if (HasCondition(Condition.ItemCurse)) s.Append("");
            if (HasCondition(Condition.Poison)) s.Append("");
            if (HasCondition(Condition.Seal)) s.Append("");
            if (HasCondition(Condition.Stun)) s.Append("");
            if (HasCondition(Condition.Venom)) s.Append("");
            return s.ToString();
        }

        public virtual void EndTurn() {
            List<string> turnLog = new List<string>();

            var newBuffs = new List<Buff>();
            Buffs.ForEach(s => {
                s.turns -= 1;
                if (s.turns >= 1)
                {
                    newBuffs.Add(s);
                } else
                {
                    turnLog.Add($"{name}'s {s.stat} normalizes.");
                }
            });
            Buffs = newBuffs;
            if (!IsAlive())
            {
                selected = new Nothing();
                hasSelected = true;
            }
        }

        public bool select(string emote)
        {
            string[] numberEmotes = new string[] {"\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };
            var trySelected = moves.Where(m => m.emote == emote).FirstOrDefault() ?? moves.Where(m => m.emote.Contains(emote)).FirstOrDefault();
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
            if (!battle.isActive)
            {
                Console.WriteLine("Why tf do you want to selectRandom(), the battle is *not* active!");
                return;
            }
            var s = (uint)rnd.Next(0, moves.Count());
            selected = moves[s];
            selected.targetNr = 0;
            if(selected is Psynergy)
            {
                if (stats.PP < ((Psynergy)selected).PPCost)
                {
                    Console.WriteLine("Not enough PP. Picking new Move.");
                    selectRandom();
                }
            }
            if (selected is OffensivePsynergy || selected is Attack){
                selected.targetNr = rnd.Next(0, battle.getTeam(enemies).Count());
                if(!battle.getTeam(enemies)[selected.targetNr].IsAlive())
                {
                    Console.WriteLine("Target not alive. Retargeting.");
                    selected.targetNr = rnd.Next(0, battle.getTeam(enemies).Count());
                }
            } else
            {
                selected.targetNr = rnd.Next(0, battle.getTeam(party).Count());
            }
            hasSelected = true;
            //select(s, t);
        }

        public int CompareTo(ColossoFighter obj)
        {
            if (obj == null) return 1;
            if (name == obj.name) return 0;

            if (stats.Spd > obj.stats.Spd) return 1;
            if (stats.Spd == obj.stats.Spd) return 0;
            return -1;
        }

        public object Clone()
        {
            return MemberwiseClone();
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
