using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Attack : Move
    {
        public Attack(string Emote = "<:Attack:536919809393295381>")
        {
            Name = "Attack";
            this.Emote = Emote;
            TargetType = Target.otherSingle;
            Range = 1;
        }

        public override object Clone()
        {
            return new Attack();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            var aliveEnemies = User.GetEnemies().Where(f => f.IsAlive).ToList();
            if (aliveEnemies.Count == 0)
            {
                TargetNr = 0;
                return;
            }
            TargetNr = User.GetEnemies().IndexOf(aliveEnemies.Random());
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            if (User.Weapon != null)
            {
                Emote = User.Weapon.Icon;
            }

            var enemy = GetTarget(User).First();

            var log = new List<string>
            {
                $"{Emote} {User.Name} attacks!"
            };

            if (!enemy.IsAlive)
            {
                log.Add($"{enemy.Name} is down already!");
                return log;
            }

            int chanceToMiss = 16;
            if (User.HasCondition(Condition.Delusion))
            {
                chanceToMiss = 2;
            }

            if (Global.Random.Next(0, chanceToMiss) == 0)
            {
                log.Add($"{enemy.Name} dodges the blow!");
                return log;
            }

            bool weaponUnleashed = User.Weapon != null && User.Weapon.IsUnleashable && Global.Random.Next(0, 100) <= User.unleashRate;
            if (weaponUnleashed)
            {
                log.Add($"{User.Weapon.IconDisplay} {User.Name}'s {User.Weapon.Name} lets out a howl! {User.Weapon.Unleash.UnleashName}!");
                User.Weapon.Unleash.AllEffects
                    .Where(e => e.ActivationTime == Effect.TimeToActivate.beforeDamge)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, enemy)));
            }

            if (!enemy.IsAlive)
            {
                return log;
            }

            var atk = User.Stats.Atk * User.MultiplyBuffs("Attack");
            var def = enemy.Stats.Def * enemy.MultiplyBuffs("Defense") * enemy.ignoreDefense;
            uint damage = (uint)Global.Random.Next(0, 4);
            if (def < atk)
            {
                damage = (uint)((atk - def) / 2);
            }
            damage += User.addDamage;
            damage = (uint)(damage * User.offensiveMult);

            var element = Element.none;
            if (User.Weapon != null)
            {
                element = User.Weapon.DamageAlignment;
            }

            if (weaponUnleashed)
            {
                element = User.Weapon.Unleash.UnleashAlignment;
            }

            //var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)enemy.elstats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400;
            var elMult = 1 + (User.ElStats.GetPower(element) * User.MultiplyBuffs("Power") - enemy.ElStats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400;

            var punctuation = "!";
            if (enemy.ElStats.GetRes(element) == enemy.ElStats.HighestRes())
            {
                punctuation = ".";
            }

            damage = (uint)(damage * elMult);
            damage = (uint)(damage * enemy.defensiveMult);

            if (enemy.ElStats.GetRes(element) == enemy.ElStats.LeastRes())
            {
                punctuation = "!!!";
                if (User is PlayerFighter p)
                {
                    p.battleStats.AttackedWeakness++;
                }
            }
            if (element == Element.none)
            {
                punctuation = "!";
            }

            User.addDamage = 0;
            if (!weaponUnleashed && Global.Random.Next(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage * 1.25 + Global.Random.Next(5, 15));
            }
            damage = Math.Max(1, damage);

            log.AddRange(enemy.DealDamage(damage, punctuation));
            User.damageDoneThisTurn += damage;
            if (weaponUnleashed)
            {
                User.Weapon.Unleash.AllEffects
                    .Where(e => e.ActivationTime == Effect.TimeToActivate.afterDamage)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, enemy)));
            }

            if (enemy.IsAlive && enemy.HasCondition(Condition.Counter))
            {
                var counterAtk = enemy.Stats.Atk * enemy.MultiplyBuffs("Attack");
                var counterDef = User.Stats.Def * User.MultiplyBuffs("Defense") * User.ignoreDefense;
                uint CounterDamage = (uint)Global.Random.Next(0, 4);
                if (counterDef < counterAtk)
                {
                    CounterDamage = (uint)((counterAtk - counterDef) * User.defensiveMult / 2);
                }
                log.Add($"{enemy.Name} strikes back!");
                log.AddRange(User.DealDamage(CounterDamage));
            }

            if (User is PlayerFighter player)
            {
                player.battleStats.DamageDealt += damage;
                if (!enemy.IsAlive)
                {
                    player.battleStats.KillsByHand++;
                    player.battleStats.Kills++;
                    player.battleStats.HighestDamage = Math.Max(player.battleStats.HighestDamage, damage);
                }
            }
            return log;
        }
    }
}