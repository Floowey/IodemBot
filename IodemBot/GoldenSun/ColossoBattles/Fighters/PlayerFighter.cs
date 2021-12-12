using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.ColossoBattles
{
    public class PlayerFighter : ColossoFighter
    {
        public int AutoTurnsInARow;

        public BattleStats BattleStats = new();
        [JsonIgnore] public PlayerFighterFactory Factory;
        [JsonIgnore] public ulong Id;

        public override List<string> EndTurn()
        {
            SelectedMove = null;
            HasSelected = false;
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
            return Factory.CreatePlayerFighter(UserAccountProvider.GetById(Id));
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

        public Stats GetStats(uint level, double reductionFactor = 1.25)
        {
            return BaseStat + (FinalBaseStats - BaseStat) * ((double)level / 99 / reductionFactor);
        }
    }

    public class PlayerFighterFactory
    {
        private static readonly StatHolder WarriorStatHolder =
            new(new Stats(31, 19, 12, 7, 7), new Stats(807, 245, 381, 171, 371));

        private static readonly StatHolder MageStatHolder =
            new(new Stats(28, 23, 9, 5, 9), new Stats(744, 280, 355, 160, 397));

        private static readonly StatHolder AverageStatHolder =
            new(new Stats(30, 20, 11, 6, 8), new Stats(775, 262, 368, 165, 384));

        public List<Djinn> Djinn = new();

        public ElementalStats ElStatMultiplier = new(100);

        public double ReductionFactor = 1.25;
        public List<Summon> Summons = new();

        public LevelOption LevelOption { get; set; } = LevelOption.CappedLevel;
        public InventoryOption InventoryOption { get; set; } = InventoryOption.Default;
        public DjinnOption DjinnOption { get; set; } = DjinnOption.Any;
        public BaseStatOption BaseStatOption { get; set; } = BaseStatOption.Default;
        public BaseStatManipulationOption BaseStatManipulationOption { get; set; } = BaseStatManipulationOption.Default;
        public List<Summon> PossibleSummons => Summons.Where(s => s.CanSummon(Djinn)).Distinct().ToList();
        public uint SetLevel { get; set; } = 99;
        public Stats StatMultiplier { get; set; } = new(100, 100, 100, 100, 100);

        public PlayerFighter CreatePlayerFighter(UserAccount avatar)
        {
            var p = new PlayerFighter
            {
                Name = avatar.Name,
                ImgUrl = avatar.ImgUrl,
                Factory = this,
                Id = avatar.Id,
                Moves = AdeptClassSeriesManager.GetMoveset(avatar)
            };
            var @class = AdeptClassSeriesManager.GetClass(avatar);
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            if (classSeries.Name == "Curse Mage Series" || classSeries.Name == "Medium Series")
                p.IsImmuneToItemCurse = true;

            var level = LevelOption switch
            {
                LevelOption.SetLevel => SetLevel,
                LevelOption.CappedLevel => Math.Min(avatar.LevelNumber, SetLevel),
                _ => avatar.LevelNumber
            };

            p.Stats = GetStats(avatar, level);
            p.ElStats = AdeptClassSeriesManager.GetElStats(avatar);

            switch (InventoryOption)
            {
                case InventoryOption.Default:
                    var gear = avatar.Inv.GetGear(classSeries.Archtype);
                    gear.ForEach(g => { p.Stats += g.AddStatsOnEquip; });
                    gear.ForEach(g => { p.ElStats += g.AddElStatsOnEquip; });
                    gear.ForEach(g =>
                    {
                        p.Stats *= g.MultStatsOnEquip;
                        p.Stats *= 0.01;
                    });

                    gear.OrderBy(i => i.ItemType).ToList().ForEach(g =>
                    {
                        p.HPrecovery += g.HpRegen;
                        p.PPrecovery += g.PpRegen;
                        p.UnleashRate += g.IncreaseUnleashRate;
                        if (g.IsCursed) p.AddCondition(Condition.ItemCurse);

                        if (g.CuresCurse) p.IsImmuneToItemCurse = true;

                        if (g.Category == ItemCategory.Weapon)
                        {
                            p.Weapon = g;
                            if (p.Weapon.IsUnleashable) p.Weapon.Unleash.AdditionalEffects.Clear();
                        }
                        else if (g.IsUnleashable)
                        {
                            if (g.GrantsUnleash)
                            {
                                if (p.Weapon is { IsUnleashable: true })
                                    p.Weapon.Unleash.AdditionalEffects.AddRange(g.Unleash.AllEffects);
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
                    Djinn.AddRange(djinnToAdd);
                    Summons.AddRange(avatar.DjinnPocket.Summons);
                    p.Moves.AddRange(djinnToAdd);
                    foreach (var djinn in djinnToAdd)
                    {
                        p.Stats *= djinn.Stats + new Stats(100, 100, 100, 100, 100);
                        p.Stats *= 0.01;
                        p.ElStats += djinn.ElementalStats * Math.Max(1, level / 10f);
                    }

                    break;

                case DjinnOption.Unique:
                    var djinnToBeAdded = avatar.DjinnPocket.GetDjinns(Djinn);
                    Djinn.AddRange(djinnToBeAdded);
                    Summons.AddRange(avatar.DjinnPocket.Summons);
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

            p.Stats *= @class.StatMultipliers;
            p.Stats *= 0.01;
            p.Stats *= StatMultiplier;
            p.Stats *= 0.01;

            return p;
        }

        private Stats GetStats(UserAccount avatar, uint level)
        {
            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);

            var stats = BaseStatOption switch
            {
                BaseStatOption.Default => classSeries.Archtype == ArchType.Warrior
                    ? WarriorStatHolder.GetStats(level, ReductionFactor)
                    : MageStatHolder.GetStats(level, ReductionFactor),
                _ => AverageStatHolder.GetStats(level)
            };
            return stats;
        }
    }
}