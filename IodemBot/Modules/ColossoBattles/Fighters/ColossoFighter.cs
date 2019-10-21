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

        public static readonly Dictionary<Condition, string> ConditionStrings = new Dictionary<Condition, string>()
        {
            { Condition.Down, "<:curse:538074679492083742>"},
            { Condition.Poison, "<:Poison:549526931847249920>"},
            { Condition.Venom, "<:Venom:598458704400220160>"},
            { Condition.Seal, "<:Psy_Seal:549526931465568257>"},
            { Condition.Stun, "<:Flash_Bolt:536966441862299678>"},
            { Condition.DeathCurse, ""},
            { Condition.Haunt, "<:Haunted:549526931821953034>"},
            { Condition.ItemCurse, "<:Condemn:583651784040644619>"},
            { Condition.Flinch, ""},
            { Condition.Delusion, "<:delusion:549526931637534721>"},
            {Condition.Sleep, "<:Sleep:555427023519088671>" },
            {Condition.Counter, "" }
        };

        public string Name { get; set; } = "";
        public Stats Stats { get; set; } = new Stats(1, 1, 1, 1, 1);
        public ElementalStats ElStats { get; set; } = new ElementalStats();
        public string ImgUrl { get; set; }
        [JsonIgnore] public Move[] Moves { get; set; }

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
        public bool IsAlive { get { return !HasCondition(Condition.Down); } }

        internal ColossoFighter()
        {
        }

        public void AddCondition(Condition con)
        {
            if (!HasCondition(con))
            {
                if (con == Condition.Venom && HasCondition(Condition.Poison))
                {
                    RemoveCondition(Condition.Poison);
                }

                if (con == Condition.Poison && HasCondition(Condition.Venom))
                {
                    return;
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

            if (Name == obj.Name)
            {
                return 0;
            }

            if (Stats.Spd > obj.Stats.Spd)
            {
                return 1;
            }

            if (Stats.Spd == obj.Stats.Spd)
            {
                return 0;
            }

            return -1;
        }

        public string ConditionsToString()
        {
            StringBuilder s = new StringBuilder();

            if (Stats.HP != 0 && 100 * Stats.HP / Stats.MaxHP <= 5)
            {
                s.Append("<:Exclamatory:549529360604856323>");
            }

            if (HasCondition(Condition.DeathCurse))
            {
                string[] DeathCurseEmotes = { ":grey_question:", "<:DeathCurse1:583645163499552791>", "<:DeathCurse2:583645163927109636>", "<:DeathCurse3:583644633314099202>", "<:DeathCurse2:583645163927109636><:DeathCurse2:583645163927109636>" };
                if (DeathCurseCounter >= DeathCurseEmotes.Length)
                {
                    s.Append($"<:DeathCurse1:583645163499552791>{DeathCurseCounter}");
                }
                else
                {
                    s.Append(DeathCurseEmotes[DeathCurseCounter]);
                }
            }

            Conditions.ForEach(c => s.Append(ConditionStrings[c]));

            if (MultiplyBuffs("Attack") != 1)
            {
                s.Append($"<:attack_up:549526931423363093>`x{MultiplyBuffs("Attack")}`");
            }

            if (MultiplyBuffs("Defense") != 1)
            {
                s.Append($"<:defense_up:549526931557842945>`x{MultiplyBuffs("Defense")}`");
            }

            if (MultiplyBuffs("Speed") != 1)
            {
            }

            if (MultiplyBuffs("Power") != 1)
            {
                s.Append($"<:resist_up:549526931465437185>`x{MultiplyBuffs("Power")}`");
            }

            if (MultiplyBuffs("Resistance") != 0)
            {
            }
            return s.ToString();
        }

        public virtual List<string> DealDamage(uint damage, string punctuation = "!")
        {
            var log = new List<string>
            {
                $"{Name} takes {damage} damage{punctuation}"
            };
            if (!IsAlive)
            {
                log.Add("Someone tried to damage the dead. This shouldn't have happened... Please use i!bug and name the action that was performed");
                return log;
            }
            if (Stats.HP > damage)
            {
                Stats.HP -= (int)damage;
                if (this is PlayerFighter)
                {
                    ((PlayerFighter)this).battleStats.DamageTanked += damage;
                }
            }
            else
            {
                Kill();
                log.Add($":x: {Name} goes down.");
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
                    turnLog.Add($"{Name}'s {s.stat} normalizes.");
                }
            });
            Buffs = newBuffs;
            defensiveMult = 1;
            offensiveMult = 1;

            if (IsAlive)
            {
                if (HPrecovery > 0 && Stats.HP < Stats.MaxHP)
                {
                    turnLog.AddRange(Heal((uint)HPrecovery));
                }
                if (PPrecovery > 0 && Stats.PP < Stats.MaxPP)
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
                    turnLog.Add($"{Name} wakes up.");
                }
            }
            //Chance to remove Stun
            if (HasCondition(Condition.Stun))
            {
                if (Global.Random.Next(0, 2) == 0)
                {
                    RemoveCondition(Condition.Stun);
                    turnLog.Add($"{Name} can move again.");
                }
            }
            //Chance to remove Seal
            if (HasCondition(Condition.Seal))
            {
                if (Global.Random.Next(0, 3) == 0)
                {
                    RemoveCondition(Condition.Seal);
                    turnLog.Add($"{Name}'s Psynergy is no longer sealed.");
                }
            }
            //Chance to remove Delusion
            if (HasCondition(Condition.Delusion))
            {
                if (Global.Random.Next(0, 4) == 0)
                {
                    RemoveCondition(Condition.Delusion);
                    turnLog.Add($"{Name} can see clearly again.");
                }
            }

            if (HasCondition(Condition.DeathCurse))
            {
                DeathCurseCounter--;
                if (DeathCurseCounter <= 0)
                {
                    Kill();
                    turnLog.Add($":x: {Name}'s light goes out.");
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
                    turnLog.Add($"{item.IconDisplay} {Name}'s {item.Name} starts to Glow.");
                    foreach (var effect in item.Unleash.Effects)
                    {
                        turnLog.AddRange(effect.Apply(this, this));
                    }

                    if (Global.Random.Next(0, 100) <= item.ChanceToBreak)
                    {
                        item.IsBroken = true;
                        turnLog.Add($"{item.IconDisplay} {Name}'s {item.Name} breaks;");
                    }
                }
            }
            damageDoneThisTurn = 0;
            if (!IsAlive)
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
            var relevantMoves = Moves.Where(m => m is Psynergy).ToList().Select(m => m.emote);
            if (detailed)
            {
                relevantMoves = Moves.Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m).ConvertAll(p => $"{p.emote} {p.name} `{p.PPCost}`");
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
            if (!IsAlive)
            {
                log.Add($"{Name} is unaffected");
                return log;
            }

            Stats.HP = (int)Math.Min(Stats.HP + healHP, Stats.MaxHP);
            if (Stats.HP == Stats.MaxHP)
            {
                log.Add($"{Name}'s HP was fully restored!");
            }
            else
            {
                log.Add($"{Name} recovers {healHP} HP.");
            }
            return log;
        }

        public List<string> RestorePP(uint restorePP)
        {
            List<string> log = new List<string>();
            if (!IsAlive)
            {
                log.Add($"{Name} is unaffected");
                return log;
            }

            Stats.PP = (int)Math.Min(Stats.PP + restorePP, Stats.MaxPP);
            if (Stats.PP == Stats.MaxPP)
            {
                log.Add($"{Name}'s PP was fully restored!");
            }
            else
            {
                log.Add($"{Name} recovers {restorePP} PP.");
            }
            return log;
        }

        public void Kill()
        {
            Stats.HP = 0;
            RemoveAllConditions();
            AddCondition(Condition.Down);
            Buffs = new List<Buff>();
        }

        public List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            if (!IsAlive)
            {
                return turnLog;
            }

            if (!selected.hasPriority)
            {
                turnLog.AddRange(selected.Use(this));
            }

            RemoveCondition(Condition.Flinch);
            //Poison Damage
            if (HasCondition(Condition.Poison))
            {
                var damage = Math.Min(200, (uint)(Stats.MaxHP * Global.Random.Next(5, 10) / 100));
                turnLog.Add($"{Name} is damaged by the Poison.");
                turnLog.AddRange(DealDamage(damage));
            }
            if (HasCondition(Condition.Venom))
            {
                var damage = Math.Min(400, (uint)(Stats.MaxHP * Global.Random.Next(10, 20) / 100));
                turnLog.Add($"{Name} is damaged by the Venom.");
                turnLog.AddRange(DealDamage(damage));
            }
            //Haunt Damage
            if (HasCondition(Condition.Haunt) && Global.Random.Next(0, 2) == 0)
            {
                var hauntDmg = Math.Min(280, (uint)(Stats.HP * Global.Random.Next(20, 40) / 100));
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
            if (!IsAlive)
            {
                Stats.HP = (int)(Stats.MaxHP * percentage / 100);
                log.Add($"{Name} is back on their feet.");
                RemoveCondition(Condition.Down);
            }
            else
            {
                log.Add($"{Name} is unaffected");
            }
            return log;
        }

        public bool Select(string emote)
        {
            string[] numberEmotes = new string[] {"\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };
            var trySelected = Moves.Where(m => m.emote == emote).FirstOrDefault() ?? Moves.Where(m => m.emote.Contains(emote)).FirstOrDefault();
            if (!IsAlive)
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
                if (trySelected is Psynergy && ((Psynergy)trySelected).PPCost > Stats.PP)
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
                ((selected.targetType == Target.otherSingle || selected.targetType == Target.otherRange) && battle.GetTeam(enemies).Count <= 1))
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

        public void ReplaceWith(ColossoFighter otherFighter)
        {
            Name = otherFighter.Name;
            ImgUrl = otherFighter.ImgUrl;
            Stats = otherFighter.Stats;
            ElStats = otherFighter.ElStats;
            Conditions = otherFighter.Conditions;
            Moves = otherFighter.Moves;
            isImmuneToConditions = otherFighter.isImmuneToConditions;
            DeathCurseCounter = otherFighter.DeathCurseCounter;
            PPrecovery = otherFighter.PPrecovery;
            HPrecovery = otherFighter.HPrecovery;
            IsImmuneToHPtoOne = otherFighter.IsImmuneToHPtoOne;
            IsImmuneToOHKO = otherFighter.IsImmuneToHPtoOne;
            IsImmuneToItemCurse = otherFighter.IsImmuneToItemCurse;
            IsImmuneToPsynergy = otherFighter.IsImmuneToPsynergy;
            Weapon = otherFighter.Weapon;
        }

        public void SelectRandom()
        {
            if (!battle.isActive)
            {
                throw new Exception("Why tf do you want to selectRandom(), the battle is *not* active!");
            }
            if (!IsAlive)
            {
                return;
            }
            if (Moves.Where(s => s.ValidSelection(this)).Count() == 0)
            {
                selected = new Nothing();
                hasSelected = true;
                return;
            }
            else
            {
                try
                {
                    selected = Moves.Where(s => s.ValidSelection(this)).Random();
                    selected.ChooseBestTarget(this);
                    hasSelected = true;
                }
                catch (Exception e)
                {
                    throw new Exception($"{Name} failed to select random Move: {selected.name}", e);
                }
            }
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