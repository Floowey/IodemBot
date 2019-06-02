using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.ColossoBattles
{
    public class PlayerFighter : ColossoFighter
    {
        public UserAccount avatar;
        private readonly SocketGuildUser guildUser;

        private static Stats baseStats = new Stats(30, 20, 11, 6, 8); //30, 20, 11, 6, 8
        public BattleStats battleStats = new BattleStats();
        public int AutoTurnPool = 10;
        public int AutoTurnsInARow = 0;

        public PlayerFighter(SocketGuildUser user) : base(user.DisplayName(), user.GetAvatarUrl(),
            ModifyStats(user),
            AdeptClassSeriesManager.GetElStats(UserAccounts.GetAccount(user)),
            AdeptClassSeriesManager.GetMoveset(UserAccounts.GetAccount(user)))
        {
            avatar = UserAccounts.GetAccount(user);
            guildUser = user;

            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            var gear = avatar.Inv.GetGear(classSeries.Archtype);
            gear.OrderBy(i => i.ItemType).ToList().ForEach(g =>
            {
                HPrecovery += g.HPRegen;
                PPrecovery += g.PPRegen;
                unleashRate += g.IncreaseUnleashRate;
                if (g.IsCursed)
                {
                    AddCondition(Condition.ItemCurse);
                }

                if (g.CuresCurse)
                {
                    IsImmuneToItemCurse = true;
                }

                if (g.IsWeapon)
                {
                    Weapon = g;
                    if (Weapon.IsUnleashable)
                    {
                        Weapon.Unleash.AdditionalEffects.Clear();
                    }
                }

                if (!g.IsWeapon && g.IsUnleashable)
                {
                    if (g.GrantsUnleash && (Weapon != null) && Weapon.IsUnleashable)
                    {
                        Weapon.Unleash.AdditionalEffects.AddRange(g.Unleash.Effects);
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
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            var adept = AdeptClassSeriesManager.GetClass(avatar);
            var multipliers = adept.StatMultipliers;
            var level = avatar.LevelNumber;

            var actualStats = new Stats(
                (int)((baseStats.MaxHP + baseStats.MaxHP * 0.25 * level / 1.5) * multipliers.MaxHP / 100),
                (int)((baseStats.MaxPP + baseStats.MaxPP * 0.115 * level / 1.5) * multipliers.MaxPP / 100),
                (int)((baseStats.Atk + baseStats.Atk * 0.3 * level / 1.5) * multipliers.Atk / 100),
                (int)((baseStats.Def + baseStats.Def * 0.3 * level / 1.5) * multipliers.Def / 100),
                (int)((baseStats.Spd + baseStats.Spd * 0.5 * level / 1.5) * multipliers.Spd / 100));

            var gear = avatar.Inv.GetGear(classSeries.Archtype);
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