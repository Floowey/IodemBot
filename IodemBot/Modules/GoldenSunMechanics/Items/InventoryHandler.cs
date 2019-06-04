using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class InventoryHandler : ModuleBase<SocketCommandContext>
    {
        [Command("Inv"), Alias("Inventory", "Bag")]
        [Cooldown(10)]
        public async Task ShowInventory(bool detailed = false)
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var embed = new EmbedBuilder();

            embed.AddField("Warrior Gear", inv.GearToString(ArchType.Warrior), true);
            embed.AddField("Mage Gear", inv.GearToString(ArchType.Mage), true);
            var invstring = inv.InventoryToString(detailed ? Inventory.Detail.Name : Inventory.Detail.none);
            if (invstring.Length > 1024)
            {
                var lastitem = invstring.Take(1024).ToList().FindLastIndex(s => s.Equals(',')) + 1;
                embed.AddField("Inventory (1/2)", string.Join("", invstring.Take(lastitem)));
                embed.AddField("Inventory (2/2)", string.Join("", invstring.Skip(lastitem)));
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
            fb.WithText($"{inv.Count} / {Inventory.MaxInvSize}");
            embed.AddField("Coin", $"<:coin:569836987767324672> {inv.Coins}");
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithFooter(fb);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Shop")]
        [Cooldown(10)]
        public async Task Shop()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var shop = ItemDatabase.GetShop();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(66, 45, 45));
            embed.WithThumbnailUrl(ItemDatabase.shopkeeper);
            embed.AddField("Shop:", shop.InventoryToString(Inventory.Detail.PriceAndName), true);

            var fb = new EmbedFooterBuilder();
            fb.WithText($"{ItemDatabase.restockMessage} {ItemDatabase.TimeToNextReset.ToString(@"hh\h\ mm\m")}");
            embed.WithFooter(fb);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("RandomizeShop")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RandomizeShop()
        {
            ItemDatabase.RandomizeShop();
            await Shop();
        }

        [Command("Buy")]
        public async Task AddItem([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var shop = ItemDatabase.GetShop();
            if (!shop.HasItem(item))
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Sorry, but we're out of stock for that. Come back later, okay?");
                embed.WithThumbnailUrl(ItemDatabase.shopkeeper);
                embed.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            if (inv.Buy(item))
            {
                _ = ShowInventory();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Balance not enough or Inventory at full capacity.");
                embed.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("Sell")]
        public async Task SellItem([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var embed = new EmbedBuilder();
            if (inv.Sell(item))
            {
                var it = ItemDatabase.GetItem(item);
                embed.WithDescription($"Sold {it.Icon}{it.Name} for <:coin:569836987767324672> {it.SellValue}.");
                embed.WithColor((it.IsWeapon && it.IsUnleashable) ? Colors.Get(it.Unleash.UnleashAlignment.ToString()) : it.IsArtifact ? Colors.Get("Artifact") : Colors.Get("Exathi"));

                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                embed.WithDescription(":x: You can only sell unequipped items in your possession.");
                embed.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("GiveChest")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task GiveChest(ChestQuality cq, SocketUser user = null)
        {
            var inv = UserAccounts.GetAccount(user ?? Context.User).Inv;
            inv.AwardChest(cq);
            await Task.CompletedTask;
        }

        [Command("Chest")]
        public async Task OpenChest(ChestQuality cq, uint bonusCount = 0)
        {
            _ = OpenChestAsync(Context, cq, bonusCount);
            await Task.CompletedTask;
        }

        [Command("Claim")]
        public async Task Claim()
        {
            var account = UserAccounts.GetAccount(Context.User);
            var inv = account.Inv;
            await Task.CompletedTask;
        }

        private async Task OpenChestAsync(SocketCommandContext Context, ChestQuality cq, uint bonusCount = 0)
        {
            var user = UserAccounts.GetAccount(Context.User);
            var inv = user.Inv;

            if (inv.IsFull)
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(":x: Inventory capacity reached!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            if (!inv.RemoveBalance(bonusCount))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(":x: Not enough Funds!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            if (!inv.OpenChest(cq))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription($":x: No {cq} Chests remaining!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            double bonus = (bonusCount > 0 ? Math.Log(bonusCount) / Math.Log(5) : 0);
            var value = user.LevelNumber;
            if (cq != ChestQuality.Daily)
            {
                value = (((uint)cq) + 1) * 11;
            }

            var itemName = ItemDatabase.GetRandomItem(value, bonus, (int)cq == 3 || (int)cq == 4 || ((int)cq == 5 && value >= 40) ? ItemDatabase.RandomItemType.Artifact : ItemDatabase.RandomItemType.Any);
            var item = ItemDatabase.GetItem(itemName);

            var embed = new EmbedBuilder();
            embed.WithDescription($"Opening {cq} Chest {Inventory.ChestIcons[cq]}...");
            embed.WithColor(Colors.Get("Iodem"));
            var msg = await Context.Channel.SendMessageAsync("", false, embed.Build());

            embed = new EmbedBuilder();
            embed.WithColor((item.IsWeapon && item.IsUnleashable) ? Colors.Get(item.Unleash.UnleashAlignment.ToString()) : item.IsArtifact ? Colors.Get("Artifact") : Colors.Get("Exathi"));
            embed.WithDescription($"You found a {item.Name} {item.IconDisplay}");
            await Task.Delay((int)cq * 700);
            _ = msg.ModifyAsync(m => m.Embed = embed.Build());
            inv.Add(item.Name);
        }

        [Command("Inv Clear")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ClearInventory()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            inv.Clear();
            await Task.CompletedTask;
        }

        [Command("Inv Sort"), Alias("Bag Sort", "Inventory Sort")]
        public async Task SortInventory()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            inv.Sort();
            _ = ShowInventory();
            await Task.CompletedTask;
        }

        [Command("Equip")]
        public async Task Equip(ArchType archType, [Remainder] string item)
        {
            var account = UserAccounts.GetAccount(Context.User);
            var inv = account.Inv;
            var selectedItem = ItemDatabase.GetItem(item);
            if (selectedItem.ExclusiveTo == null || (selectedItem.ExclusiveTo != null && selectedItem.ExclusiveTo.Contains(account.Element)))
            {
                if (inv.Equip(item, archType))
                {
                    _ = ShowInventory();
                }
            }
            await Task.CompletedTask;
        }

        [Command("Unequip")]
        public async Task Unequip([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            if (inv.Unequip(item))
            {
                _ = ShowInventory();
            }
            await Task.CompletedTask;
        }

        [Command("Repair")]
        public async Task Repair([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            if (inv.Repair(item))
            {
                embed.WithDescription($"Item repaired successfully.");
            }
            else
            {
                embed.WithDescription($"No such item to repair, or not enough funds.");
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("removeCursed")]
        public async Task RemoveCursed()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            if (inv.RemoveCursedEquipment())
            {
                _ = ShowInventory();
            }
            await Task.CompletedTask;
        }

        [Command("iteminfo"), Alias("item", "i")]
        [Cooldown(5)]
        public async Task ItemInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            var item = ItemDatabase.GetItem(name);
            if (item.Name.Contains("NOT IMPLEMENTED"))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(":x: I asked our treasurer, the weapon smith, the priest, the librarian and a cool looking kid walking by, and noone has heard of that item!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithAuthor($"{item.Name} {(item.IsArtifact ? " (Artifact)" : "")}");

            embed.AddField("Icon", item.IconDisplay, true);
            embed.AddField("Value", item.Price, true);
            embed.AddField("Type", item.ItemType, true);
            embed.AddField("Description", item.Summary());

            embed.WithColor((item.IsWeapon && item.IsUnleashable) ? Colors.Get(item.Unleash.UnleashAlignment.ToString()) : item.IsArtifact ? Colors.Get("Artifact") : Colors.Get("Exathi"));

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }
    }
}