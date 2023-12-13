using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Attack : Move
    {
        public Attack(string emote = "<:Attack:536919809393295381>")
        {
            Name = "Attack";
            Emote = emote;
            TargetType = TargetType.EnemyRange;
            Range = 1;
        }

        public override object Clone()
        {
            return new Attack();
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

        public override bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            if (user.Weapon != null) Emote = user.Weapon.Icon;

            var enemy = GetTarget(user).First();

            var log = new List<string>
            {
                $"{Emote} {user.Name} attacks!"
            };

            if (!enemy.IsAlive)
            {
                log.Add($"{enemy.Name} is down already!");
                if (user.Moves.FirstOrDefault(m => m is Defend) != null)
                    log.AddRange(user.Moves.FirstOrDefault(m => m is Defend).Use(user));
                return log;
            }

            var chanceToMiss = 16;
            if (user.HasCondition(Condition.Delusion)) chanceToMiss = 2;

            if (!enemy.HasCondition(Condition.Delusion) && enemy.Stats.Spd > 0 &&
                Global.RandomNumber(0, chanceToMiss) == 0)
            {
                log.Add($"{enemy.Name} dodges the blow!");
                return log;
            }

            var weaponUnleashed = user.Weapon is { IsUnleashable: true } && Global.RandomNumber(0, 100) <= user.UnleashRate;
            if (weaponUnleashed)
            {
                log.Add(
                    $"{user.Weapon.IconDisplay} {user.Name}'s {user.Weapon.Name} lets out a howl! {user.Weapon.Unleash.UnleashName}!");
                log.AddRange(user.Weapon.Unleash.AllEffects
                    .Where(e => e.ActivationTime == TimeToActivate.BeforeDamage)
                    .ApplyAll(user, enemy));
            }

            if (!enemy.IsAlive) return log;

            var atk = user.Stats.Atk * user.MultiplyBuffs("Attack");
            var def = enemy.Stats.Def * enemy.MultiplyBuffs("Defense") * enemy.IgnoreDefense;
            var damage = (uint)Global.RandomNumber(0, 5);
            if (def < atk) damage = (uint)((atk - def) / 2);
            damage += user.AddDamage;
            damage = (uint)(damage * user.OffensiveMult);

            var element = Element.None;
            if (user.Weapon != null) element = user.Weapon.DamageAlignment;

            if (weaponUnleashed) element = user.Weapon.Unleash.UnleashAlignment;

            var immuneTag = enemy.Tags.FirstOrDefault(t => t.StartsWith("ImmuneTo:"))?.Split(':').Skip(1).Select(Enum.Parse<Element>)
                ?? Array.Empty<Element>();

            if (immuneTag.Contains(element))
            {
                log.Add($"{enemy.Name} is unharmed.");
                return log;
            }

            //var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)enemy.elstats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400;
            var elMult = 1.0 + (user.ElStats.GetPower(element) * user.MultiplyBuffs("Power") -
                                enemy.ElStats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400.0;

            var punctuation = "!";
            if (enemy.ElStats.GetRes(element) == enemy.ElStats.HighestRes()) punctuation = ".";

            if (!weaponUnleashed && Global.RandomNumber(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage * 1.25 + Global.RandomNumber(5, 15));
            }

            if (enemy.ElStats.GetRes(element) == enemy.ElStats.LowestRes())
            {
                punctuation = "!!!";
                if (user is PlayerFighter p) p.BattleStats.AttackedWeakness++;
            }

            if (element == Element.None) punctuation = "!";

            damage = (uint)(damage * elMult);
            damage = (uint)(damage * enemy.DefensiveMult);
            damage = (uint)Math.Max(enemy.DefensiveMult <= 0 ? 0 : 1, damage);

            log.AddRange(enemy.DealDamage(damage, punctuation));

            user.DamageDoneThisTurn += damage;
            user.AddDamage = 0;
            if (weaponUnleashed)
                user.Weapon.Unleash.AllEffects
                    .Where(e => e.ActivationTime == TimeToActivate.AfterDamage)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(user, enemy)));

            if (enemy.IsAlive && enemy.HasCondition(Condition.Counter))
            {
                var counterAtk = enemy.Stats.Atk * enemy.MultiplyBuffs("Attack");
                var counterDef = user.Stats.Def * user.MultiplyBuffs("Defense") * user.IgnoreDefense;
                var counterDamage = (uint)Global.RandomNumber(0, 4);
                if (counterDef < counterAtk)
                    counterDamage = (uint)((counterAtk - counterDef) * user.DefensiveMult / 2);
                log.Add($"{enemy.Name} strikes back!");
                log.AddRange(user.DealDamage(counterDamage));
            }

            if (enemy.IsAlive && enemy.HasCondition(Condition.Trap))
            {
                var counterAtk = enemy.Stats.Atk * enemy.MultiplyBuffs("Attack");
                var counterDef = user.Stats.Def;
                var counterDamage = (uint)Global.RandomNumber(0, 4);
                if (counterDef < counterAtk)
                    counterDamage = (uint)((counterAtk - counterDef) * user.DefensiveMult / 2);
                log.Add("It was a Trap!");
                log.AddRange(user.DealDamage(counterDamage));
            }

            if (enemy.HasCondition(Condition.Key))
            {
                if (enemy.Party.Count(e => e.IsAlive && e.HasCondition(Condition.Key)) == 0)
                    enemy.Party.ForEach(e => e.Kill());
                log.Add("Your choice was correct!");
            }

            if (enemy.HasCondition(Condition.Decoy))
            {
                var counterDamage = (uint)(user.Stats.MaxHP * enemy.Stats.Atk / 100);
                var miniLog = new List<string>();
                miniLog.Add("It was a Trap!");
                miniLog.AddRange(user.DealDamage(counterDamage));
                enemy.EquipmentWithEffect.ForEach(i =>
                    i.Unleash.AllEffects.ForEach(e => miniLog.AddRange(e.Apply(enemy, user))));

                if (miniLog.Count == 2 && counterDamage == 0)
                    log.Add("A choice was made.");
                else
                    log.AddRange(miniLog);

                enemy.Party.ForEach(e => e.Kill());
            }

            if (user is not PlayerFighter player) return log;
            player.BattleStats.DamageDealt += damage;
            if (enemy.IsAlive) return log;
            var Battle = player.Battle;
            var killedByTags = enemy.Tags.Where(t => t.StartsWith("KilledBy"));
            // KilledBy:Player shouldnt need a check, its a white card
            // KilledBy:Venus:Mars:Djinn@2@3
            string[] MoveNames = { "Djinn", "Hand", "Summon", "Psynergy" };
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
                    if (ElementArgs.Any() && !ElementArgs.Contains(element))
                        continue;

                    if (MoveArgs.Any(MoveNames.Contains) && !MoveArgs.Contains("Hand"))
                        continue;

                    Battle.OutValue = BattleNumbers.Random();
                }
            }

            if (enemy.Stats.Spd > 0 && weaponUnleashed) player.BattleStats.KillsByHand++;
            player.BattleStats.Kills++;
            player.BattleStats.HighestDamage = Math.Max(player.BattleStats.HighestDamage, damage);

            return log;
        }
    }
}