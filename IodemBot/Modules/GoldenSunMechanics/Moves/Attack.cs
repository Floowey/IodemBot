using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Attack : Move
    {
        public Attack() : base("Attack", "<:Attack:536919809393295381>", Target.otherSingle, 1, new List<EffectImage>())
        {
        }

        public override object Clone()
        {
            return new Attack();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            var aliveEnemies = User.GetEnemies().Where(f => f.IsAlive()).ToList();
            if (aliveEnemies.Count == 0)
            {
                targetNr = 0;
                return;
            }
            targetNr = User.GetEnemies().IndexOf(aliveEnemies[Global.Random.Next(0, aliveEnemies.Count)]);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            if (User.Weapon != null)
            {
                emote = User.Weapon.Icon;
            }

            var enemy = User.battle.GetTeam(User.enemies)[targetNr];

            var log = new List<string>
            {
                $"{emote} {User.name} attacks!"
            };

            if (!enemy.IsAlive())
            {
                log.Add($"{enemy.name} is down already!");
                return log;
            }

            bool weaponUnleashed = User.Weapon != null && User.Weapon.IsUnleashable && Global.Random.Next(0, 100) <= User.unleashRate;

            if (weaponUnleashed)
            {
                log.Add($"{User.Weapon.IconDisplay} {User.name}'s {User.Weapon.Name} lets out a howl! {User.Weapon.Unleash.UnleashName}!");
                User.Weapon.Unleash.Effects
                    .Where(e => e.timeToActivate == IEffect.TimeToActivate.beforeDamge)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, enemy)));
            }
            else
            {
                int chanceToMiss = 8;
                if (User.HasCondition(Condition.Delusion))
                {
                    chanceToMiss = 3;
                }

                if (Global.Random.Next(0, chanceToMiss) == 0)
                {
                    log.Add($"{enemy.name} dodges the blow!");
                    return log;
                }
            }

            if (!enemy.IsAlive())
            {
                return log;
            }

            var atk = User.stats.Atk * User.MultiplyBuffs("Attack");
            var def = enemy.stats.Def * enemy.MultiplyBuffs("Defense") * enemy.ignoreDefense;
            uint damage = 1;
            if (def < atk)
            {
                damage = (uint)((atk - def) * enemy.defensiveMult / 2 + (uint)Global.Random.Next(1, 4));
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

            var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)enemy.elstats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400;
            var punctuation = "!";
            if (enemy.elstats.GetRes(element) == enemy.elstats.HighestRes())
            {
                punctuation = ".";
            }

            damage = (uint)(damage * elMult);

            if (enemy.elstats.GetRes(element) == enemy.elstats.LeastRes())
            {
                punctuation = "!!!";
                if (User is PlayerFighter p)
                {
                    p.battleStats.AttackedWeakness++;
                }
            }
            if (element == Psynergy.Element.none)
            {
                punctuation = "!";
            }

            User.addDamage = 0;
            if (!weaponUnleashed && Global.Random.Next(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage * 1.25 + Global.Random.Next(5, 15));
            }
            if (damage == 0)
            {
                damage = 1;
            }

            log.AddRange(enemy.DealDamage(damage, punctuation));
            User.damageDoneThisTurn += damage;
            if (weaponUnleashed)
            {
                User.Weapon.Unleash.Effects
                    .Where(e => e.timeToActivate == IEffect.TimeToActivate.afterDamage)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, enemy)));
            }

            if (User is PlayerFighter player)
            {
                player.battleStats.DamageDealt += damage;
                if (!enemy.IsAlive())
                {
                    player.battleStats.KillsByHand++;
                    player.battleStats.Kills++;
                }
            }
            return log;
        }
    }
}