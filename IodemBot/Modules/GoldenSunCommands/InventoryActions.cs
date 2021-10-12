using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    class InventoryAction : IodemBotCommandAction
    {
        private static string coinEmote = "<:coin:569836987767324672>";
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        [ActionParameterSlash(Order = 0, Name = "detail", Description = "Whether to show Names or not", Required = false, Type = ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Order = 0, Name = "None", Value = "none")]
        [ActionParameterOptionString(Order = 1, Name = "Name", Value = "Names")]
        public Detail detail { get; set; } = Detail.none;
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "inv",
            Description = "Manage your Inventory",
            FillParametersAsync = options =>
            {
                if (options != null)
                    detail = Enum.Parse<Detail>((string)options.FirstOrDefault().Value);

                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetInventoryEmbed(account, detail);
            msgProps.Components = GetInventoryComponent(account, detail);
            await Task.CompletedTask;
        }
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetInventoryEmbed(account, detail);
            var component = GetInventoryComponent(account, detail);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }
        private static readonly Dictionary<Detail, char> split = new ()
        {
            { Detail.none, '>' },
            {Detail.Names,',' },
            {Detail.NameAndPrice, '\n' }
        };

        internal static Embed GetInventoryEmbed(UserAccount account, Detail detail=Detail.none)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder()
            .AddField("Warrior Gear", inv.GearToString(ArchType.Warrior), true)
            .AddField("Mage Gear", inv.GearToString(ArchType.Mage), true);

            var invstring = inv.InventoryToString(detail);
            if (invstring.Length >= 1024)
            {
                var remainingstring = invstring;
                List<string> parts = new List<string>();
                while (remainingstring.Length >= 1024)
                {
                    var lastitem = remainingstring.Take(1024).ToList().FindLastIndex(s => s.Equals(split[detail])) + 1;
                    parts.Add(string.Join("", remainingstring.Take(lastitem)));
                    remainingstring = string.Join("", remainingstring.Skip(lastitem));
                }
                parts.Add(remainingstring);
                foreach (var (value, index) in parts.Select((v, i) => (v, i)))
                {
                    embed.AddField($"Inventory ({index + 1}/{parts.Count})", value);
                }
            }
            else
            {
                embed.AddField("Inventory", invstring);
            }
            if (inv.GetChestsToString().Length > 0)
            {
                embed.AddField("Chests:", inv.GetChestsToString());
            }

            var fb = new EmbedFooterBuilder();
            fb.WithText($"{inv.Count} / {inv.MaxInvSize} {(inv.Upgrades < 4 ? $"Upgrade: {50000 * Math.Pow(2, inv.Upgrades)}" : "")}");
            embed.AddField("Coin", $"{coinEmote} {inv.Coins}");
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithFooter(fb);
            return embed.Build();
        }

        internal static MessageComponent GetInventoryComponent(UserAccount account, Detail detail = Detail.none)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            bool detailled = detail == Detail.Names;
            //add status menu button
            builder.WithButton(detailled ? "Status" : null, $"#{nameof(StatusAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Status:896069873124409375>"));
            builder.WithButton(detailled ? "Warrior Gear" : null, $"#{nameof(GearAction)}.Warrior", emote: Emote.Parse("<:Long_Sword:569813505423704117>"), style: ButtonStyle.Success);
            builder.WithButton(detailled ? "Mage Gear" : null, $"#{nameof(GearAction)}.Mage", emote: Emote.Parse("<:Wooden_Stick:569813632897253376>"), style: ButtonStyle.Success);
            builder.WithButton(detailled ? "Shop" : null, $"#{nameof(ShopAction)}.", ButtonStyle.Secondary, Emote.Parse("<:Buy:896735785137614878>"));
            var chest = inv.HasAnyChests() ? inv.NextChestQuality() : ChestQuality.Normal;
            builder.WithButton(detailled ? "Open Chest" : null, $"#{nameof(ChestAction)}.{chest}", emote: Emote.Parse(Inventory.ChestIcons[chest]), disabled:!inv.HasAnyChests());
            if (inv.Upgrades < 4)
                builder.WithButton(detailled ? "Upgrade Inventory" : null, $"#{nameof(UpgradeInventory)}", emote: Emote.Parse("<:button_inventoryupgrade:897032416626098217>"));
            builder.WithButton(detailled ? "Sort Inventory" : null, $"#{nameof(SortInventory)}", emote: Emote.Parse("<:button_inventorysort:897032416915488869>"));

            // If cursed, add remove Curse Button
            return builder.Build();
        }
    }

    public class GearAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order =0, Name ="archtype", Description ="Archtype", Required =false, Type=ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Order =1, Name ="Warrior", Value ="Warrior")]
        [ActionParameterOptionString(Order = 2, Name = "Mage", Value = "Mage")]
        [ActionParameterComponent(Order = 0, Name = "archtype", Required = false)]
        public ArchType SelectedArchtype { get; set; } = ArchType.Warrior;

        [ActionParameterComponent(Order=1, Name ="selection", Required = false)]
        public string SelectedCategory { get; set; }
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "gear",
            Description ="Change the gear of one of your two Archtypes",
            FillParametersAsync = options => 
            {
                if (options != null)
                    SelectedArchtype = Enum.Parse<ArchType>((string)options.FirstOrDefault().Value);
                return Task.CompletedTask;
            }
        };
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetGearEmbed(account, SelectedArchtype);
            var comp = GetGearComponent(account, SelectedArchtype, SelectedCategory);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: comp);
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                {
                    var arch_opt = (string) idOptions[0];
                    SelectedArchtype = Enum.Parse<ArchType>(arch_opt);

                    if(idOptions.Count() > 1)
                    {
                        var gear_opt = (string) idOptions[1];
                        SelectedCategory = gear_opt;
                    }
                } else if (selectOptions != null && selectOptions.Any())
                {
                    var splits = selectOptions[0].Split('.');
                    var arch_opt = splits[0];
                    SelectedArchtype = Enum.Parse<ArchType>(arch_opt);

                    if (splits.Count() > 1)
                    {
                        var gear_opt = splits[1];
                        SelectedCategory = gear_opt;
                    }
                }
                    return Task.CompletedTask;
            },
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync
        };

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            if (inv.Count == 0)
                return Task.FromResult((false, "You don't have any items."));

            var ItemsForGear = inv.Where(i => !exclusives(SelectedArchtype).Contains(i.ItemType)).ToList();
            if (ItemsForGear.Count == 0)
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

        private bool isWarrior { get => SelectedArchtype == ArchType.Warrior; }
        private static ItemType[] exclusives(ArchType archType) => archType == ArchType. Warrior ? Inventory.MageExclusive : Inventory.WarriorExclusive;

        internal static Embed GetGearEmbed(UserAccount account, ArchType archtype)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            var ItemsForGear = inv.Where(i => !exclusives(archtype).Contains(i.ItemType)).ToList();

            embed.AddField($"{archtype} Gear", inv.GearToString(archtype), false);
            
            var invstring = string.Join("", ItemsForGear.Select(i => i.IconDisplay));
            if (invstring.Length >= 1024)
            {
                var remainingstring = invstring;
                List<string> parts = new List<string>();
                while (remainingstring.Length >= 1024)
                {
                    var lastitem = remainingstring.Take(1024).ToList().FindLastIndex(s => s.Equals('>')) + 1;
                    parts.Add(string.Join("", remainingstring.Take(lastitem)));
                    remainingstring = string.Join("", remainingstring.Skip(lastitem));
                }
                parts.Add(remainingstring);
                foreach (var (value, index) in parts.Select((v, i) => (v, i)))
                {
                    embed.AddField($"Equippable Gear ({index + 1}/{parts.Count})", value);
                }
            }
            else
            {
                embed.AddField("Equippable Gear", invstring);
            }

            return embed.Build();
        }

        internal static MessageComponent GetGearComponent(UserAccount account, ArchType archtype, string category=null)
        {
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            var ItemsForGear = inv.Where(i => !exclusives(archtype).Contains(i.ItemType)).ToList();
            var EquippedGear = inv.GetGear(archtype);
            var builder = new ComponentBuilder();
            var isWarrior = archtype == ArchType.Warrior;

            var categoryOptions = new List<SelectMenuOptionBuilder>();
            foreach (var cat in Item.Equippables)
            {
                if(ItemsForGear.Count(i => i.Category == cat) > 0)
                {
                    var equipped = EquippedGear.FirstOrDefault(i => i.Category == cat);
                    var icons = isWarrior ? Inventory.WarriorIcons : Inventory.MageIcons;
                    var emote = equipped?.IconDisplay ?? icons[cat];
                    var defaultSel = category?.Equals(cat.ToString()) ?? false;
                    categoryOptions.Add(new SelectMenuOptionBuilder($"{cat}", $"{archtype}.{cat}.", @default:defaultSel, emote: Emote.Parse(emote)));
                }
            }
            builder.WithSelectMenu("gear", $"#{nameof(GearAction)}", options:categoryOptions, placeholder:"Select a Gear Slot", row:0);

            if(!string.IsNullOrEmpty(category))
            {
                var itemOptions = new List<SelectMenuOptionBuilder>();
                foreach (var item in ItemsForGear.Where(i => category.Equals(i.Category.ToString())))
                {
                    var defaultSel = EquippedGear.Contains(item);
                    if(!itemOptions.Any(o => o.Label.Equals(item.Itemname)))
                        itemOptions.Add(new SelectMenuOptionBuilder($"{item.Name}", $"{item.Itemname}", emote: Emote.Parse(item.IconDisplay), @default: defaultSel));
                }

                builder.WithSelectMenu("items", $"#{nameof(EquipAction)}.{archtype}", options: itemOptions, placeholder:"Select an item to equip it to this slot", row:1);
            }
            builder.WithButton(null, $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Item:895957416557027369>"));
            return builder.Build();
        }
    }
    
    public class EquipAction : IodemBotCommandAction
    {
        [ActionParameterComponent(Order=0, Name="archtype", Description= "...", Required =true)]
        [ActionParameterOptionString(Order = 1, Name = "Warrior", Value = "Warrior")]
        [ActionParameterOptionString(Order = 2, Name = "Mage", Value = "Mage")]
        [ActionParameterSlash(Order = 0, Name = "archtype", Description = "The archtype to equip to", Required = true, Type = ApplicationCommandOptionType.String)]
        public ArchType archType { get; set; }

        //[ActionParameterComponent(Order = 1, Name = "item", Description ="...", Required = true)]
        [ActionParameterSlash(Order=1, Name="item", Description= "The item to equip", Required=true, Type=ApplicationCommandOptionType.String)]
        public string ItemToEquip { get; set; }

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var item = ItemDatabase.GetItem(ItemToEquip);

            EquipItem(account);
            var embed = GearAction.GetGearEmbed(account, archType);
            var component = GearAction.GetGearComponent(account, archType, item?.Category.ToString() ?? null);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "equip",
            Description = "Equip an item to one an archtype's gear",
            FillParametersAsync = options =>
            {
                if (options != null)
                    archType = Enum.Parse<ArchType>((string)options.FirstOrDefault().Value);
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
                {
                    archType = Enum.Parse<ArchType>((string)idOptions.FirstOrDefault());
                };

                if (selectOptions != null && selectOptions.Any())
                {
                    ItemToEquip = selectOptions.FirstOrDefault();
                };
                return Task.CompletedTask;
            }
        };

        private void EquipItem(UserAccount account)
        {
            var inv = account.Inv;

            if (inv.Equip(ItemToEquip, archType))
            {
                UserAccountProvider.StoreUser(account);
                return;
            }
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
            msgProps.Embed = GearAction.GetGearEmbed(account, archType);
            msgProps.Components = GearAction.GetGearComponent(account, archType, item?.Category.ToString() ?? null);
            await Task.CompletedTask;
        }
        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            var item = inv.GetItem(ItemToEquip);
            if (item == null)
            {
                return Task.FromResult((false, "Dont have that."));
            }
            return Task.FromResult((true, (string)null));
        }
    }

    public class ShopAction : IodemBotCommandAction
    {
        //private string coinEmote = "<:coin:569836987767324672>";
        private Inventory _shop { get; set; }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "shop",
            Description = "I wonder what's in store today"
        };
        public override EphemeralRule EphemeralRule => EphemeralRule.Permanent;
        public override async Task RunAsync()
        {
            _shop = ItemDatabase.GetShop();
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetShopEmbed(), components: GetShopComponent().Build());
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            FillParametersAsync = null,
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync
        };

        private Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            _shop = ItemDatabase.GetShop();
            msgProps.Embed = GetShopEmbed();
            var builder = GetShopComponent();
            if (!intoNew)
            {
                builder.WithButton(null, $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Item:895957416557027369>"));
            }
            msgProps.Components = builder.Build() ;
            return Task.CompletedTask;
        }

        private Task<(bool, string)> CanRefreshAsync(bool arg) => Task.FromResult((true, (string)null));

        private Embed GetShopEmbed()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(66, 45, 45));
            embed.WithThumbnailUrl(ItemDatabase.shopkeeper);

            embed.AddField("Shop:", _shop.InventoryToString(Detail.NameAndPrice), true);

            var fb = new EmbedFooterBuilder();
            fb.WithText($"{ItemDatabase.restockMessage} {ItemDatabase.TimeToNextReset:hh\\h\\ mm\\m}");
            embed.WithFooter(fb);
            return embed.Build();
        }

        private ComponentBuilder GetShopComponent()
        {
            var builder = new ComponentBuilder();
            List<SelectMenuOptionBuilder> options = new();
            foreach (var item in _shop){
                options.Add(new SelectMenuOptionBuilder($"{item.Name} - {item.Price}", $"{item.Itemname}", emote: Emote.Parse(item.IconDisplay)));
            }
            builder.WithSelectMenu("Shop Items", nameof(ShopTake), options, placeholder: "Select an item to buy it.");

            return builder;
        }
    }

    public class ShopTake : BotComponentAction
    {
        public string ItemName { get; set; }
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;
        public override Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (idOptions != null && idOptions.Any() && idOptions[0] != null && idOptions[0].ToString().Length > 0)
                ItemName = (string) idOptions[0];
            else if (selectOptions != null && selectOptions.Any() && selectOptions[0] != null && selectOptions[0].ToString().Length > 0)
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
            } else
            {
                await Context.ReplyWithMessageAsync(EphemeralRule, "Couldn't buy that.");
            }
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var item = ItemDatabase.GetItem(ItemName);
            var shop = ItemDatabase.GetShop();

            if (item == null)
                return Task.FromResult((false, "Item unknown."));

            if (!shop.Any(i => i.Itemname.Equals(item.Itemname, StringComparison.CurrentCultureIgnoreCase)))
                return Task.FromResult((false, "The shop does not carry this item at the moment."));
            
            if(!account.Inv.HasBalance(item.Price))
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
        [ActionParameterSlash(Order = 0, Name = "quality", Description = "The archtype to equip to", Required = false, Type = ApplicationCommandOptionType.String)]
        public ChestQuality? chestQuality { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            _ = OpenChestAsync(account);
            await Task.CompletedTask;
        }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "chest",
            Description = "Open a chest",
            FillParametersAsync = options =>
            {
                if (options != null)
                {
                    chestQuality = Enum.Parse<ChestQuality>((string)options.FirstOrDefault().Value);
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
                {
                    chestQuality = Enum.Parse<ChestQuality>((string)idOptions.FirstOrDefault());
                };

                return Task.CompletedTask;
            }
        };

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

            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task OpenChestAsync(UserAccount account)
        {
            var inv = account.Inv;
            inv.TryOpenChest(chestQuality.Value, out Item item, account.LevelNumber);
            inv.Add(item);
            UserAccountProvider.StoreUser(account);

            RestInteractionMessage InventoryMessage = null;
            if (Context is RequestInteractionContext c && c.OriginalInteraction is SocketMessageComponent)
            {
                await Task.Delay(25);
                // Only try to assign this when interaction is from a Button, not from Slash Command
                InventoryMessage = await c.OriginalInteraction.GetOriginalResponseAsync();
            }

            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetSecondChestEmbed(item, inv));
            //await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetFirstChestEmbed());

            //await Task.Delay(1000);
            //await Context.UpdateReplyAsync(msgProps => {
            //    msgProps.Embed = GetSecondChestEmbed(item, inv);
            //}
            //);


            if (InventoryMessage != null)
            {
                await InventoryMessage.ModifyAsync(msgProps =>
                {
                    msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                    msgProps.Components = InventoryAction.GetInventoryComponent(account);
                });
            }
        }
        private Embed GetFirstChestEmbed()
        {
            var embed = new EmbedBuilder();

            embed.WithDescription($"Opening {chestQuality} Chest {Inventory.ChestIcons[chestQuality.Value]}...");

            embed.WithColor(Colors.Get("Iodem"));
            return embed.Build();
        }
        private Embed GetSecondChestEmbed(Item item, Inventory inv)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(item.Color);
            if (chestQuality == ChestQuality.Daily)
            {
                embed.WithFooter($"Current Reward: {inv.DailiesInARow % Inventory.dailyRewards.Length + 1}/{Inventory.dailyRewards.Length} | Overall Streak: {inv.DailiesInARow + 1}");
            }
            embed.WithDescription($"{Inventory.ChestIcons[chestQuality.Value]} You found a {item.Name} {item.IconDisplay}");

            return embed.Build();
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;

            if (!inv.HasAnyChests()) return Task.FromResult((false, "You don't have any chests."));
            if (!chestQuality.HasValue)
                chestQuality = inv.NextChestQuality();
            if (!inv.HasChest(chestQuality.Value)) return Task.FromResult((false, $"You don't have any {chestQuality} chests"));
            if(inv.IsFull) return Task.FromResult((false, "Your inventory is full."));

            return Task.FromResult((true, (string)null));
        }
    }

    public class UpgradeInventory : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;
     
        public override async Task RunAsync()
        {
            _ = UpgradeInv();
            await Task.CompletedTask;
        }
        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };
        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
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

                RestInteractionMessage InventoryMessage = null;
                if (Context is RequestInteractionContext c && c.OriginalInteraction is SocketMessageComponent)
                {
                    await Task.Delay(25);
                    // Only try to assign this when interaction is from a Button, not from Slash Command
                    InventoryMessage = await c.OriginalInteraction.GetOriginalResponseAsync();
                }

                await Task.Delay(100);
                await Context.ReplyWithMessageAsync(EphemeralRule, "Successfully upgraded inventory.");

                if (InventoryMessage != null)
                {
                    await InventoryMessage.ModifyAsync(msgProps =>
                    {
                        msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
                        msgProps.Components = InventoryAction.GetInventoryComponent(account);
                    });
                }
            }


        }
        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
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

    public class ItemRenameAction : IodemBotCommandAction
    {
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var item = inv.GetItem(ItemToRename);
            item.Nickname = NewName;
            UserAccountProvider.StoreUser(account);
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Renamed {item.IconDisplay} {item.Itemname} to {NewName}");
        }
        [ActionParameterSlash(Order = 0, Name = "item", Description = "The item to rename", Required=true, Type=ApplicationCommandOptionType.String)]
        public string ItemToRename { get; set; }

        [ActionParameterSlash(Order = 1, Name = "name", Description = "The Name to rename it to", Required = false, Type = ApplicationCommandOptionType.String)]

        public string NewName { get; set; }
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;
        public override bool GuildsOnly => true;
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "renameitem",
            Description = "Rename one of your items",
            FillParametersAsync = options =>
            {
                if (options != null)
                    ItemToRename = (string)options.FirstOrDefault().Value;
                if(options.Count() > 1)
                    NewName = ((string)options.ElementAt(1)?.Value ?? "").Trim();

                return Task.CompletedTask;
            }
        };

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var item = EntityConverter.ConvertUser(Context.User).Inv.GetItem(ItemToRename);
            if (item == null)
                return Task.FromResult((false, "Couldn't find that item in your inventory"));

            if (string.IsNullOrWhiteSpace(NewName))
                NewName = item.Itemname;
//                return Task.FromResult((false, "Not a valid name"));


            return Task.FromResult((true, (string)null));
        }
    }

    public class SortInventory : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            //_ = UpgradeInv();
            await Task.CompletedTask;
        }
        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };
        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            inv.Sort();
            UserAccountProvider.StoreUser(account);
            msgProps.Embed = InventoryAction.GetInventoryEmbed(account);
            msgProps.Components = InventoryAction.GetInventoryComponent(account);
            await Task.CompletedTask;
        }
    }
}
    // Remove Cursed Action
    // Sell Action
    // Iteminfo (With equip button where applicable)

