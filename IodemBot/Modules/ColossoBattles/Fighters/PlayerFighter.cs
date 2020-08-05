using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

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

    internal class StatHolder
    {
        public Stats BaseStat;
        public Stats FinalBaseStats;

        public StatHolder(Stats baseStat, Stats finalBaseStats)
        {
            BaseStat = baseStat;
            FinalBaseStats = finalBaseStats;
        }

        public Stats GetStats(uint Level, double ReductionFactor = 1.25)
        {
            return BaseStat + (FinalBaseStats - BaseStat) * ((double)Level / 99 / ReductionFactor);
        }
    }

    public class PlayerFighterFactory
    {
        private static readonly StatHolder WarriorStatHolder = new StatHolder(new Stats(31, 19, 12, 7, 7), new Stats(807, 245, 381, 171, 371));
        private static readonly StatHolder MageStatHolder = new StatHolder(new Stats(28, 23, 9, 5, 9), new Stats(744, 280, 355, 160, 397));
        private static readonly StatHolder AverageStatHolder = new StatHolder(new Stats(30, 20, 11, 6, 8), new Stats(775, 262, 368, 165, 384));

        public LevelOption LevelOption { get; set; } = LevelOption.CappedLevel;
        public InventoryOption InventoryOption { get; set; } = InventoryOption.Default;
        public DjinnOption DjinnOption { get; set; } = DjinnOption.Unique;
        public BaseStatOption BaseStatOption { get; set; } = BaseStatOption.Default;
        public BaseStatManipulationOption BaseStatManipulationOption { get; set; } = BaseStatManipulationOption.Default;

        public List<Djinn> uniqueDjinn = new List<Djinn>();
        public List<Summon> summons = new List<Summon>();
        public List<Summon> PossibleSummons { get => summons.Where(s => s.CanSummon(uniqueDjinn)).Distinct().ToList(); }

        public double ReductionFactor = 1.25;
        public uint SetLevel { get; set; } = 99;
        public Stats StatMultiplier { get; set; } = new Stats(100, 100, 100, 100, 100);

        public ElementalStats ElStatMultiplier = new ElementalStats(100, 100, 100, 100, 100, 100, 100, 100);

        public PlayerFighter CreatePlayerFighter(SocketUser user)
        {
            var p = new PlayerFighter();
            var avatar = UserAccounts.GetAccount(user);

            p.Name = (user is SocketGuildUser u) ? u.DisplayName() : user.Username;
            p.avatar = avatar;
            p.ImgUrl = user.GetAvatarUrl();
            p.factory = this;
            if (user is SocketGuildUser gu)
            {
                p.guildUser = gu;
            }
            p.Moves = AdeptClassSeriesManager.GetMoveset(avatar);
            var Class = AdeptClassSeriesManager.GetClass(avatar);
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            if (classSeries.Name == "Curse Mage Series" || classSeries.Name == "Medium Series")
            {
                p.IsImmuneToItemCurse = true;
            }

            var level = LevelOption switch
            {
                LevelOption.SetLevel => SetLevel,
                LevelOption.CappedLevel => Math.Min(avatar.LevelNumber, SetLevel),
                _ => avatar.LevelNumber,
            };

            p.Stats = GetStats(avatar, level);
            p.ElStats = AdeptClassSeriesManager.GetElStats(avatar);

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
                                    p.Weapon.Unleash.AdditionalEffects.AddRange(g.Unleash.AllEffects);
                                }
                            }
                            else
                            {
                                p.EquipmentWithEffect.Add(g);
                            }
                        }
                    });
                    p.HPrecovery = (int)(p.HPrecovery * (1 + (double)level / 33));

                    break;

                case InventoryOption.NoInventory:
                    break;
            }

            switch (DjinnOption)
            {
                case DjinnOption.Any:
                    var djinnToAdd = avatar.DjinnPocket.GetDjinns();
                    summons.AddRange(avatar.DjinnPocket.summons);
                    p.Moves.AddRange(djinnToAdd);
                    foreach (var djinn in djinnToAdd)
                    {
                        p.Stats *= djinn.Stats + new Stats(100, 100, 100, 100, 100);
                        p.Stats *= 0.01;
                        p.ElStats += djinn.ElementalStats;
                    }
                    break;
                case DjinnOption.Unique:
                    var djinnToBeAdded = avatar.DjinnPocket.GetDjinns(uniqueDjinn);
                    uniqueDjinn.AddRange(djinnToBeAdded);
                    summons.AddRange(avatar.DjinnPocket.summons);
                    p.Moves.AddRange(djinnToBeAdded);
                    foreach (var djinn in djinnToBeAdded)
                    {
                        p.Stats *= djinn.Stats + new Stats(100, 100, 100, 100, 100);
                        p.Stats *= 0.01;
                        p.ElStats += djinn.ElementalStats;
                    }
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

        private Stats GetStats(UserAccount avatar, uint level)
        {
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
           
            Stats Stats = BaseStatOption switch
            {
                BaseStatOption.Default => classSeries.Archtype == ArchType.Warrior ? WarriorStatHolder.GetStats(level, ReductionFactor) : MageStatHolder.GetStats(level, ReductionFactor),
                _ => AverageStatHolder.GetStats(level),
            };
            return Stats;
        }
    }
}