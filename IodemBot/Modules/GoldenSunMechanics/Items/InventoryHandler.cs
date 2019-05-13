using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class InventoryHandler : ModuleBase<SocketCommandContext>
    {
        [Command("Inv"), Alias("Inventory", "Bag")]
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
            embed.WithFooter(fb);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Shop")]
        public async Task Shop()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            var shop = ItemDatabase.GetShop();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(200, 200, 50));
            embed.WithImageUrl(Sprites.GetImageFromName("Sunshine"));
            embed.AddField("Today's Shop:", shop.InventoryToString(Inventory.Detail.PriceAndName), true);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("RandomizeShop")]
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
                return;
            }

            if (inv.Buy(item))
            {
                _ = ShowInventory();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription("Balance not enough or Inventory at full capacity.");
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
                embed.WithDescription($"Sold {it.Name} for {it.SellValue}.");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                embed.WithDescription("You can only sell unequipped items in your possession.");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("GiveChest")]
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

        private async Task OpenChestAsync(SocketCommandContext Context, ChestQuality cq, uint bonusCount = 0)
        {
            var user = UserAccounts.GetAccount(Context.User);
            var inv = user.Inv;

            if (!inv.RemoveBalance(bonusCount))
            {
                return;
            }

            if (!inv.OpenChest(cq))
            {
                return;
            }

            if (inv.IsFull)
            {
                var emb = new EmbedBuilder();
                emb.WithDescription("Inventory capacity reached!");
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
            var msg = await Context.Channel.SendMessageAsync("", false, embed.Build());

            embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"You found a {item.Name} {item.Icon}");

            await Task.Delay((int)cq * 800);
            _ = msg.ModifyAsync(m => m.Embed = embed.Build());
            inv.Add(item.Name);
        }

        [Command("Inv Clear")]
        public async Task ClearInventory()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            inv.Clear();
            await Task.CompletedTask;
        }

        [Command("Inv Sort")]
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

        [Command("removeCursed")]
        public async Task RemoveCursed()
        {
            var inv = UserAccounts.GetAccount(Context.User).Inv;
            inv.RemoveCursedEquipment();
            await Task.CompletedTask;
        }
    }
}