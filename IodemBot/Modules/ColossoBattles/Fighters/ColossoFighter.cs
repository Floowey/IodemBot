using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IodemBot.Modules.ColossoBattles
{
    public enum Condition { Down, Poison, Venom, Seal, Stun, DeathCurse, Haunt, ItemCurse, Flinch, Delusion, Sleep, Counter }

    public struct Buff
    {
        public double multiplier;
        public string stat;
        public uint turns;

        public Buff(string stat, double multiplier, uint turns)
        {
            this.stat = stat;
            this.multiplier = multiplier;
            this.turns = turns;
        }
    }

    public abstract class ColossoFighter : IComparable<ColossoFighter>, ICloneable
    {
        private static readonly Random rnd = Global.Random;

        public string name;
        public Stats stats;
        public ElementalStats elstats;
        public string imgUrl;
        [JsonIgnore] public Move[] moves;

        [JsonProperty("Conditions", ItemConverterType = typeof(StringEnumConverter))]
        private List<Condition> Conditions = new List<Condition>();

        [JsonProperty("isImmuneToConditions", ItemConverterType = typeof(StringEnumConverter))]
        public List<Condition> isImmuneToConditions = new List<Condition>();

        public bool IsImmuneToOHKO { get; set; }
        public bool IsImmuneToHPtoOne { get; set; }
        public bool IsImmuneToPsynergy { get; set; }
        public bool IsImmuneToItemCurse { get; set; }
        public Item Weapon;
        [JsonIgnore] public ColossoBattle.Team party;
        [JsonIgnore] public Move selected;
        [JsonIgnore] public uint damageDoneThisTurn;
        [JsonIgnore] public ColossoBattle battle;
        [JsonIgnore] public List<Buff> Buffs = new List<Buff>();
        [JsonIgnore] public ColossoBattle.Team enemies;
        [JsonIgnore] public bool hasSelected = false;
        [JsonIgnore] public double offensiveMult = 1;
        [JsonIgnore] public double defensiveMult = 1;
        [JsonIgnore] public double ignoreDefense = 1;
        public int unleashRate = 35;
        [JsonIgnore] public uint addDamage;
        public List<Item> EquipmentWithEffect = new List<Item>();
        public int HPrecovery { get; set; } = 0;
        public int PPrecovery { get; set; } = 0;
        public int DeathCurseCounter = 4;

        internal ColossoFighter(string name, string imgUrl, Stats stats, ElementalStats elstats, Move[] moves)
        {
            this.name = name;
            this.imgUrl = imgUrl;
            this.stats = stats;
            this.stats.HP = stats.MaxHP;
            this.stats.PP = stats.MaxPP;
            this.elstats = elstats;
            this.moves = moves;
        }

        public void AddCondition(Condition con)
        {
            if (!Conditions.Contains(con))
            {
                if (con == Condition.Venom && HasCondition(Condition.Poison))
                {
                    RemoveCondition(Condition.Poison);
                }

                Conditions.Add(con);
            }
        }

        public void ApplyBuff(Buff buff)
        {
            Buffs.Add(buff);
        }

        public abstract object Clone();

        public int CompareTo(ColossoFighter obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (name == obj.name)
            {
                return 0;
            }

            if (stats.Spd > obj.stats.Spd)
            {
                return 1;
            }

            if (stats.Spd == obj.stats.Spd)
            {
                return 0;
            }

            return -1;
        }

        public string ConditionsToString()
        {
            StringBuilder s = new StringBuilder();
            if (HasCondition(Condition.DeathCurse))
            {
                string[] DeathCurseEmotes = { ":grey_question:", "<:DeathCurse1:583645163499552791>", "<:DeathCurse2:583645163927109636>", "<:DeathCurse3:583644633314099202>", "<:DeathCurse2:583645163927109636><:DeathCurse2:583645163927109636>" };
                s.Append(DeathCurseEmotes[DeathCurseCounter]);
            }

            if (HasCondition(Condition.Delusion))
            {
                s.Append("<:delusion:549526931637534721>");
            }

            if (HasCondition(Condition.Down))
            {
                s.Append("<:curse:538074679492083742>");
            }

            if (HasCondition(Condition.Flinch))
            {
                s.Append("");
            }

            if (HasCondition(Condition.Haunt))
            {
                s.Append("<:Haunted:549526931821953034>");
            }

            if (HasCondition(Condition.ItemCurse))
            {
                s.Append("<:Condemn:583651784040644619>");
            }

            if (HasCondition(Condition.Poison))
            {
                s.Append("<:Poison:549526931847249920>");
            }

            if (HasCondition(Condition.Seal))
            {
                s.Append("<:Psy_Seal:549526931465568257>");
            }

            if (HasCondition(Condition.Sleep))
            {
                s.Append("<:Sleep:555427023519088671>");
            }

            if (HasCondition(Condition.Stun))
            {
                s.Append("<:Flash_Bolt:536966441862299678>");
            }

            if (HasCondition(Condition.Venom))
            {
                s.Append("<:Poison:549526931847249920>");
            }

            return s.ToString();
        }

        public virtual List<string> DealDamage(uint damage, string punctuation = "!")
        {
            var log = new List<string>
            {
                $"{name} takes {damage} damage{punctuation}"
            };
            if (!IsAlive())
            {
                log.Add("Someone tried to damage the dead. This shouldn't have happened... Please use i!bug and name the action that was performed");
                return log;
            }
            if (stats.HP > damage)
            {
                stats.HP -= (int)damage;
                if (this is PlayerFighter)
                {
                    ((PlayerFighter)this).battleStats.DamageTanked += damage;
                }
            }
            else
            {
                Kill();
                log.Add($":x: {name} goes down.");
            }
            return log;
        }

        public virtual List<string> EndTurn()
        {
            List<string> turnLog = new List<string>();

            var newBuffs = new List<Buff>();
            Buffs.ForEach(s =>
            {
                s.turns -= 1;
                if (s.turns >= 1)
                {
                    newBuffs.Add(s);
                }
                else
                {
                    turnLog.Add($"{name}'s {s.stat} normalizes.");
                }
            });
            Buffs = newBuffs;
            defensiveMult = 1;
            offensiveMult = 1;

            if (IsAlive())
            {
                if (HPrecovery > 0 && stats.HP < stats.MaxHP)
                {
                    turnLog.AddRange(Heal((uint)HPrecovery));
                }
                if (PPrecovery > 0 && stats.PP < stats.MaxPP)
                {
                    turnLog.AddRange(RestorePP((uint)PPrecovery));
                }
            }

            RemoveCondition(Condition.Flinch);

            //Chance to wake up
            if (HasCondition(Condition.Sleep))
            {
                if (Global.Random.Next(0, 2) == 0)
                {
                    RemoveCondition(Condition.Sleep);
                    turnLog.Add($"{name} wakes up.");
                }
            }
            //Chance to remove Stun
            if (HasCondition(Condition.Stun))
            {
                if (Global.Random.Next(0, 2) == 0)
                {
                    RemoveCondition(Condition.Stun);
                    turnLog.Add($"{name} can move again.");
                }
            }
            //Chance to remove Seal
            if (HasCondition(Condition.Seal))
            {
                if (Global.Random.Next(0, 3) == 0)
                {
                    RemoveCondition(Condition.Seal);
                    turnLog.Add($"{name}'s Psynergy is no longer sealed.");
                }
            }
            //Chance to remove Delusion
            if (HasCondition(Condition.Delusion))
            {
                if (Global.Random.Next(0, 4) == 0)
                {
                    RemoveCondition(Condition.Delusion);
                    turnLog.Add($"{name} can see clearly again.");
                }
            }

            //Poison Damage
            if (HasCondition(Condition.Poison))
            {
                var damage = Math.Min(200, (uint)(stats.MaxHP * Global.Random.Next(5, 10) / 100));
                turnLog.Add($"{name} is damaged by the Poison.");
                turnLog.AddRange(DealDamage(damage));
            }
            if (HasCondition(Condition.Venom))
            {
                var damage = Math.Min(400, (uint)(stats.MaxHP * Global.Random.Next(10, 20) / 100));
                turnLog.Add($"{name} is damaged by the Venom.");
                turnLog.AddRange(DealDamage(damage));
            }

            if (HasCondition(Condition.DeathCurse))
            {
                DeathCurseCounter--;
                if (DeathCurseCounter <= 0)
                {
                    Kill();
                    turnLog.Add($":x: {name}'s light goes out.");
                }
            }

            RemoveCondition(Condition.Counter);

            foreach (var item in EquipmentWithEffect)
            {
                if (item.IsUnleashable
                    && !item.IsBroken
                    && item.Unleash.Effects.Any(e => e.ValidSelection(this))
                    && Global.Random.Next(0, 100) <= item.ChanceToActivate)
                {
                    turnLog.Add($"{item.IconDisplay} {name}'s {item.Name} starts to Glow.");
                    foreach (var effect in item.Unleash.Effects)
                    {
                        turnLog.AddRange(effect.Apply(this, this));
                    }

                    if (Global.Random.Next(0, 100) <= item.ChanceToBreak)
                    {
                        item.IsBroken = true;
                        turnLog.Add($"{item.IconDisplay} {name}'s {item.Name} breaks;");
                    }
                }
            }
            damageDoneThisTurn = 0;
            if (!IsAlive())
            {
                selected = new Nothing();
                hasSelected = true;
            }
            return turnLog;
        }

        public virtual List<string> ExtraTurn()
        {
            return new List<string>();
        }

        public List<ColossoFighter> GetEnemies()
        {
            return battle.GetTeam(enemies);
        }

        public string GetMoves(bool detailed = true)
        {
            var relevantMoves = moves.Where(m => m is Psynergy).ToList().Select(m => m.emote);
            if (detailed)
            {
                relevantMoves = moves.Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m).ConvertAll(p => $"{p.emote} {p.name} `{p.PPCost}`");
            }
            return string.Join(" - ", relevantMoves);
        }

        public List<ColossoFighter> GetTeam()
        {
            return battle.GetTeam(party);
        }

        public bool HasCondition(Condition con)
        {
            return Conditions.Contains(con);
        }

        public bool HasCurableCondition()
        {
            Condition[] badConditions = { Condition.Poison, Condition.Venom, Condition.Seal, Condition.Sleep, Condition.Stun, Condition.DeathCurse };
            return Conditions.Any(c => badConditions.Contains(c));
        }

        public List<string> Heal(uint healHP)
        {
            List<string> log = new List<string>();
            if (!IsAlive())
            {
                log.Add($"{name} is unaffected");
                return log;
            }

            stats.HP = (int)Math.Min(stats.HP + healHP, stats.MaxHP);
            if (stats.HP == stats.MaxHP)
            {
                log.Add($"{name}'s HP was fully restored!");
            }
            else
            {
                log.Add($"{name} recovers {healHP} HP.");
            }
            return log;
        }

        public List<string> RestorePP(uint restorePP)
        {
            List<string> log = new List<string>();
            if (!IsAlive())
            {
                log.Add($"{name} is unaffected");
                return log;
            }

            stats.PP = (int)Math.Min(stats.PP + restorePP, stats.MaxPP);
            if (stats.PP == stats.MaxPP)
            {
                log.Add($"{name}'s PP was fully restored!");
            }
            else
            {
                log.Add($"{name} recovers {restorePP} PP.");
            }
            return log;
        }

        public bool IsAlive()
        {
            return !HasCondition(Condition.Down);
        }

        public void Kill()
        {
            stats.HP = 0;
            RemoveAllConditions();
            AddCondition(Condition.Down);
            Buffs = new List<Buff>();
        }

        public List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            if (!IsAlive())
            {
                return turnLog;
            }

            if (!selected.hasPriority)
            {
                turnLog.AddRange(selected.Use(this));
            }
            else
            {
                if (selected is Defend)
                {
                    turnLog.Add($"{selected.emote} {this.name} is defending.");
                }
            }
            RemoveCondition(Condition.Flinch);
            //Haunt Damage
            if (HasCondition(Condition.Haunt) && Global.Random.Next(0, 2) == 0)
            {
                var hauntDmg = Math.Min(280, (uint)(stats.HP * Global.Random.Next(20, 40) / 100));
                turnLog.AddRange(DealDamage(hauntDmg));
            }

            return turnLog;
        }

        public double MultiplyBuffs(string stat)
        {
            var mult = Buffs.Where(b => b.stat.Equals(stat, StringComparison.InvariantCultureIgnoreCase) && b.multiplier > 0).Aggregate(1.0, (p, s) => p *= s.multiplier);
            if (mult > 2.0)
            {
                mult = 2.0;
            }

            if (mult < 0.4)
            {
                mult = 0.4;
            }

            return mult;
        }

        public void RemoveAllConditions()
        {
            Condition[] dontRemove = new Condition[] { Condition.Down, Condition.Counter, Condition.ItemCurse, Condition.Haunt };
            Conditions.RemoveAll(c => !dontRemove.Contains(c));
            DeathCurseCounter = 4;
        }

        public void RemoveNearlyAllConditions()
        {
            Condition[] dontRemove = new Condition[] { Condition.Down, Condition.Counter, Condition.ItemCurse, Condition.Poison, Condition.Venom, Condition.Haunt };
            Conditions.RemoveAll(c => !dontRemove.Contains(c));
            DeathCurseCounter = 4;
        }

        public void RemoveCondition(Condition con)
        {
            if (Conditions.Contains(con))
            {
                Conditions.Remove(con);
                if (con == Condition.DeathCurse)
                {
                    DeathCurseCounter = 4;
                }
            }
        }

        public List<string> Revive(uint percentage)
        {
            List<string> log = new List<string>();
            if (!IsAlive())
            {
                stats.HP = (int)(stats.MaxHP * percentage / 100);
                log.Add($"{name} is back on their feet.");
                RemoveCondition(Condition.Down);
            }
            else
            {
                log.Add($"{name} is unaffected");
            }
            return log;
        }

        public bool Select(string emote)
        {
            string[] numberEmotes = new string[] {"\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };
            var trySelected = moves.Where(m => m.emote == emote).FirstOrDefault() ?? moves.Where(m => m.emote.Contains(emote)).FirstOrDefault();
            if (!IsAlive())
            {
                return false;
            }

            if (trySelected == null)
            {
                if (numberEmotes.Contains(emote) && selected != null)
                {
                    selected.targetNr = Array.IndexOf(numberEmotes, emote) - 1;
                    hasSelected = true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (trySelected is Psynergy && ((Psynergy)trySelected).PPCost > stats.PP)
                {
                    return false;
                }
                else
                {
                    selected = trySelected;
                }
            }

            if (selected.targetType == Target.self || selected.targetType == Target.ownAll || selected.targetType == Target.otherAll)
            {
                hasSelected = true;
            }

            if ((selected.targetType == Target.ownSingle && battle.GetTeam(party).Count == 1) ||
                ((selected.targetType == Target.otherSingle || selected.targetType == Target.otherRange) && battle.GetTeam(enemies).Count == 1))
            {
                selected.targetNr = 0;
                hasSelected = true;
            }
            if (this is PlayerFighter)
            {
                ((PlayerFighter)this).AutoTurnsInARow = 0;
            }
            return true;
        }

        public void Select(int targetNr)
        {
            selected.targetNr = targetNr;
        }

        public void SelectRandom()
        {
            if (!battle.isActive)
            {
                Console.WriteLine("Why tf do you want to selectRandom(), the battle is *not* active!");
                return;
            }
            if (moves.Count() == 0)
            {
                selected = new Nothing();
                hasSelected = true;
                return;
            }
            selected = moves.Random();

            selected.targetNr = 0;
            Console.WriteLine($"{selected.name} was rolled.");

            if (selected.ValidSelection(this))
            {
                selected.ChooseBestTarget(this);
                Console.WriteLine($"  {selected.name} passed the check.");
            }
            else
            {
                Console.WriteLine($"X {selected.name} was a bad choice. Rerolling.");
                SelectRandom();
                return;
            }
            hasSelected = true;
        }

        public virtual List<string> StartTurn()
        {
            List<string> turnLog = new List<string>();
            if (selected.hasPriority)
            {
                turnLog.AddRange(selected.Use(this));
            }

            return turnLog;
        }
    }
}