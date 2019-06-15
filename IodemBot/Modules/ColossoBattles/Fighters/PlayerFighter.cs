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

        private static Stats baseStats = new Stats(30, 20, 11, 6, 8);
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
            if (classSeries.Name == "Curse Mage Series" || classSeries.Name == "Medium Series")
            {
                IsImmuneToItemCurse = true;
            }
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
                    if (g.GrantsUnleash)
                    {
                        if ((Weapon != null) && Weapon.IsUnleashable)
                        {
                            Weapon.Unleash.AdditionalEffects.AddRange(g.Unleash.Effects);
                        }
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
            var classMultipliers = adept.StatMultipliers;
            var level = avatar.LevelNumber;

            var Stats = new Stats(
                (int)(baseStats.MaxHP * (1 + 0.25 * level / 1.5)),
                (int)(baseStats.MaxPP * (1 + 0.115 * level / 1.5)),
                (int)(baseStats.Atk * (1 + 0.3 * level / 1.5)),
                (int)(baseStats.Def * (1 + 0.3 * level / 1.5)),
                (int)(baseStats.Spd * (1 + 0.5 * level / 1.5)));

            Stats *= classMultipliers;
            Stats *= 0.01;

            var gear = avatar.Inv.GetGear(classSeries.Archtype);
            gear.ForEach(g =>
            {
                Stats += g.AddStatsOnEquip;
            });

            gear.ForEach(g =>
            {
                Stats *= g.MultStatsOnEquip;
                Stats *= 0.01;
            });

            return Stats;
        }

        public override List<string> EndTurn()
        {
            selected = null;
            hasSelected = false;
            var log = new List<string>();

            if (AutoTurnsInARow >= 4 && IsAlive)
            {
                Kill();
                log.Add($":x: {name} dies from inactivity.");
                AutoTurnsInARow = 0;
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