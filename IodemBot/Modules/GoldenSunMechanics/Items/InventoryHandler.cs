using Discord;
using Discord.Commands;
using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [Group("Inventory"), Alias("inv", "bag")]
    public class InventoryHandler : ModuleBase<SocketCommandContext>
    {
        [Command("")]
        public async Task ShowInventory(bool detailed = false)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            var embed = new EmbedBuilder();

            embed.AddField("Warrior Gear", inv.GearToString(ArchType.Warrior), true);
            embed.AddField("Mage Gear", inv.GearToString(ArchType.Mage), true);
            embed.AddField("Inventory", inv.InventoryToString());
            var fb = new EmbedFooterBuilder();
            embed.AddField("Coin", $"<:coin:569836987767324672> {inv.Coins}");

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Buy")]
        public async Task AddItem([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Buy(item);
        }

        [Command("Sell")]
        public async Task SellItem([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Sell(item);
        }

        [Command("Sort")]
        public async Task SortInventory()
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Sort();
        }

        [Command("Equip")]
        public async Task Equip(ArchType archType, [Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Equip(item, archType);
        }

        [Command("Unequip")]
        public async Task Unequip([Remainder] string item)
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.Unequip(item);
        }

        [Command("removeCursed")]
        public async Task RemoveCursed()
        {
            var inv = UserAccounts.GetAccount(Context.User).inv;
            inv.RemoveCursedEquipment();
        }
    }
}
