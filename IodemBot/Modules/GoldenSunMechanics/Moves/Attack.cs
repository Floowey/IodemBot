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
            var aliveEnemies = User.getEnemies().Where(f => f.IsAlive()).ToList();
            if (aliveEnemies.Count == 0)
            {
                targetNr = 0;
                return;
            }
            targetNr = User.getEnemies().IndexOf(aliveEnemies[Global.random.Next(0, aliveEnemies.Count)]);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            var enemy = User.battle.getTeam(User.enemies)[targetNr];

            var log = new List<string>();
            log.Add($"{emote} {User.name} attacks!");
            if (!enemy.IsAlive())
            {
                log.Add($"{enemy.name} is down already!");
                return log;
            }

            bool weaponUnleashed = User.Weapon != null && User.Weapon.IsUnleashable && Global.random.Next(0, 100) <= User.unleashRate;

            if (weaponUnleashed)
            {
                log.Add($"{User.Weapon.Icon} {User.name}'s {User.Weapon.Name} lets out a howl! {User.Weapon.unleash.UnleashName}!");
                User.Weapon.unleash.effects
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

                if (Global.random.Next(0, chanceToMiss) == 0)
                {
                    log.Add($"{enemy.name} dodges the blow!");
                    return log;
                }
            }

            var atk = User.stats.Atk * User.MultiplyBuffs("Attack");
            var def = enemy.stats.Def * enemy.MultiplyBuffs("Defense");
            uint damage = 1;
            if (def < atk)
            {
                damage = (uint)((atk - def) * enemy.defensiveMult / 2 + (uint)Global.random.Next(1, 4));
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
                element = User.Weapon.unleash.UnleashAlignment;
            }

            var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)enemy.elstats.GetRes(element) * enemy.MultiplyBuffs("Resistance")) / 400;
            var punctuation = "!";
            if (enemy.elstats.GetRes(element) == enemy.elstats.highestRes())
            {
                punctuation = ".";
            }

            damage = (uint)(damage * elMult);

            if (enemy.elstats.GetRes(element) == enemy.elstats.leastRes())
            {
                punctuation = "!!!";
                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).battleStats.attackedWeakness++;
                }
            }
            if (element == Psynergy.Element.none)
            {
                punctuation = "!";
            }

            User.addDamage = 0;
            if (!weaponUnleashed && Global.random.Next(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage * 1.25 + Global.random.Next(5, 15));
            }
            if (damage == 0)
            {
                damage = 1;
            }

            log.AddRange(enemy.DealDamage(damage, punctuation));
            User.damageDoneThisTurn += damage;
            if (weaponUnleashed)
            {
                User.Weapon.unleash.effects
                    .Where(e => e.timeToActivate == IEffect.TimeToActivate.afterDamage)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, enemy)));
            }

            if (User is PlayerFighter)
            {
                var player = (PlayerFighter)User;
                player.battleStats.damageDealt += damage;
                if (!enemy.IsAlive())
                {
                    player.battleStats.killsByHand++;
                    player.battleStats.kills++;
                }
            }
            return log;
        }
    }
}