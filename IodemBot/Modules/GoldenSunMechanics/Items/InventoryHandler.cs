using Discord;
using Discord.Commands;
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
            var inv = UserAccounts.GetAccount(Context.User).inv;
            var embed = new EmbedBuilder();

            embed.AddField("Warrior Gear", inv.GearToString(ArchType.Warrior), true);
            embed.AddField("Mage Gear", inv.GearToString(ArchType.Mage), true);
            embed.AddField("Inventory", inv.InventoryToString(detailed ? Inventory.Detail.Name : Inventory.Detail.none));
            if (inv.getChestsToString().Length > 0)
            {
                embed.AddField("Chests:", inv.getChestsToString());
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
            var inv = UserAccounts.GetAccount(Context.User).inv;
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
            var inv = UserAccounts.GetAccount(Context.User).inv;
            var shop = ItemDatabase.GetShop();
            if (!shop.HasItem(item))
            {
                return;
            }

            if (inv.Buy(item))
            {
                ShowInventory();
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
            var inv = UserAccounts.GetAccount(Context.User).inv;
            var embed = new EmbedBuilder();
            if (inv.Sell(item))
            {
                var it = ItemDatabase.GetItem(item);
                embed.WithDescription($"Sold {it.Name} for {it.sellValue}.");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                embed.WithDescription("You can only sell unequipped items in your possession.");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("GiveChest")]
        public async Task GiveChest(ChestQuality cq)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.AwardChest(cq);
        }

        [Command("Chest")]
        public async Task OpenChest(ChestQuality cq, uint bonusCount = 0)
        {
            _ = OpenChestAsync(Context, cq, bonusCount);
        }

        private async Task OpenChestAsync(SocketCommandContext Context, ChestQuality cq, uint bonusCount = 0)
        {
            var user = UserAccounts.GetAccount(Context.User);
            var inv = user.inv;

            if (!inv.RemoveBalance(bonusCount))
            {
                return;
            }

            if (!inv.OpenChest(cq))
            {
                return;
            }

            if (inv.isFull)
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
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Clear();
        }

        [Command("Inv Sort")]
        public async Task SortInventory()
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Sort();
            ShowInventory();
        }

        [Command("Equip")]
        public async Task Equip(ArchType archType, [Remainder] string item)
        {
            var account = UserAccounts.GetAccount(Context.User);
            var inv = account.inv;
            var selectedItem = ItemDatabase.GetItem(item);
            if (selectedItem.ExclusiveTo == null || (selectedItem.ExclusiveTo != null && selectedItem.ExclusiveTo.Contains(account.element)))
            {
                if (inv.Equip(item, archType))
                {
                    ShowInventory();
                }
            }
        }

        [Command("Unequip")]
        public async Task Unequip([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            if (inv.Unequip(item))
            {
                ShowInventory();
            }
        }

        [Command("removeCursed")]
        public async Task RemoveCursed()
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.RemoveCursedEquipment();
        }
    }
}