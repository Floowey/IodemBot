using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using System.Collections.Generic;

namespace IodemBot.Modules.ColossoBattles
{
    public class PlayerFighter : ColossoFighter
    {
        public UserAccount avatar;
        private SocketGuildUser guildUser;

        private static Stats baseStats = new Stats(30, 20, 11, 6, 8); //30, 20, 11, 6, 8
        public BattleStats battleStats = new BattleStats();
        public int AutoTurnPool = 10;
        public int AutoTurnsInARow = 0;

        public PlayerFighter(SocketGuildUser user) : base(user.DisplayName(), user.GetAvatarUrl(),
            ModifyStats(user),
            AdeptClassSeriesManager.getElStats(UserAccounts.GetAccount(user)),
            AdeptClassSeriesManager.getMoveset(UserAccounts.GetAccount(user)))
        {
            avatar = UserAccounts.GetAccount(user);
            guildUser = user;

            var classSeries = AdeptClassSeriesManager.getClassSeries(avatar);
            var gear = avatar.inv.GetGear(classSeries.archtype);
            gear.ForEach(g =>
            {
                HPrecovery += g.HPRegen;
                PPrecovery += g.PPRegen;
                unleashRate += g.increaseUnleashRate;
                if (g.IsCursed)
                {
                    AddCondition(Condition.ItemCurse);
                }

                if (g.CuresCurse)
                {
                    isImmuneToItemCurse = true;
                }

                if (g.IsWeapon)
                {
                    Weapon = g;
                }

                if (!g.IsWeapon && g.IsUnleashable && !g.GrantsUnleash)
                {
                    if (g.GrantsUnleash)
                    {
                        Weapon.unleash.effects.AddRange(g.unleash.effects);
                    }
                    else
                    {
                        EquipmentWithEffect.Add(g);
                    }
                }
            });
        }

        private static Stats ModifyStats(SocketGuildUser user)
        {
            var avatar = UserAccounts.GetAccount(user);
            var classSeries = AdeptClassSeriesManager.getClassSeries(avatar);
            var adept = AdeptClassSeriesManager.getClass(avatar);
            var multipliers = adept.statMultipliers;
            var level = avatar.LevelNumber;

            var actualStats = new Stats(
                (int)((baseStats.maxHP + baseStats.maxHP * 0.25 * level / 1.5) * multipliers.maxHP / 100),
                (int)((baseStats.maxPP + baseStats.maxPP * 0.115 * level / 1.5) * multipliers.maxPP / 100),
                (int)((baseStats.Atk + baseStats.Atk * 0.3 * level / 1.5) * multipliers.Atk / 100),
                (int)((baseStats.Def + baseStats.Def * 0.3 * level / 1.5) * multipliers.Def / 100),
                (int)((baseStats.Spd + baseStats.Spd * 0.5 * level / 1.5) * multipliers.Spd / 100));

            var gear = avatar.inv.GetGear(classSeries.archtype);
            gear.ForEach(g =>
            {
                actualStats += g.AddStatsOnEquip;
            });

            gear.ForEach(g =>
            {
                actualStats *= g.MultStatsOnEquip;
                actualStats *= 0.01;
            });

            return actualStats;
        }

        public override List<string> EndTurn()
        {
            selected = null;
            hasSelected = false;
            var log = new List<string>();

            if (AutoTurnsInARow >= 4)
            {
                Kill();
                log.Add($":x: {name} dies from inactivity.");
            }

            log.AddRange(base.EndTurn());
            return log;
        }

        public override object Clone()
        {
            return new PlayerFighter(guildUser);
        }
    }
}