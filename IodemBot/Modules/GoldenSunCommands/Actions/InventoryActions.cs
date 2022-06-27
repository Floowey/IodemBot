using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    // Missing:
    // Remove Cursed Action (either in inv or gear)
    // Sell Action (button also on chests)
    // Iteminfo (With equip button where applicable)
    public class InventoryAction : IodemBotCommandAction
    {
        private static readonly Dictionary<Detail, char> Split = new()
        {
            { Detail.None, '>' },
            { Detail.Names, ',' },
            { Detail.NameAndPrice, '\n' }
        };

        public override bool GuildsOnly => false;

        [ActionParameterComponent(Order = 0, Name = "Detail", Description = "...", Required = false)]
        public Detail Detail { get; set; } = Detail.None;

        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "inv",
            Description = "Manage your Inventory"
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (stringOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    Detail = Enum.Parse<Detail>((string)idOptions.FirstOrDefault());

                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetInventoryEmbed(account, Detail);
            msgProps.Components = GetInventoryComponent(account, Detail);
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetInventoryEmbed(account, Detail);
            var component = GetInventoryComponent(account, Detail);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }

        internal static Embed GetInventoryEmbed(UserAccount account, Detail detail = Detail.None)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder()
                .AddField("Warrior Gear", string.Concat(inv.GearToString(ArchType.Warrior).Prepend('\u200B')), true)
                .AddField("Mage Gear", string.Concat(inv.GearToString(ArchType.Mage).Prepend('\u200B')), true);

            var invstring = string.Concat(inv.InventoryToString(detail).Prepend('\u200B'));
            if (invstring.Length >= 1024)
            {
                var remainingstring = invstring;
                var parts = new List<string>();
                while (remainingstring.Length > 1024)
                {
                    var lastitem = remainingstring.Take(1020).ToList().FindLastIndex(s => s.Equals(Split[detail])) + 1;
                    parts.Add(string.Concat(remainingstring.Take(lastitem)));
                    remainingstring = string.Concat(remainingstring.Skip(lastitem).Prepend('\u200B'));
                }

                parts.Add(remainingstring);
                foreach (var (value, index) in parts.Select((v, i) => (v, i)))
                    embed.AddField($"Inventory ({index + 1}/{parts.Count})", value);
            }
            else
            {
                embed.AddField("Inventory", invstring);
            }

            if (inv.GetChestsToString().Length > 0) embed.AddField("Chests:", inv.GetChestsToString());

            var fb = new EmbedFooterBuilder();
            var upgradeCost = (int)(50000 * Math.Pow(2, inv.Upgrades));
            fb.WithText($"{inv.Count} / {inv.MaxInvSize} {(inv.Upgrades < 4 ? $"Upgrade: {upgradeCost}" : "")}");
            embed.AddField("Coin", $"{Emotes.GetIcon("Coin")} {inv.Coins}", true);
            embed.AddField("Game Tickets", $"{Emotes.GetIcon("GameTicket")} {inv.GameTickets}", true);
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithFooter(fb);
            return embed.Build();
        }

        internal static MessageComponent GetInventoryComponent(UserAccount account, Detail detail = Detail.None)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            var labels = account.Preferences.ShowButtonLabels;
            var upgradeCost = (uint)(50000 * Math.Pow(2, inv.Upgrades));
            //add status menu button
            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", ButtonStyle.Primary,
                Emotes.GetEmote("StatusAction"));
            builder.WithButton(labels ? "Warrior Gear" : null, $"#{nameof(GearAction)}.Warrior",
                emote: Emotes.GetEmote("Warrior"), style: ButtonStyle.Success);
            builder.WithButton(labels ? "Mage Gear" : null, $"#{nameof(GearAction)}.Mage",
                emote: Emotes.GetEmote("Mage"), style: ButtonStyle.Success);
            var chest = inv.HasAnyChests() ? inv.NextChestQuality() : ChestQuality.Normal;
            builder.WithButton(labels ? "Open Chest" : null, $"#{nameof(ChestAction)}.{chest}", ButtonStyle.Success,
                Emotes.GetEmote(chest), disabled: !inv.HasAnyChests());
            builder.WithButton(labels ? "Sell" : null, $"{nameof(SellItemComponent)}", ButtonStyle.Success,
                Emotes.GetEmote("SellAction"));
            if (inv.Upgrades < 4)
                builder.WithButton(labels ? "Upgrade" : null, $"{nameof(UpgradeInventory)}", ButtonStyle.Success,
                    Emotes.GetEmote("UpgradeInventoryAction"), disabled: !inv.HasBalance(upgradeCost), row: 1);

            builder.WithButton(labels ? "Sort" : null, $"{nameof(SortInventory)}", ButtonStyle.Success,
                Emotes.GetEmote("SortInventoryAction"), row: 1);
            builder.WithButton(labels ? "Shop" : null, $"#{nameof(ShopAction)}.", ButtonStyle.Success,
                Emotes.GetEmote("ShopAction"), row: 1);
            if (detail == Detail.None)
                builder.WithButton(labels ? "Show Names" : null, $"#{nameof(InventoryAction)}.Names",
                    ButtonStyle.Secondary, Emotes.GetEmote("LabelsOn"), row: 1);
            else
                builder.WithButton(labels ? "Hide Names" : null, $"#{nameof(InventoryAction)}.None",
                    ButtonStyle.Secondary, Emotes.GetEmote("LabelsOff"), row: 1);
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
                Emotes.GetEmote("RevealEphemeralAction"), row: 1);

            // If cursed, add remove Curse Button
            return builder.Build();
        }
    }

    public class GearAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;

        [ActionParameterSlash(Order = 0, Name = "archtype", Description = "Archtype", Required = false,
            Type = ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Order = 1, Name = "Warrior", Value = "Warrior")]
        [ActionParameterOptionString(Order = 2, Name = "Mage", Value = "Mage")]
        [ActionParameterComponent(Order = 0, Name = "archtype", Required = false)]
        public ArchType SelectedArchtype { get; set; } = ArchType.Warrior;

        [ActionParameterComponent(Order = 1, Name = "selection", Required = false)]
        public string SelectedCategory { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "gear",
            Description = "Change the gear of one of your two Archtypes",
            FillParametersAsync = options =>
            {
                if (options != null)
                    SelectedArchtype = Enum.Parse<ArchType>((string)options.FirstOrDefault().Value);
                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                {
                    var archOpt = (string)idOptions[0];
                    SelectedArchtype = Enum.Parse<ArchType>(archOpt);

                    if (idOptions.Length > 1)
                    {
                        var gearOpt = (string)idOptions[1];
                        SelectedCategory = gearOpt;
                    }
                }

                if (selectOptions != null && selectOptions.Any()) SelectedCategory = selectOptions.FirstOrDefault();
                return Task.CompletedTask;
            },
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync
        };

        private bool IsWarrior => SelectedArchtype == ArchType.Warrior;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetGearEmbed(account, SelectedArchtype);
            var comp = GetGearComponent(account, SelectedArchtype, SelectedCategory);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: comp);
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            if (inv.Count == 0)
                return Task.FromResult((false, "You don't have any items."));

            var itemsForGear = inv.Where(i => !Exclusives(SelectedArchtype).Contains(i.ItemType)).ToList();
            if (itemsForGear.Count == 0)
                return Task.FromResult((false, $"You don't have any items equippable for {SelectedArchtype}."));

            return Task.FromResult((true, (string)null));
        }

        private Task<(bool, string)> CanRefreshAsync(bool intoNew)
        {
            return Task.FromResult((true, (string)null));
        }

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);

            msgProps.Embed = GetGearEmbed(account, SelectedArchtype);
            msgProps.Components = GetGearComponent(account, SelectedArchtype, SelectedCategory);
            await Task.CompletedTask;
        }

        private static ItemType[] Exclusives(ArchType archType)
        {
            return archType == ArchType.Warrior ? Inventory.MageExclusive : Inventory.WarriorExclusive;
        }

        internal static Embed GetGearEmbed(UserAccount account, ArchType archtype)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            var itemsForGear = inv.Where(i => !Exclusives(archtype).Contains(i.ItemType)).ToList();

            embed.AddField($"{archtype} Gear", inv.GearToString(archtype));

            var invstring = string.Join("", itemsForGear.Select(i => i.IconDisplay));
            if (invstring.Length >= 1024)
            {
                var remainingstring = invstring;
                var parts = new List<string>();
                while (remainingstring.Length >= 1024)
                {
                    var lastitem = remainingstring.Take(1024).ToList().FindLastIndex(s => s.Equals('>')) + 1;
                    parts.Add(string.Join("", remainingstring.Take(lastitem)));
                    remainingstring = string.Join("", remainingstring.Skip(lastitem));
                }

                parts.Add(remainingstring);
                foreach (var (value, index) in parts.Select((v, i) => (v, i)))
                    embed.AddField($"Equippable Gear ({index + 1}/{parts.Count})", value);
            }
            else
            {
                embed.AddField("Equippable Gear", invstring);
            }

            return embed.Build();
        }

        internal static MessageComponent GetGearComponent(UserAccount account, ArchType archtype,
            string category = null)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            var itemsForGear = inv.Where(i => !Exclusives(archtype).Contains(i.ItemType)).ToList();
            var equippedGear = inv.GetGear(archtype);
            var builder = new ComponentBuilder();
            var isWarrior = archtype == ArchType.Warrior;

            var categoryOptions = new List<SelectMenuOptionBuilder>();
            var labels = account.Preferences.ShowButtonLabels;
            foreach (var cat in Item.Equippables)
                if (itemsForGear.Count(i => i.Category == cat) > 0)
                {
                    var equipped = equippedGear.FirstOrDefault(i => i.Category == cat);
                    var icons = isWarrior ? Inventory.WarriorIcons : Inventory.MageIcons;
                    var emote = equipped?.IconDisplay ?? icons[cat];
                    var defaultSel = category?.Equals(cat.ToString()) ?? false;
                    categoryOptions.Add(new SelectMenuOptionBuilder($"{cat}", $"{cat}", isDefault: defaultSel,
                        emote: Emote.Parse(emote)));
                }

            builder.WithSelectMenu($"#{nameof(GearAction)}.{archtype}", categoryOptions, "Select a Gear Slot", row: 0);

            if (!string.IsNullOrEmpty(category))
            {
                var itemOptions = new List<SelectMenuOptionBuilder>();
                foreach (var item in itemsForGear.Where(i => category.Equals(i.Category.ToString()) && i.Name.Length < SelectMenuOptionBuilder.MaxSelectValueLength))
                {
                    var defaultSel = equippedGear.Contains(item);
                    if (!itemOptions.Any(o => o.Value.Equals(item.Name)))
                    {
                        var desc = item.AddStatsOnEquip.NonZerosToString()[1..^1];
                        desc = desc.Length <= SelectMenuOptionBuilder.MaxDescriptionLength ? desc : null;
                        itemOptions.Add(new SelectMenuOptionBuilder($"{item.Name}", $"{item.Name}", description: desc.IsNullOrEmpty() ? null : desc,
                            emote: Emote.Parse(item.IconDisplay), isDefault: defaultSel));
                    }
                }

                builder.WithSelectMenu($"#{nameof(EquipAction)}.{archtype}", itemOptions,
                    "Select an item to equip it to this slot", row: 1);
            }

            builder.WithButton(labels ? "Inventory" : null, $"#{nameof(InventoryAction)}", ButtonStyle.Success,
                Emotes.GetEmote("InventoryAction"));
            if (inv.GetGear(ArchType.Mage).Any(w => w.IsCursed) || inv.GetGear(ArchType.Warrior).Any(w => w.IsCursed))
                builder.WithButton(labels ? "Remove Cursed Gear" : null, $"{nameof(RemoveCursedGearInventory)}.None",
                    ButtonStyle.Success, Emotes.GetEmote("RemoveCursedAction"));

            return builder.Build();
        }
    }

    public class EquipAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;

        [ActionParameterComponent(Order = 0, Name = "archtype", Description = "...", Required = true)]
        [ActionParameterOptionString(Order = 1, Name = "Warrior", Value = "Warrior")]
        [ActionParameterOptionString(Order = 2, Name = "Mage", Value = "Mage")]
        [ActionParameterSlash(Order = 0, Name = "archtype", Description = "The archtype to equip to", Required = true,
            Type = ApplicationCommandOptionType.String)]
        public ArchType ArchType { get; set; }

        //[ActionParameterComponent(Order = 1, Name = "item", Description ="...", Required = true)]
        [ActionParameterSlash(Order = 1, Name = "item", Description = "The item to equip", Required = true, AutoComplete = true,
            Type = ApplicationCommandOptionType.String)]
        public string ItemToEquip { get; set; }

        public override ActionAutoCompleteProperties AutoCompleteProperties => new()
        {
            AutoComplete = AutoComplete
        };

        private IEnumerable<AutocompleteResult> AutoComplete(AutocompleteOption current, IReadOnlyCollection<AutocompleteOption> selected)
        {
            if (current.Name != "item")
                return null;

            if (selected.First().Name != "archtype")
                return null;

            var arch = ArchType.Warrior;
            if ((string)selected.First().Value != "Warrior")
                arch = ArchType.Mage;
            var text = (string)current.Value;
            var user = EntityConverter.ConvertUser(Context.User);
            return user.Inv.Where(i => i.IsEquippable && i.IsEquippableBy(arch) &&
                    (i.Itemname.Contains(text, StringComparison.InvariantCultureIgnoreCase) || (i.Nickname?.Contains(text, StringComparison.InvariantCultureIgnoreCase) ?? false)))
                .Take(20)
                .Select(i => new AutocompleteResult(i.Name, i.Name));
        }

        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "equip",
            Description = "Equip an item to one an archtype's gear",
            FillParametersAsync = options =>
            {
                if (options != null)
                    ArchType = Enum.Parse<ArchType>((string)options.FirstOrDefault().Value);
                ItemToEquip = (string)options.ElementAt(1).Value;
                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    ArchType = Enum.Parse<ArchType>((string)idOptions.FirstOrDefault());

                if (selectOptions != null && selectOptions.Any()) ItemToEquip = selectOptions.FirstOrDefault();
                return Task.CompletedTask;
            }
        };

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var item = account.Inv.GetItem(ItemToEquip);

            EquipItem(account);
            var embed = GearAction.GetGearEmbed(account, ArchType);
            var component = GearAction.GetGearComponent(account, ArchType, item?.Category.ToString());
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }

        private void EquipItem(UserAccount account)
        {
            var inv = account.Inv;

            if (inv.Equip(ItemToEquip, ArchType)) UserAccountProvider.StoreUser(account);
        }

        private Task<(bool, string)> CanRefreshAsync(bool intoNew)
        {
            return Task.FromResult((true, (string)null));
        }

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var item = ItemDatabase.GetItem(ItemToEquip);

            EquipItem(account);
            msgProps.Embed = GearAction.GetGearEmbed(account, ArchType);
            msgProps.Components = GearAction.GetGearComponent(account, ArchType, item?.Category.ToString());
            await Task.CompletedTask;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            var item = inv.GetItem(ItemToEquip);
            if (item == null)
                return Task.FromResult((false, "Dont have that."));

            if (!item.IsEquippableBy(ArchType))
                return Task.FromResult((false, $"{ArchType}s cannot equip {item.ItemType}s"));

            if (item.ExclusiveTo.Any() && !item.ExclusiveTo.Contains(account.Element))
                return Task.FromResult((false, $"A {account.Element} cannot equip {item.Name}"));

            if (inv.GetGear(ArchType).FirstOrDefault(i => i.Category == item.Category)?.IsCursed ?? false)
                return Task.FromResult((false, $"Oh no! Your {item.Category} slot is cursed!"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class ShopAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;
        private Inventory Shop { get; set; }

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "shop",
            Description = "I wonder what's in store today"
        };

        public override EphemeralRule EphemeralRule => EphemeralRule.Permanent;

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            FillParametersAsync = null,
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync
        };

        public override async Task RunAsync()
        {
            Shop = ItemDatabase.GetShop();
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetShopEmbed(),
                components: GetShopComponent().Build());
        }

        private Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            Shop = ItemDatabase.GetShop();
            msgProps.Embed = GetShopEmbed();
            var builder = GetShopComponent();
            if (!intoNew)
                builder.WithButton("Inventory", $"#{nameof(InventoryAction)}", ButtonStyle.Success,
                    Emotes.GetEmote("InventoryAction"));
            msgProps.Components = builder.Build();
            return Task.CompletedTask;
        }

        private Task<(bool, string)> CanRefreshAsync(bool arg)
        {
            return Task.FromResult((true, (string)null));
        }

        private Embed GetShopEmbed()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(66, 45, 45));
            embed.WithThumbnailUrl(ItemDatabase.Shopkeeper);

            embed.AddField("Shop:", Shop.InventoryToString(Detail.NameAndPrice), true);

            var fb = new EmbedFooterBuilder();
            fb.WithText($"{ItemDatabase.RestockMessage} {ItemDatabase.TimeToNextReset:hh\\h\\ mm\\m}");
            embed.WithFooter(fb);
            return embed.Build();
        }

        private ComponentBuilder GetShopComponent()
        {
            var builder = new ComponentBuilder();
            List<SelectMenuOptionBuilder> options = new();
            foreach (var item in Shop)
                options.Add(new SelectMenuOptionBuilder($"{item.Name}", $"{item.Itemname}", description: $"{item.Price} coins",
                    emote: Emote.Parse(item.IconDisplay)));
            builder.WithSelectMenu(nameof(ShopTake), options, "Select an item to buy it.");

            return builder;
        }
    }

    public class ShopTake : BotComponentAction
    {
        public string ItemName { get; set; }
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (idOptions != null && idOptions.Any() && idOptions[0] != null && idOptions[0].ToString().Length > 0)
                ItemName = (string)idOptions[0];
            else if (selectOptions != null && selectOptions.Any() && selectOptions[0] != null &&
                     selectOptions[0].Length > 0)
                ItemName = selectOptions[0];

            return Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            if (account.Inv.Buy(ItemName))
            {
                UserAccountProvider.StoreUser(account);
                await Context.ReplyWithMessageAsync(EphemeralRule, "Successfully bought item");
            }
            else
            {
                await Context.ReplyWithMessageAsync(EphemeralRule, "Couldn't buy that.");
            }
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var item = ItemDatabase.GetItem(ItemName);
            var shop = ItemDatabase.GetShop();

            if (item == null)
                return Task.FromResult((false, "Item unknown."));

            if (!shop.Any(i => i.Itemname.Equals(item.Itemname, StringComparison.CurrentCultureIgnoreCase)))
                return Task.FromResult((false, "The shop does not carry this item at the moment."));

            if (!account.Inv.HasBalance(item.Price))
                return Task.FromResult((false, "Not enough money"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class ChestAction : IodemBotCommandAction
    {
        [ActionParameterComponent(Order = 0, Name = "quality", Description = "...", Required = false)]
        [ActionParameterOptionString(Order = 1, Name = "Wooden", Value = "Wooden")]
        [ActionParameterOptionString(Order = 2, Name = "Normal", Value = "Normal")]
        [ActionParameterOptionString(Order = 3, Name = "Silver", Value = "Silver")]
        [ActionParameterOptionString(Order = 4, Name = "Gold", Value = "Gold")]
        [ActionParameterOptionString(Order = 5, Name = "Adept", Value = "Adept")]
        [ActionParameterOptionString(Order = 6, Name = "Daily", Value = "Daily")]
        [ActionParameterSlash(Order = 0, Name = "quality", Description = "The archtype to equip to", Required = false,
            Type = ApplicationCommandOptionType.String)]
        public ChestQuality? ChestQuality { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "chest",
            Description = "Open a chest",
            FillParametersAsync = options =>
            {
                if (options != null && options.Any())
                {
                    ChestQuality = Enum.Parse<ChestQuality>((string)options.FirstOrDefault().Value);
                }

                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    ChestQuality = Enum.Parse<ChestQuality>((string)idOptions.FirstOrDefault());

                return Task.CompletedTask;
            }
        };

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            _ = OpenChestAsync(account);
            await Task.CompletedTask;
        }

        private Task<(bool, string)> CanRefreshAsync(bool intoNew)
        {
            return Task.FromResult((true, (string)null));
        }

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            try
            {
                var account = EntityConverter.ConvertUser(Context.User);
                _ = OpenChestAsync(account);
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task OpenChestAsync(UserAccount account)
        {
            var inv = account.Inv;

            inv.TryOpenChest(ChestQuality.Value, out var item, account.LevelNumber);
            inv.Add(item);
            var autoSold = false;
            if (account.Preferences.AutoSell.Contains(item.Rarity))
                autoSold = inv.Sell(item.Name);

            var tickets = (uint)Math.Min(10, inv.DailiesInARow + 1);
            if (ChestQuality.Value == IodemBot.ChestQuality.Daily)
                inv.GameTickets += tickets;

            UserAccountProvider.StoreUser(account);

            RestInteractionMessage inventoryMessage = null;
            if (Context is RequestInteractionContext { OriginalInteraction: SocketMessageComponent component })
            {
                await Task.Delay(25);
                // Only try to assign this when interaction is from a Button, not from Slash Command
                inventoryMessage = await component.GetOriginalResponseAsync();
            }
            var builder = new ComponentBuilder();
            builder.WithButton(account.Preferences.ShowButtonLabels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
               Emotes.GetEmote("RevealEphemeralAction"), row: 1);

            if (!autoSold)
            {
                builder.WithButton(account.Preferences.ShowButtonLabels ? "Sell" : null, $"^{nameof(SellItemAction)}.{item.Name}", ButtonStyle.Secondary, Emotes.GetEmote("SellAction"));
            }

            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetSecondChestEmbed(item, inv, autoSold), components: builder.Build());
            //await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetFirstChestEmbed());

            //await Task.Delay(1000);
            //await Context.UpdateReplyAsync(msgProps => {
            //    msgProps.Embed = GetSecondChestEmbed(item, inv);
            //}
            //);

            if (inventoryMessage != null)
                await inventoryMessage.ModifyAsync(msgProps =>
                {
                    msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                    msgProps.Components = InventoryAction.GetInventoryComponent(account);
                });
        }

        private Embed GetFirstChestEmbed()
        {
            var embed = new EmbedBuilder();

            embed.WithDescription($"Opening {ChestQuality} Chest {Emotes.GetIcon(ChestQuality.Value)}...");

            embed.WithColor(Colors.Get("Iodem"));
            return embed.Build();
        }

        private Embed GetSecondChestEmbed(Item item, Inventory inv, bool isSold = false)
        {
            var embed = new EmbedBuilder();
            var tickets = (uint)Math.Min(10, inv.DailiesInARow + 1);
            embed.WithColor(item.Color);
            if (ChestQuality == IodemBot.ChestQuality.Daily)
                embed.WithFooter(
                    $"Current Reward: {inv.DailiesInARow % Inventory.DailyRewards.Length + 1}/{Inventory.DailyRewards.Length} | Overall Streak: {inv.DailiesInARow + 1}");
            embed.WithDescription(
                $"{Emotes.GetIcon(ChestQuality.Value)} {Context.User.Mention} found a {item.Name} {item.IconDisplay}{(isSold ? "(Auto Sold)" : "")}" +
               $"{(ChestQuality == IodemBot.ChestQuality.Daily ? $"\nYou also obtained {Emotes.GetIcon("GameTicket")} {tickets}" : "")}");

            return embed.Build();
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            if (!inv.HasAnyChests()) return Task.FromResult((false, "You don't have any chests."));

            ChestQuality ??= inv.NextChestQuality();
            if (!inv.HasChest(ChestQuality.Value))
                return Task.FromResult((false, $"You don't have any {ChestQuality} chests"));
            if (inv.IsFull) return Task.FromResult((false, "Your inventory is full."));

            return Task.FromResult((true, (string)null));
        }
    }

    public class UpgradeInventory : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            _ = UpgradeInv();
            await Task.CompletedTask;
        }

        private async Task UpgradeInv()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var moneyneeded = (uint)(50000 * Math.Pow(2, inv.Upgrades));
            if (inv.RemoveBalance(moneyneeded))
            {
                inv.Upgrades++;
                UserAccountProvider.StoreUser(account);

                await Context.UpdateReplyAsync(msgProps =>
                {
                    msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                    msgProps.Components = InventoryAction.GetInventoryComponent(account);
                });
                await Context.ReplyWithMessageAsync(EphemeralRule, "Successfully upgraded inventory.");
            }
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;
            var moneyneeded = (uint)(50000 * Math.Pow(2, inv.Upgrades));
            if (inv.Upgrades >= 4)
                return Task.FromResult((false, "Max upgrades reached"));

            if (!account.Inv.HasBalance(moneyneeded))
                return Task.FromResult((false, "Not enough money"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class RemoveCursedGearInventory : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            _ = RemoveCursed();
            await Task.CompletedTask;
        }

        private async Task RemoveCursed()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            if (inv.RemoveCursedEquipment())
            {
                UserAccountProvider.StoreUser(account);

                await Context.UpdateReplyAsync(msgProps =>
                {
                    msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                    msgProps.Components = InventoryAction.GetInventoryComponent(account);
                });
                await Context.ReplyWithMessageAsync(EphemeralRule, "A priest removed all of your cursed equipment");
            }
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            if (!account.Inv.HasBalance(Inventory.RemoveCursedCost))
                return Task.FromResult((false, "Not enough money"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class ItemRenameAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 0, Name = "item", Description = "The item to rename", Required = true,
            Type = ApplicationCommandOptionType.String)]
        public string ItemToRename { get; set; }

        [ActionParameterSlash(Order = 1, Name = "name", Description = "The Name to rename it to", Required = false,
            Type = ApplicationCommandOptionType.String)]
        public string NewName { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;
        public override bool GuildsOnly => true;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "renameitem",
            Description = "Rename one of your items",
            FillParametersAsync = options =>
            {
                if (options != null)
                    ItemToRename = (string)options.FirstOrDefault().Value;
                if (options.Count() > 1)
                    NewName = ((string)options.ElementAt(1)?.Value ?? "").Trim();

                return Task.CompletedTask;
            }
        };

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var item = inv.GetItem(ItemToRename);
            item.Nickname = NewName;
            UserAccountProvider.StoreUser(account);
            await Context.ReplyWithMessageAsync(EphemeralRule,
                $"Renamed {item.IconDisplay} {item.Itemname} to {NewName}");
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);
            var item = EntityConverter.ConvertUser(Context.User).Inv.GetItem(ItemToRename);
            if (item == null)
                return Task.FromResult((false, "Couldn't find that item in your inventory"));

            if (string.IsNullOrWhiteSpace(NewName))
                NewName = item.Itemname;

            return Task.FromResult((true, (string)null));
        }
    }

    public class SellItemAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 0, Name = "item-or-list-to-sell",
            Description = "e.g. `Sol Blade` or `Machete, Padded Gloves, Sol Blade`", Required = true,
            Type = ApplicationCommandOptionType.String)]
        public string[] ItemsToSell { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;
        public override bool GuildsOnly => false;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "sell",
            Description = "Sell one or multiple items",
            FillParametersAsync = options =>
            {
                if (options != null)
                    ItemsToSell = ((string)options.FirstOrDefault().Value).Split(',').Select(s => s.Trim()).ToArray();

                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = (selectOptions, idOptions) =>
             {
                 if (idOptions != null && idOptions.Any())
                 {
                     ItemsToSell = idOptions.Select(i => ((string)i).Trim('*')).ToArray();
                 }
                 if (selectOptions != null && selectOptions.Any())
                 {
                     ItemsToSell = selectOptions.Select(i => ((string)i).Trim('*')).ToArray();
                 }

                 return Task.CompletedTask;
             },
            RefreshAsync = RefreshAsync
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            if (!intoNew)
                return;

            msgProps.Embed = GetSellEmbed().Build();
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var embed = GetSellEmbed();
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed.Build());
        }

        private EmbedBuilder GetSellEmbed()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            EmbedBuilder embed = new();
            if (ItemsToSell.Length == 1)
            {
                var item = ItemsToSell[0];
                var it = inv.GetItem(item);
                if (inv.Sell(item))
                {
                    if (!it.IsBoughtFromShop && it.IsArtifact)
                    {
                        embed.WithDescription($"Sold {it.Icon}{it.Name} for {Emotes.GetIcon("Coin")} {it.SellValue}. Here's {it.TicketValue} Game Ticket{(it.TicketValue > 1 ? "s" : "")} for you, as a little gift.");
                    }
                    else
                    {
                        embed.WithDescription($"Sold {it.Icon}{it.Name} for {Emotes.GetIcon("Coin")} {it.SellValue}.");
                    }
                    embed.WithColor(it.Color);
                }
                else
                {
                    embed.WithDescription(":x: You can only sell unequipped items in your possession.");
                    embed.WithColor(Colors.Get("Error"));
                }
            }
            else
            {
                uint sum = 0;
                uint tickets = 0;
                uint successfull = 0;
                foreach (var i in ItemsToSell)
                    if (inv.HasItem(i.Trim()))
                    {
                        var it = inv.GetItem(i.Trim());
                        if (inv.Sell(it.Name))
                        {
                            sum += it.SellValue;
                            if (!it.IsBoughtFromShop && it.IsArtifact)
                                tickets += it.TicketValue;
                            successfull++;
                        }
                    }

                embed.WithDescription($"Sold {successfull} items for {Emotes.GetIcon("Coin")} {sum} and {Emotes.GetIcon("GameTicket")} {tickets}.");
                embed.WithColor(Colors.Get("Iodem"));
            }
            UserAccountProvider.StoreUser(account);
            return embed;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            if (ItemsToSell == null || !ItemsToSell.Any())
                return Task.FromResult((false, "Can't sell nothin'"));

            if (ItemsToSell.Length == 1)
            {
                var item = EntityConverter.ConvertUser(Context.User).Inv.GetItem(ItemsToSell[0]);
                if (item == null)
                    return Task.FromResult((false, "Couldn't find that item in your inventory"));
            }

            return Task.FromResult((true, (string)null));
        }
    }

    public class SellItemComponent : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            await Context.ReplyWithMessageAsync(EphemeralRule, "Choose what to sell", components: Comp());
        }

        private MessageComponent Comp()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            IEnumerable<Item> ItemsInInv = account.Inv.OrderBy(i => i.Price).Where(i => i.Name.Length + 10 < SelectMenuOptionBuilder.MaxSelectValueLength);

            var builder = new ComponentBuilder();
            int cnt = 0;
            while (ItemsInInv.Any())
            {
                var options = new List<SelectMenuOptionBuilder>();
                foreach (var i in ItemsInInv.Take(SelectMenuBuilder.MaxOptionCount))
                {
                    var itemName = $"{i.Name}{new string('*', options.Count(o => o.Value.StartsWith(i.Name)))}";
                    options.Add(new() { Label = itemName, Value = itemName, Emote = Emote.Parse(i.Icon), Description = $"{i.Price / 2} coins" });
                }
                builder.WithSelectMenu($"^{nameof(SellItemAction)}.{cnt}", options, placeholder: "Select Items to Sell", maxValues: Math.Min(SelectMenuBuilder.MaxValuesCount, options.Count));

                ItemsInInv = ItemsInInv.Skip(SelectMenuBuilder.MaxOptionCount);
                cnt++;
            }

            return builder.Build();
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);
            return SuccessFullResult;
        }
    }

    public class SortInventory : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            inv.Sort();
            UserAccountProvider.StoreUser(account);
            await Context.UpdateReplyAsync(msgProps =>
            {
                msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                msgProps.Components = InventoryAction.GetInventoryComponent(account);
            });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);
            return SuccessFullResult;
        }
    }
}