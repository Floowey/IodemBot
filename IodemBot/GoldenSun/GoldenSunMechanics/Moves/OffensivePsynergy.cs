using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class OffensivePsynergy : Psynergy
    {
        private static readonly double[] Spread = { 1.0, 0.66, 0.5, 0.33, 0.25, 0.15, 0.1 };

        public static readonly Dictionary<string, string[]> MoveCategories = new()
        {
            { "##Plant", new[] { "Growth", "Mad Growth", "Wild Growth", "Thorn", "Nettle", "Briar" } }, // Maybe Punji
            { "##Fire", new[] { "Fireball" } },
            { "##Water", new[] { "Drench" } },
            { "##Ice", new[] { "Prism", "Frost", "Ice" } },
            { "##Wind", new[] { "Whirlwind" } },
            { "##Lightning", new[] { "Plasma", "Shine Plasma", "Spark Plasma", "Bolt", "Thunder Mine" } },
            { "##Earth", new[] { "Quake" } }, //Either incldue Spire and Rockfall or make separate ##Rock (or both)
            { "##Dragon", new[] { "Reigning Dragon", "Dragon Fume", "Dragon Cloud" } },
            { "##Sword", new[] { "Ragnarok", "Helm Splitter" } },
            { "##Explosion", new[] { "Burst", "Fire Bomb", "Blast" } },
            { "##Undead", new[] { "Undead Gloom", "Demon Night", "Call Zombie" } }
        };

        public uint Power { get; set; } = 0;
        public uint AddDamage { get; set; } = 0;
        public double DmgMult { get; set; } = 1;
        public uint PercentageDamage { get; set; } = 0;
        private bool AttackBased => Power == 0;
        public bool IgnoreSpread { get; set; }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<OffensivePsynergy>(serialized);
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            var aliveEnemies = user.Enemies.Where(f => f.IsAlive).ToList();
            if (aliveEnemies.Count == 0)
            {
                TargetNr = 0;
                return;
            }

            TargetNr = user.Enemies.IndexOf(aliveEnemies.Random());
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            //Psynergy Handling
            var log = new List<string>();

            //Get enemies and targeted enemies
            var actualSpread = new double[2 * Range - 1];
            var enemyTeam = user.Battle.GetTeam(user.enemies);
            var targets = GetTarget(user);

            var ii = 0;
            foreach (var t in targets)
            {
                if (!t.IsAlive) continue;

                if (PpCost > 1 && t.IsImmuneToPsynergy || t.HasCondition(Condition.Key) ||
                    t.HasCondition(Condition.Trap) || t.HasCondition(Condition.Decoy))
                {
                    log.Add($"A magical barrier is protecting {t.Name}.");
                    continue;
                }

                //Effects that trigger before damage
                log.AddRange(Effects.Where(e => e.ActivationTime == TimeToActivate.BeforeDamage).ApplyAll(user, t));

                if (!t.IsAlive) continue;

                var baseDmg = Global.RandomNumber(0, 4);
                var dmg = AttackBased
                    ? Math.Max(0,
                        (user.Stats.Atk * user.MultiplyBuffs("Attack") -
                         t.Stats.Def * t.IgnoreDefense * t.MultiplyBuffs("Defense")) / 2)
                    : (int)Power;

                //                var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)t.elstats.GetRes(element) * t.MultiplyBuffs("Resistance")) / (attackBased ? 400 : 200);
                //var elMult = 1 + (User.ElStats.GetPower(Element) * User.MultiplyBuffs("Power") - t.ElStats.GetRes(Element) * t.MultiplyBuffs("Resistance")) / (AttackBased ? 400 : 200);

                var elMultFactor =
                    (user.ElStats.GetPower(Element) * user.MultiplyBuffs("Power") -
                     t.ElStats.GetRes(Element) * t.MultiplyBuffs("Resistance")) / (AttackBased ? 400 : 200);
                var elMult = 1 + elMultFactor;
                elMult = Math.Pow(elMult, 1.4);
                //elMult = Math.Pow(2, elMultFactor);

                elMult = Math.Max(0.25, elMult);
                var distFromCenter = Math.Abs(enemyTeam.IndexOf(t) - TargetNr);
                var spreadMult = IgnoreSpread ? 1 : Spread[distFromCenter];
                var concentrationMult = 1 + Math.Max(0, (float)Range - targets.Count - 1) / (2 * Range);
                var prctdmg = (uint)(t.Stats.MaxHP * PercentageDamage / 100);
                var realDmg = (uint)((baseDmg + dmg + AddDamage + user.AddDamage) * DmgMult * elMult * spreadMult *
                    t.DefensiveMult * user.OffensiveMult * concentrationMult + prctdmg);
                var punctuation = "!";

                var immuneTag = t.Tags.FirstOrDefault(t => t.StartsWith("ImmuneTo:"))?.Split(':').Skip(1).Select(Enum.Parse<Element>)
                ?? Array.Empty<Element>();

                if (immuneTag.Contains(Element))
                {
                    log.Add($"{t.Name} is unharmed.");
                    return log;
                }

                if (t.ElStats.GetRes(Element) == t.ElStats.HighestRes()) punctuation = ".";

                if (t.ElStats.GetRes(Element) == t.ElStats.LowestRes())
                {
                    punctuation = "!!!";
                    if (user is PlayerFighter k) k.BattleStats.AttackedWeakness++;
                }

                realDmg = Math.Max(1, realDmg);

                log.AddRange(t.DealDamage(realDmg, punctuation));
                user.DamageDoneThisTurn += realDmg;

                log.AddRange(Effects.Where(e => e.ActivationTime == TimeToActivate.AfterDamage).ApplyAll(user, t));

                if (user is PlayerFighter p)
                {
                    p.BattleStats.DamageDealt += realDmg;
                    if (!t.IsAlive)
                    {
                        var Battle = user.Battle;
                        var killedByTags = t.Tags.Where(t => t.StartsWith("KilledBy"));
                        string[] MoveTypes = { "Djinn", "Hand", "Summon", "Psynergy" };

                        if (killedByTags.Any())
                        {
                            foreach (var killedByTag in killedByTags)
                            {
                                var args = killedByTag.Split('@');
                                var BattleNumbers = args.Skip(1).Select(int.Parse);
                                var MoveArgs = args.First().Split(':').Skip(1);

                                var ElementArgs = MoveArgs
                                    .Where(Enum.GetValues<Element>().Select(e => e.ToString()).Contains)
                                    .Select(Enum.Parse<Element>);
                                if (ElementArgs.Any() && !ElementArgs.Contains(Element))
                                    continue;

                                var parentMove = user.Moves.First(m => m.Name == Name);
                                var isPsy = MoveArgs.Contains("Psynergy") && parentMove is Psynergy { PpCost: > 1 };
                                var isDjinn = MoveArgs.Contains("Djinn") && parentMove is Djinn;
                                var isSummon = MoveArgs.Contains("Summon") && parentMove is Summon;
                                if (MoveArgs.Any(MoveTypes.Contains) && !(isPsy || isDjinn || isSummon))
                                    continue;

                                var MoveNames = MoveArgs.Where(s => s.StartsWith('#') && !s.StartsWith("##")).Select(s => s[1..]).ToList();
                                MoveNames.AddRange(MoveArgs.Where(s => s.StartsWith("##")).SelectMany(MoveCategories.GetValueOrDefault).ToList());
                                if (MoveNames.Any() && !MoveNames.Contains(parentMove is Djinn d ? d.Djinnname : Name))
                                    continue;

                                Battle.OutValue = BattleNumbers.Random();
                            }
                        }

                        if (AttackBased && Range == 1) p.BattleStats.KillsByHand++;
                        p.BattleStats.Kills++;
                    }
                }

                if (t.IsAlive && t.HasCondition(Condition.Counter))
                {
                    var counterAtk = t.Stats.Atk * t.MultiplyBuffs("Attack");
                    var counterDef = user.Stats.Def * user.MultiplyBuffs("Defense") * user.IgnoreDefense;
                    var counterDamage = (uint)Global.RandomNumber(0, 4);
                    if (counterDef < counterAtk)
                        counterDamage += (uint)((counterAtk - counterDef) * user.DefensiveMult / 2);
                    log.Add($"{t.Name} strikes back!");
                    log.AddRange(user.DealDamage(counterDamage));
                }

                ii++;
            }

            user.AddDamage = 0;
            return log;
        }

        public override string ToString()
        {
            return
                $"Attack {(TargetType == TargetType.EnemyAll ? "all enemies" : Range == 1 ? "an enemy" : $"up to {Range * 2 - 1} enemies")} with a base damage of {Emotes.GetIcon(Element)} {(AttackBased ? "a normal physical Attack" : $"{Power}")}{(AddDamage > 0 ? $" plus an additional {AddDamage} Points" : "")}{(DmgMult != 1 ? $" multiplied by {DmgMult}" : "")}{(PercentageDamage > 0 ? $" + {PercentageDamage}% of the targets health as damage" : "")}.{(TargetType == TargetType.PartySelf || TargetType == TargetType.PartySingle || TargetType == TargetType.PartyAll ? "Target type set to hit your teammates! Probably an error..." : "")}";
        }
    }
}