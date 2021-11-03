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
            var damage = (uint)Global.RandomNumber(0, 4);
            if (def < atk) damage = (uint)((atk - def) / 2);
            damage += user.AddDamage;
            damage = (uint)(damage * user.OffensiveMult);

            var element = Element.None;
            if (user.Weapon != null) element = user.Weapon.DamageAlignment;

            if (weaponUnleashed) element = user.Weapon.Unleash.UnleashAlignment;

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

            if (enemy.ElStats.GetRes(element) == enemy.ElStats.LeastRes())
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
                log.Add("It was a Trap!");
                log.AddRange(user.DealDamage(counterDamage));
                enemy.EquipmentWithEffect.ForEach(i =>
                    i.Unleash.AllEffects.ForEach(e => log.AddRange(e.Apply(enemy, user))));
                enemy.Party.ForEach(e => e.Kill());
            }

            if (user is PlayerFighter player)
            {
                player.BattleStats.DamageDealt += damage;
                if (!enemy.IsAlive)
                {
                    if (enemy.Stats.Spd > 0 && weaponUnleashed) player.BattleStats.KillsByHand++;
                    player.BattleStats.Kills++;
                    player.BattleStats.HighestDamage = Math.Max(player.BattleStats.HighestDamage, damage);
                }
            }

            return log;
        }
    }
}