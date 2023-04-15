using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IodemBot.ColossoBattles
{
    public class Buff
    {
        public double Multiplier = 1;
        public string Stat = "";
        public uint Turns = 5;

        public Buff(string stat, double multiplier, uint turns)
        {
            Stat = stat;
            Multiplier = multiplier;
            Turns = turns;
        }
    }

    public abstract class ColossoFighter : IComparable<ColossoFighter>, ICloneable
    {
        [JsonIgnore] private readonly Dictionary<Condition, int> _conditionsFromTurn = new();
        [JsonIgnore] public List<Buff> Buffs = new();

        [JsonProperty("Conditions", ItemConverterType = typeof(StringEnumConverter))]
        private List<Condition> _conditions = new();

        public List<string> Tags = new();
        [JsonIgnore] public uint DamageDoneThisTurn;
        public int DeathCurseCounter = 4;
        [JsonIgnore] public double DefensiveMult = 1;
        public List<Item> EquipmentWithEffect = new();
        [JsonIgnore] public bool HasSelected;
        [JsonIgnore] public double IgnoreDefense = 1;

        [JsonProperty("isImmuneToConditions", ItemConverterType = typeof(StringEnumConverter))]
        public List<Condition> IsImmuneToConditions = new();

        [JsonIgnore] public List<LingeringEffect> LingeringEffects = new();
        [JsonIgnore] public double OffensiveMult = 1;
        public int UnleashRate = 35;
        public Item Weapon;

        internal ColossoFighter()
        {
        }

        public string Name { get; set; } = "";
        public Stats Stats { get; set; } = new(1, 1, 1, 1, 1);
        public ElementalStats ElStats { get; set; }

        public Stats BoostedStats
        {
            get => Stats * new Stats(100, 100,
                    (int)(MultiplyBuffs("Atk") * 100),
                    (int)(MultiplyBuffs("Def") * 100),
                    (int)(MultiplyBuffs("Speed") * 100)) * 0.01;
        }

        public string ImgUrl { get; set; }
        [JsonIgnore] public List<Move> Moves { get; set; }
        public Passive Passive { get; set; }
        public int PassiveLevel { get; set; }
        [JsonIgnore] public ColossoBattle Battle { get; set; }
        [JsonIgnore] public Team party { get; set; }
        [JsonIgnore] public Team enemies => party == Team.A ? Team.B : Team.A;
        [JsonIgnore] public List<ColossoFighter> Party => Battle.GetTeam(party);
        [JsonIgnore] public List<ColossoFighter> Enemies => Battle.GetTeam(enemies);
        public bool IsImmuneToOhko { get; set; }
        public bool IsImmuneToHPtoOne { get; set; }
        public bool IsImmuneToPsynergy { get; set; }
        public bool IsImmuneToItemCurse { get; set; }
        [JsonIgnore] public Move SelectedMove { get; set; }
        [JsonIgnore] public uint AddDamage { get; set; } = 0;
        public int HPrecovery { get; set; }
        public int PPrecovery { get; set; }
        public bool IsAlive => !HasCondition(Condition.Down);

        public bool IsImmobilized => HasCondition(new[] { Condition.Down, Condition.Sleep, Condition.Stun, Condition.Flinch });

        public bool HasCurableCondition
        {
            get
            {
                Condition[] badConditions =
                {
                    Condition.Poison, Condition.Venom, Condition.Seal, Condition.Sleep, Condition.Stun,
                    Condition.DeathCurse
                };
                return _conditions.Any(c => badConditions.Contains(c));
            }
        }

        public abstract object Clone();

        public int CompareTo(ColossoFighter obj)
        {
            if (obj == null) return 1;

            if (Name == obj.Name) return 0;

            if (Stats.Spd > obj.Stats.Spd) return 1;

            if (Stats.Spd == obj.Stats.Spd) return 0;

            return -1;
        }

        public void AddCondition(Condition con)
        {
            if (!HasCondition(con))
            {
                if (HasCondition(Condition.Avoid))
                    return;

                if (con == Condition.Venom && HasCondition(Condition.Poison)) RemoveCondition(Condition.Poison);

                if (con == Condition.Poison && HasCondition(Condition.Venom)) return;

                _conditions.Add(con);
                _conditionsFromTurn[con] = Battle?.TurnNumber ?? 0;
            }
        }

        public void ApplyBuff(Buff buff)
        {
            var existingBuff = Buffs
                .FirstOrDefault(b => b.Stat == buff.Stat && (b.Multiplier - 1) * (buff.Multiplier - 1) >= 0);
            if (existingBuff == null)
            {
                Buffs.Add(buff);
            }
            else
            {
                existingBuff.Multiplier = Math.Max(0.001, existingBuff.Multiplier + (buff.Multiplier - 1));
                existingBuff.Turns = Math.Max(existingBuff.Turns, buff.Turns);
            }
        }

        public string ConditionsToString()
        {
            var s = new StringBuilder();

            if (Stats.HP != 0 && 100 * Stats.HP / Stats.MaxHP <= 10) s.Append("<:Exclamatory:549529360604856323>");

            if (HasCondition(Condition.DeathCurse))
            {
                string[] deathCurseEmotes =
                {
                    ":grey_question:", "<:DeathCurse1:583645163499552791>", "<:DeathCurse2:583645163927109636>",
                    "<:DeathCurse3:583644633314099202>",
                    "<:DeathCurse2:583645163927109636><:DeathCurse2:583645163927109636>"
                };
                s.Append(DeathCurseCounter >= deathCurseEmotes.Length
                    ? $"<:DeathCurse1:583645163499552791>{DeathCurseCounter}"
                    : deathCurseEmotes[DeathCurseCounter]);
            }

            _conditions.Where(c => c != Condition.DeathCurse).ToList().ForEach(c => s.Append(Emotes.GetIcon(c, "")));

            var stat = MultiplyBuffs("Attack");
            if (stat != 1)
                s.Append(
                    $"{(stat > 1 ? "<:Atk_Increase:669146889471393833>" : "<:Atk_Decrease:669147349859303433>").ToShortEmote()}`x{stat}`");

            stat = MultiplyBuffs("Defense");
            if (stat != 1)
                s.Append(
                    $"{(stat > 1 ? "<:Def_Increase:669147527710375957>" : "<:Def_Decrease:669147401780461568>").ToShortEmote()}`x{stat}`");

            stat = MultiplyBuffs("Resistance");
            if (stat != 1)
                s.Append(
                    $"{(stat > 1 ? "<:Res_Increase:669147593963601960>" : "<:Res_Decrease:669147473373298698>").ToShortEmote()}`x{stat}`");

            stat = MultiplyBuffs("Power");
            if (stat != 1)
                s.Append(
                    $"{(stat > 1 ? "<:Pow_Increase:669147830316695563>" : "<:Pow_Decrease:669147728651223040>").ToShortEmote()}`x{stat}`");

            stat = MultiplyBuffs("Speed");
            if (stat != 1)
                s.Append(
                    $"{(stat > 1 ? "<:Spe_Increase:669147782732316682>" : "<:Spe_Decrease:669147666164350976>").ToShortEmote()}`x{stat}`");

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
                log.Add(
                    "Someone tried to damage the dead. This shouldn't have happened... Please use i!bug and name the action that was performed");
                return log;
            }

            if (Stats.HP > damage)
            {
                Stats.HP -= (int)damage;
                var colossoFighter = this;
                if (colossoFighter is PlayerFighter) ((PlayerFighter)this).BattleStats.DamageTanked += damage;
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
            var turnLog = new List<string>();
            turnLog.AddRange(EndTurnLingeringEffects()); // Should include Passive Healing and Damaging Abilities

            turnLog.AddRange(EndTurnDjinnRecovery());
            RemoveCondition(Condition.Counter); //Might need to go first

            turnLog.AddRange(EndTurnPassiveRecovery()); // HP and PP from equipment

            turnLog.AddRange(EndTurnDoBuffs()); // Count Down Stat Increases
            turnLog.AddRange(EndTurnDoConditions()); // Poison, Venom, Stun, Sleep, Seal, Delusion, Curse

            turnLog.AddRange(EndTurnItemActivation()); // Custom Item unleashes like Unicorn Ring or Soul Ring
            turnLog.AddRange(EndTurnRunner());
            DamageDoneThisTurn = 0;
            DefensiveMult = 1;
            OffensiveMult = 1;
            if (!IsAlive)
            {
                SelectedMove = new Nothing();
                HasSelected = true;
            }

            return turnLog;
        }

        private List<string> EndTurnRunner()
        {
            if (!IsAlive || !Tags.Any(t => t.StartsWith("Runner"))) return new();

            var tag = Tags.First(t => t.StartsWith("Runner"));
            var splits = tag.Split(':');
            var turns = splits.Length > 1 ? int.Parse(splits[1]) : 0;
            var msg = splits.Length > 2 ? splits[2] : "";
            if (turns > 0)
            {
                Tags.Remove(tag);
                Tags.Add($"Runner:{turns - 1}{(msg.IsNullOrEmpty() ? "" : $":{msg}")}");
                return new();
            }

            var newEnemy = EnemiesDatabase.GetEnemy("Next");
            if (Party.Count(c => c.Name != "Runner") > 1)
                newEnemy.Name = "Runner";
            else
                newEnemy.Name = "No Enemies remaining";
            List<string> log = new() { msg.IsNullOrEmpty() ? $"{Name} ran away." : msg };
            ReplaceWith(newEnemy);
            return log;
        }

        private List<string> EndTurnDoBuffs()
        {
            var turnLog = new List<string>();
            var newBuffs = new List<Buff>();
            Buffs.ForEach(s =>
            {
                s.Turns -= 1;
                if (s.Turns >= 1)
                    newBuffs.Add(s);
                else
                    turnLog.Add($"{Name}'s {s.Stat} normalizes.");
            });
            Buffs = newBuffs;
            return turnLog;
        }

        private List<string> EndTurnPassiveRecovery()
        {
            var turnLog = new List<string>();
            if (IsAlive)
            {
                if (HPrecovery > 0 && Stats.HP < Stats.MaxHP) turnLog.AddRange(Heal((uint)HPrecovery));
                if (PPrecovery > 0 && Stats.PP < Stats.MaxPP) turnLog.AddRange(RestorePp((uint)PPrecovery));
            }

            return turnLog;
        }

        private List<string> ConditionDamage()
        {
            var turnLog = new List<string>();
            if (HasCondition(Condition.Poison))
            {
                var damage = Math.Min(200, (uint)(Stats.MaxHP * Global.RandomNumber(5, 10) / 100));
                turnLog.Add($"{Name} is damaged by the Poison.");
                turnLog.AddRange(DealDamage(damage));
            }

            if (HasCondition(Condition.Venom))
            {
                var damage = Math.Min(400, (uint)(Stats.MaxHP * Global.RandomNumber(10, 20) / 100));
                turnLog.Add($"{Name} is damaged by the Venom.");
                turnLog.AddRange(DealDamage(damage));
            }

            //Haunt Damage
            if (HasCondition(Condition.Haunt) && Global.RandomNumber(0, 2) == 0)
            {
                var hauntDmg = DamageDoneThisTurn / 4;
                turnLog.AddRange(DealDamage(hauntDmg));
            }

            return turnLog;
        }

        private List<string> EndTurnDoConditions()
        {
            var turnLog = new List<string>();

            RemoveCondition(Condition.Flinch);

            //Chance to wake up
            if (HasCondition(Condition.Avoid))
            {
                if (!_conditionsFromTurn.TryGetValue(Condition.Avoid, out int fromTurn)) fromTurn = 0;
                if (Battle.TurnNumber - fromTurn != 0)
                    if (Global.RandomNumber(0, 100) <= 30)
                    {
                        RemoveCondition(Condition.Avoid);
                        turnLog.Add($"{Name} is suscpect to status conditions again.");
                    }
            }

            //Chance to wake up
            if (HasCondition(Condition.Sleep))
            {
                if (!_conditionsFromTurn.TryGetValue(Condition.Sleep, out int fromTurn)) fromTurn = 0;
                if (Battle.TurnNumber - fromTurn != 0)
                    if (Battle.TurnNumber - fromTurn >= Party.Count || Global.RandomNumber(0, 100) <= 60)
                    {
                        RemoveCondition(Condition.Sleep);
                        turnLog.Add($"{Name} wakes up.");
                    }
            }

            //Chance to remove Stun
            if (HasCondition(Condition.Stun))
            {
                if (!_conditionsFromTurn.TryGetValue(Condition.Stun, out int fromTurn)) fromTurn = 0;
                if (Battle.TurnNumber - fromTurn != 0)
                    if (Battle.TurnNumber - fromTurn >= Party.Count || Global.RandomNumber(0, 100) <= 60)
                    {
                        RemoveCondition(Condition.Stun);
                        turnLog.Add($"{Name} can move again.");
                    }
            }
            //Chance to remove Seal
            if (HasCondition(Condition.Seal))
            {
                if (!_conditionsFromTurn.TryGetValue(Condition.Seal, out int fromTurn)) fromTurn = 0;
                if (Battle.TurnNumber - fromTurn != 0)
                    if (Battle.TurnNumber - fromTurn >= Party.Count || Global.RandomNumber(0, 100) <= 60)
                    {
                        RemoveCondition(Condition.Seal);
                        turnLog.Add($"{Name}'s Psynergy is no longer sealed.");
                    }
            }
            //Chance to remove Delusion
            if (HasCondition(Condition.Delusion))
            {
                if (!_conditionsFromTurn.TryGetValue(Condition.Delusion, out int fromTurn)) fromTurn = 0;
                if (Battle.TurnNumber - fromTurn != 0)
                    if (Battle.TurnNumber - fromTurn >= Party.Count || Global.RandomNumber(0, 100) <= 33)
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
                    if (Party.Count == 1 && this is PlayerFighter p)
                    {
                        p.Stats.HP = 1;
                        RemoveCondition(Condition.DeathCurse);
                        turnLog.Add($"<:DeathCurse1:583645163499552791> {Name} barely holds on.");
                    }
                    else
                    {
                        Kill();
                        turnLog.Add($":x: {Name}'s light goes out.");
                    }
                }
            }

            return turnLog;
        }

        private List<string> EndTurnLingeringEffects()
        {
            if (LingeringEffects.Count > 0 && IsAlive)
            {
                Console.WriteLine($"Applying LingeringEffects: {string.Join("", LingeringEffects.Select(e => e.Effect.Type))}");
                return LingeringEffects.SelectMany(e => e.ApplyLingering(this)).ToList();
            }

            return new List<string>();
        }

        private List<string> EndTurnDjinnRecovery()
        {
            return Moves.OfType<Djinn>().SelectMany(d => d.EndTurn(this)).ToList();
        }

        private List<string> EndTurnItemActivation()
        {
            var turnLog = new List<string>();
            foreach (var item in EquipmentWithEffect)
                if (item.IsUnleashable
                    && !item.IsBroken
                    && (item.Unleash.AllEffects.Where(e => !(e is ReviveEffect)).Any(e => e.ValidSelection(this))
                        || item.Unleash.AllEffects.Any(e => e is ReviveEffect r && !IsAlive))
                    && Global.RandomNumber(0, 100) <= item.ChanceToActivate
                    && !HasCondition(Condition.Decoy))
                {
                    turnLog.Add($"{item.IconDisplay} {Name}'s {item.Name} starts to Glow.");
                    foreach (var effect in item.Unleash.AllEffects) turnLog.AddRange(effect.Apply(this, this));

                    if (Global.RandomNumber(0, 100) <= item.ChanceToBreak)
                    {
                        item.IsBroken = true;
                        turnLog.Add($"{item.IconDisplay} {Name}'s {item.Name} breaks;");
                    }
                }

            return turnLog;
        }

        public virtual List<string> ExtraTurn()
        {
            return new List<string>();
        }

        public string GetMoves(bool detailed = true)
        {
            var relevantMoves = Moves.Where(m => m is Psynergy).ToList().Select(m => m.Emote);
            if (detailed)
                relevantMoves = Moves.Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m)
                    .ConvertAll(p => $"{p.Emote} {p.Name} `{p.PpCost}`");
            return string.Join(" - ", relevantMoves);
        }

        public bool HasCondition(params Condition[] con)
        {
            return _conditions.Intersect(con).Any();
        }

        public List<string> Heal(uint healHp)
        {
            var log = new List<string>();
            if (!IsAlive)
            {
                log.Add($"{Name} is unaffected");
                return log;
            }

            Stats.HP = (int)Math.Min(Stats.HP + healHp, Stats.MaxHP);
            log.Add(Stats.HP == Stats.MaxHP ? $"{Name}'s HP was fully restored!" : $"{Name} recovers {healHp} HP.");
            return log;
        }

        public List<string> RestorePp(uint restorePp)
        {
            var log = new List<string>();
            if (!IsAlive)
            {
                log.Add($"{Name} is unaffected");
                return log;
            }

            Stats.PP = (int)Math.Min(Stats.PP + restorePp, Stats.MaxPP);
            log.Add(Stats.PP == Stats.MaxPP ? $"{Name}'s PP was fully restored!" : $"{Name} recovers {restorePp} PP.");
            return log;
        }

        public void Kill()
        {
            Stats.HP = 0;
            RemoveAllConditions();
            AddCondition(Condition.Down);
            Buffs = new List<Buff>();
            LingeringEffects.RemoveAll(e => e.RemovedOnDeath);
            SelectedMove = new Nothing();

            if (Tags.Any(t => t == "Head") && !Party.Any(p => p.IsAlive && p.Tags.Any(t => t == "Head")))
                Party.Where(e => e.IsAlive).ToList().ForEach(e => e.Kill());

            // OnKill@4@8
            var OnKillTag = Tags.FirstOrDefault(t => t.StartsWith("OnKill"));
            if (OnKillTag is not null)
            {
                var OnKillBattleNumbers = OnKillTag.Split('@').Skip(1).Select(int.Parse);
                Battle.OutValue = OnKillBattleNumbers.Random();
            }

            // KilledBefore:3@4@5
            var killedBeforeTag = Tags.FirstOrDefault(t => t.StartsWith("KilledBeforeTurn"));
            if (killedBeforeTag is null) return;

            var args = killedBeforeTag.Split(':').Last();
            var argsSplits = args.Split('@');
            var beforeTurn = int.Parse(argsSplits.First());
            var battleNumbers = argsSplits.Skip(1).Select(int.Parse);
            if (Battle.TurnNumber <= beforeTurn)
                Battle.OutValue = battleNumbers.Random();
        }

        public List<string> MainTurn()
        {
            var turnLog = new List<string>();
            if (!IsAlive) return turnLog;

            if (!SelectedMove.HasPriority) turnLog.AddRange(SelectedMove.Use(this));

            RemoveCondition(Condition.Flinch);
            turnLog.AddRange(ConditionDamage());
            return turnLog;
        }

        public double MultiplyBuffs(string stat)
        {
            var mult = Buffs.Where(b => b.Stat.Equals(stat, StringComparison.InvariantCultureIgnoreCase))
                .Aggregate(1.0, (p, s) => p *= s.Multiplier);
            mult = Math.Min(mult, 2.0);
            mult = Math.Max(mult, 0.4);
            return Math.Round(mult, 2);
        }

        public void RemoveAllConditions()
        {
            Condition[] dontRemove =
            {
                Condition.Down, Condition.Counter, Condition.ItemCurse, Condition.Key, Condition.Trap, Condition.Decoy
            };
            _conditions.RemoveAll(c => !dontRemove.Contains(c));
            DeathCurseCounter = 4;
        }

        public void RemoveNearlyAllConditions()
        {
            Condition[] dontRemove =
            {
                Condition.Down, Condition.Counter, Condition.ItemCurse, Condition.Poison, Condition.Venom,
                Condition.Haunt
            };
            _conditions.RemoveAll(c => !dontRemove.Contains(c));
            DeathCurseCounter = 4;
        }

        public int RemoveCondition(params Condition[] con)
        {
            var removed = 0;
            foreach (var c in con)
            {
                if (_conditions.Remove(c))
                    removed++;
                _conditionsFromTurn.Remove(c);
                if (c == Condition.DeathCurse) DeathCurseCounter = 4;
            }
            return removed;
        }

        public List<string> Revive(uint percentage)
        {
            var log = new List<string>();
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

        public bool Select(IEmote emote)
        {
            string[] numberEmotes =
            {
                "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣",
                "6️⃣", "7️⃣", "8️⃣", "9️⃣"
            };

            var trySelected = Moves.FirstOrDefault(m => m.GetEmote().Equals(emote));
            //var trySelected = Moves.Where(m => m.Emote == emote).FirstOrDefault() ?? Moves.Where(m => m.Emote.Contains(emote)).FirstOrDefault();
            if (!IsAlive)
            {
                Console.WriteLine("Dead Players can't select");
                return false;
            }

            if (trySelected == null)
            {
                if (numberEmotes.Contains(emote.Name) && SelectedMove != null)
                {
                    SelectedMove.TargetNr = Array.IndexOf(numberEmotes, emote.Name) - 1;
                    HasSelected = true;
                }
                else
                {
                    Console.WriteLine(
                        $"{Name}: {emote.Name} {emote} not in {string.Join(",", Moves.Select(m => m.Emote))}");
                    return false;
                }
            }
            else
            {
                if (trySelected is Psynergy psynergy && psynergy.PpCost > Stats.PP)
                {
                    Console.WriteLine("Not enough PP");
                    return false;
                }

                SelectedMove = trySelected;
            }

            if (SelectedMove.TargetType == TargetType.PartySelf ||
                SelectedMove.TargetType == TargetType.PartyAll ||
                SelectedMove.TargetType == TargetType.EnemyAll ||
                SelectedMove.TargetType == TargetType.PartySingle && Party.Count == 1 ||
                SelectedMove.TargetType == TargetType.EnemyRange && Enemies.Count == 1)
            {
                HasSelected = true;
                SelectedMove.TargetNr = 0;
            }

            if (this is PlayerFighter fighter) fighter.AutoTurnsInARow = 0;
            return true;
        }

        public void SetTarget(int targetNr)
        {
            SelectedMove.TargetNr = targetNr;
        }

        public void ReplaceWith(ColossoFighter otherFighter)
        {
            Name = otherFighter.Name;
            ImgUrl = otherFighter.ImgUrl;
            Stats = new Stats(otherFighter.Stats);
            ElStats = new ElementalStats(otherFighter.ElStats);
            _conditions = otherFighter._conditions.ToList();
            Moves = otherFighter.Moves.Select(m => m is Djinn ? (Djinn)m.Clone() : m).ToList();
            IsImmuneToConditions = otherFighter.IsImmuneToConditions;
            DeathCurseCounter = otherFighter.DeathCurseCounter;
            PPrecovery = otherFighter.PPrecovery;
            HPrecovery = otherFighter.HPrecovery;
            IsImmuneToHPtoOne = otherFighter.IsImmuneToHPtoOne;
            IsImmuneToOhko = otherFighter.IsImmuneToHPtoOne;
            IsImmuneToItemCurse = otherFighter.IsImmuneToItemCurse;
            IsImmuneToPsynergy = otherFighter.IsImmuneToPsynergy;
            Weapon = (Item)otherFighter.Weapon?.Clone();
            Tags = otherFighter.Tags.ToList();
        }

        public void SelectRandom(bool includePriority = true)
        {
            if (!Battle.IsActive)
                throw new Exception("Why tf do you want to selectRandom(), the battle is *not* active!");
            if (!IsAlive) return;
            if (!Moves.Any(s => s.ValidSelection(this)))
            {
                SelectedMove = new Nothing();
                HasSelected = true;
                return;
            }

            try
            {
                SelectedMove = Moves.Where(s => (includePriority || !s.HasPriority) && s.ValidSelection(this)).Random();
                SelectedMove.ChooseBestTarget(this);
                HasSelected = true;
            }
            catch (Exception e)
            {
                throw new Exception($"{Name} failed to select random Move: {SelectedMove.Name}", e);
            }
        }

        public virtual List<string> StartTurn()
        {
            var turnLog = new List<string>();
            if (SelectedMove.HasPriority) turnLog.AddRange(SelectedMove.Use(this));

            return turnLog;
        }
    }
}