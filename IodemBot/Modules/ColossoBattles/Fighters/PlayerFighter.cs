using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.ColossoBattles
{
    public class PlayerFighter : ColossoFighter
    {
        [JsonIgnore] public UserAccount avatar;
        [JsonIgnore] public SocketGuildUser guildUser;
        [JsonIgnore] public PlayerFighterFactory factory;

        public BattleStats battleStats = new BattleStats();
        public int AutoTurnPool = 10;
        public int AutoTurnsInARow = 0;

        public PlayerFighter() : base()
        {
        }

        public override List<string> EndTurn()
        {
            selected = null;
            hasSelected = false;
            var log = new List<string>();

            if (AutoTurnsInARow >= 4 && IsAlive)
            {
                Kill();
                log.Add($":x: {Name} dies from inactivity.");
                AutoTurnsInARow = 0;
            }

            log.AddRange(base.EndTurn());
            return log;
        }

        public override object Clone()
        {
            return factory.CreatePlayerFighter(guildUser);
        }
    }

    public enum LevelOption { Default, SetLevel, CappedLevel }

    public enum InventoryOption { Default, NoInventory }

    public enum DjinnOption { Default, NoDjinn }

    public enum BaseStatOption { Default, Average }

    public enum BaseStatManipulationOption { Default, NoIncrease }

    internal class StatHolder
    {
        public Stats BaseStat;
        public Stats FinalBaseStats;

        public StatHolder(Stats baseStat, Stats finalBaseStats)
        {
            BaseStat = baseStat;
            FinalBaseStats = finalBaseStats;
        }

        public Stats GetStats(uint Level)
        {
            return BaseStat + (FinalBaseStats - BaseStat) * ((double)Level / 99 / 1.5);
        }
    }

    public class PlayerFighterFactory
    {
        private static StatHolder WarriorStatHolder = new StatHolder(new Stats(31, 19, 12, 7, 7), new Stats(807, 245, 381, 171, 371));
        private static StatHolder MageStatHolder = new StatHolder(new Stats(28, 23, 9, 5, 9), new Stats(744, 280, 355, 160, 397));
        private static StatHolder AverageStatHolder = new StatHolder(new Stats(30, 20, 11, 6, 8), new Stats(775, 262, 368, 165, 384));

        public LevelOption LevelOption { get; set; } = LevelOption.CappedLevel;
        public InventoryOption InventoryOption { get; set; } = InventoryOption.Default;
        public DjinnOption DjinnOption { get; set; } = DjinnOption.Default;
        public BaseStatOption BaseStatOption { get; set; } = BaseStatOption.Default;
        public BaseStatManipulationOption BaseStatManipulationOption { get; set; } = BaseStatManipulationOption.Default;

        public uint SetLevel { get; set; } = 100;
        public Stats StatMultiplier { get; set; } = new Stats(100, 100, 100, 100, 100);

        public ElementalStats ElStatMultiplier = new ElementalStats(100, 100, 100, 100, 100, 100, 100, 100);

        public PlayerFighter CreatePlayerFighter(SocketUser user)
        {
            var p = new PlayerFighter();
            var avatar = UserAccounts.GetAccount(user);

            p.Name = (user is SocketGuildUser) ? ((SocketGuildUser)user).DisplayName() : user.Username;
            p.avatar = avatar;
            p.ImgUrl = user.GetAvatarUrl();
            p.factory = this;
            if (user is SocketGuildUser)
            {
                p.guildUser = (SocketGuildUser)user;
            }
            p.Moves = AdeptClassSeriesManager.GetMoveset(avatar);

            var Class = AdeptClassSeriesManager.GetClass(avatar);
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            p.Stats = GetStats(avatar);
            p.ElStats = AdeptClassSeriesManager.GetElStats(avatar);
            if (classSeries.Name == "Curse Mage Series" || classSeries.Name == "Medium Series")
            {
                p.IsImmuneToItemCurse = true;
            }

            switch (InventoryOption)
            {
                case InventoryOption.Default:
                    var gear = avatar.Inv.GetGear(classSeries.Archtype);
                    gear.ForEach(g =>
                    {
                        p.Stats += g.AddStatsOnEquip;
                    });
                    gear.ForEach(g =>
                    {
                        p.ElStats += g.AddElStatsOnEquip;
                    });
                    gear.ForEach(g =>
                    {
                        p.Stats *= g.MultStatsOnEquip;
                        p.Stats *= 0.01;
                    });

                    gear.OrderBy(i => i.ItemType).ToList().ForEach(g =>
                    {
                        p.HPrecovery += g.HPRegen;
                        p.PPrecovery += g.PPRegen;
                        p.unleashRate += g.IncreaseUnleashRate;
                        if (g.IsCursed)
                        {
                            p.AddCondition(Condition.ItemCurse);
                        }

                        if (g.CuresCurse)
                        {
                            p.IsImmuneToItemCurse = true;
                        }

                        if (g.Category == ItemCategory.Weapon)
                        {
                            p.Weapon = g;
                            if (p.Weapon.IsUnleashable)
                            {
                                p.Weapon.Unleash.AdditionalEffects.Clear();
                            }
                        }
                        else if (g.IsUnleashable)
                        {
                            if (g.GrantsUnleash)
                            {
                                if ((p.Weapon != null) && p.Weapon.IsUnleashable)
                                {
                                    p.Weapon.Unleash.AdditionalEffects.AddRange(g.Unleash.Effects);
                                }
                            }
                            else
                            {
                                p.EquipmentWithEffect.Add(g);
                            }
                        }
                    });
                    p.HPrecovery = (int)(p.HPrecovery * (1 + (double)avatar.LevelNumber / 33));

                    break;

                case InventoryOption.NoInventory:
                    break;
            }

            switch (DjinnOption)
            {
                case DjinnOption.Default:
                    break;

                case DjinnOption.NoDjinn:
                    break;
            }

            p.Stats *= Class.StatMultipliers;
            p.Stats *= 0.01;
            p.Stats *= StatMultiplier;
            p.Stats *= 0.01;

            return p;
        }

        private Stats GetStats(UserAccount avatar)
        {
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            var adept = AdeptClassSeriesManager.GetClass(avatar);
            var classMultipliers = adept.StatMultipliers;
            uint level;
            switch (LevelOption)
            {
                default:
                case LevelOption.Default:
                    level = avatar.LevelNumber;
                    break;

                case LevelOption.SetLevel:
                    level = SetLevel;
                    break;

                case LevelOption.CappedLevel:
                    level = Math.Min(avatar.LevelNumber, SetLevel);
                    break;
            };
            Stats Stats;
            switch (BaseStatOption)
            {
                case BaseStatOption.Default:
                    Stats = classSeries.Archtype == ArchType.Warrior ? WarriorStatHolder.GetStats(level) : MageStatHolder.GetStats(level);
                    break;

                case BaseStatOption.Average:
                default:
                    Stats = AverageStatHolder.GetStats(level);
                    break;
            }
            return Stats;
        }
    }
}