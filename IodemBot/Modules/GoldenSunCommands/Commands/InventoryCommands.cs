using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Statistics.Distributions.Univariate;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Preconditions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [Name("Inventory and Items")]
    public class InventoryCommands : ModuleBase<SocketCommandContext>
    {
        public ColossoBattleService BattleService { get; set; }

        [Command("Inv")]
        [Cooldown(10)]
        [Summary("Displays inventory and current sets")]
        public async Task ShowInventory(Detail detail = Detail.None)
        {
            var split = new Dictionary<Detail, char>
            {
                {Detail.None, '>'},
                {Detail.Names, ','},
                {Detail.NameAndPrice, '\n'}
            };
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var embed = new EmbedBuilder()
                .AddField("Warrior Gear", inv.GearToString(ArchType.Warrior), true)
                .AddField("Mage Gear", inv.GearToString(ArchType.Mage), true);

            var invstring = inv.InventoryToString(detail);
            if (invstring.Length >= 1024)
            {
                var remainingstring = invstring;
                var parts = new List<string>();
                while (remainingstring.Length >= 1024)
                {
                    var lastitem = remainingstring.Take(1024).ToList().FindLastIndex(s => s.Equals(split[detail])) + 1;
                    parts.Add(string.Join("", remainingstring.Take(lastitem)));
                    remainingstring = string.Join("", remainingstring.Skip(lastitem));
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
            fb.WithText(
                $"{inv.Count} / {inv.MaxInvSize} {(inv.Upgrades < 4 ? $"Upgrade: {50000 * Math.Pow(2, inv.Upgrades)}" : "")}");
            embed.AddField("Coin", $"{Emotes.GetIcon("Coin")} {inv.Coins}", true);
            embed.AddField("Game Tickets", $"{Emotes.GetIcon("GameTicket")} {inv.GameTickets}", true);

            embed.WithColor(Colors.Get("Iodem"));
            embed.WithFooter(fb);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("UpgradeInventory")]
        [Summary("Increase the slots of your inventory by 10")]
        public async Task IncreaseBagSize()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;

            if (inv.Upgrades >= 4)
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Maximum Inventory capacity reached.");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            if (inv.RemoveBalance((uint)(50000 * Math.Pow(2, inv.Upgrades))))
            {
                inv.Upgrades++;
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(
                    ":x: Not enough funds. The four upgrades cost, in order:\n<:coin:569836987767324672> 50 000\n<:coin:569836987767324672> 100 000\n<:coin:569836987767324672> 200 000 and\n<:coin:569836987767324672> 400 000");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            }

            await Task.CompletedTask;
        }

        [Command("Shop")]
        [Cooldown(10)]
        [Summary("See the current shop rotation")]
        public async Task Shop()
        {
            var embed = GetShopEmbed();
            await Context.Channel.SendMessageAsync("", false, embed);
        }

        private Embed GetShopEmbed()
        {
            var shop = ItemDatabase.GetShop();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(66, 45, 45));
            embed.WithThumbnailUrl(ItemDatabase.Shopkeeper);

            if (EventSchedule.CheckEvent("Shop"))
            {
                embed.WithDescription("It's the pre-Halloween market! Until October 6th, you'll get a chance to find more and rarer gear! On top of that, the stalls are rotated every 6 hours!");
            }

            embed.AddField("Shop:", shop.InventoryToString(Detail.NameAndPrice), true);

            var fb = new EmbedFooterBuilder();
            fb.WithText($"{ItemDatabase.RestockMessage} {ItemDatabase.TimeToNextReset:hh\\h\\ mm\\m}");
            embed.WithFooter(fb);
            return embed.Build();
        }

        [Command("RandomizeShop")]
        [RequireModerator]
        public async Task RandomizeShop()
        {
            ItemDatabase.RandomizeShop();
            await Shop();
        }

        [Command("item rename")]
        [Alias("item nickname")]
        [Summary("Rename one of your items")]
        [Remarks("`i!item rename Disk Axe, Pizza Cutter`")]
        public async Task RenameItem([Remainder] string itemandnewname)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            var item = itemandnewname;
            var newname = "";
            if (itemandnewname.Contains(','))
            {
                item = itemandnewname.Split(',')[0].Trim();
                newname = itemandnewname.Split(',')[1].Trim().RemoveBadChars();
            }

            if (inv.Rename(item, newname))
            {
                UserAccountProvider.StoreUser(account);
                embed.WithDescription("Item renamed successfully.");
            }
            else
            {
                embed.WithDescription(":x: You don't have such item to polish.");
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("polishes")]
        [RequireStaff]
        public async Task Polishes()
        {
            var allitems = ItemDatabase.GetAllItems();
            var weapons = allitems.Where(i => i.Category == ItemCategory.Weapon);
            var invstring = string.Join(' ', weapons.Where(w => w.CanBeAnimated).Select(i => i.AnimatedIcon));
            invstring += string.Join(", ", weapons.Where(w => !w.CanBeAnimated).Select(i => i.Name));
            var embed = new EmbedBuilder();
            var remainingstring = invstring;
            var parts = new List<string>();
            while (remainingstring.Length >= 1024)
            {
                var lastitem = remainingstring.Take(1024).ToList().FindLastIndex(s => s.Equals(' ')) + 1;
                parts.Add(string.Join("", remainingstring.Take(lastitem)));
                remainingstring = string.Join("", remainingstring.Skip(lastitem));
            }

            parts.Add(remainingstring);
            foreach (var (value, index) in parts.Select((v, i) => (v, i)))
                embed.AddField($"{index + 1}/{parts.Count}", value);

            _ = ReplyAsync(embed: embed.Build());
            await Task.CompletedTask;
        }

        [Command("item polish")]
        [Summary("Polish an item to get its animated sprite")]
        public async Task PolishItem([Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (inv.Polish(item))
            {
                UserAccountProvider.StoreUser(account);
                embed.WithDescription("Item polished successfully.");
            }
            else
            {
                embed.WithDescription(
                    ":x: No such item to polish, or not enough funds. Polishing costs x10 the items price and can only be done with selected artifacts.");
            }

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }

        [Command("Buy")]
        [Summary("Buy an item currently in the shop")]
        public async Task AddItem([Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var shop = ItemDatabase.GetShop();
            if (!shop.HasItem(item))
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Sorry, but we're out of stock for that. Come back later, okay?");
                embed.WithThumbnailUrl(ItemDatabase.Shopkeeper);
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            if (inv.Buy(item))
            {
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Balance not enough or Inventory at full capacity.");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            }

            await Task.CompletedTask;
        }

        [Command("BlackMarket")]
        [Summary("Black Market? What black market? No no, this is a concession stand for your game tickets!")]
        public async Task TicketBuy([Remainder] string item = "")
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var embed = new EmbedBuilder();

            if (!account.Tags.Contains("LunpaCompleted"))
            {
                embed.WithDescription("Black Market? What black market? Oh, are you looking for the concession stand in Lunpa?");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            embed.WithThumbnailUrl(Sprites.GetImageFromName("Dodonpa"));
            if (item.IsNullOrEmpty())
            {
                embed.WithDescription($"Welcome, to the blackmarket, uuh, the concession stand! Have a look around!\nAnything that brain of yours can think of can be found.\nWe've got mountains of content. Some better, some worse.\nIf none of it's of interest to you, you'd be the first!");
                embed.WithColor(Colors.Get("Artifact"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }
            var i = ItemDatabase.GetItem(item);

            if (i.Name.Contains("NOT IMPLEMENTED!"))
            {
                embed.WithDescription($":x: A what? {item}? Never heard of that. I've got my eyes all around Angara, this item is unheard of.");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            if (i.Rarity == ItemRarity.Unique)
            {
                var price = i.TicketPrice;
                var discount = (new[] { 2, 3, 4 }).Random();
                if (account.Id == 557413372979838986 && inv.GameTickets >= price && inv.RemoveTickets(price - (uint)(price / discount)))
                {
                    i = ItemDatabase.GetItem("Leprechaun Needle");
                    embed.WithDescription("I'm sorry to dissappoint, but that's something I really can't get my hands on for you.");
                    embed.WithColor(Colors.Get("Artifact"));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());

                    await Task.Delay(4000);
                    embed.WithDescription($"However..... *intensly stares at {Emotes.GetIcon("GameTicket", "")} {price} Game Tickets*\nLet me see what I can find in the back....");
                    await Context.Channel.SendMessageAsync("", false, embed.Build());

                    await Task.Delay(6000);
                    embed.WithDescription($"There, found something. Not sure where it's from. If you asked me, it's priceless. A unique thing. Well, it's no use to me, but surely you will get more Luck out of it!" +
                        $"\n I'll even give you a {100 / 3}% discount on it!" +
                        $"\n{account.Name} found a {i.Icon} {i.Name}!");
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    embed.WithDescription($":x: You want to get {Utilities.Article(item)} {item}? You jest, there is no way you could get your hands on that.");
                    embed.WithColor(Colors.Get("Error"));
                    _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                    return;
                }
            }
            else
            {
                if (inv.TicketBuy(item))
                {
                    UserAccountProvider.StoreUser(account);
                    _ = ShowInventory();
                }
                else
                {
                    embed.WithDescription(":x: Balance not enough or Inventory at full capacity.");
                    embed.WithColor(Colors.Get("Error"));
                    _ = Context.Channel.SendMessageAsync("", false, embed.Build());
                }
            }

            await Task.CompletedTask;
        }

        [Command("ModBuy")]
        [RequireModerator]
        public async Task ModBuy([Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            if (inv.Buy(item))
            {
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(":x: Balance not enough or Inventory at full capacity.");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            }

            await Task.CompletedTask;
        }

        [Command("GiveItem")]
        [RequireStaff]
        public async Task GiveItem(SocketUser user, [Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(user);
            var inv = account.Inv;
            inv.Add(item);
            UserAccountProvider.StoreUser(account);
            await Task.CompletedTask;
        }

        [Command("Sell")]
        [Summary("Sell an unequipped item from your inventory")]
        public async Task SellItem([Remainder] string item)
        {
            var items = Array.Empty<string>();
            if (item.Contains(',')) items = item.Split(',');
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var embed = new EmbedBuilder();

            if (items.Length > 0)
            {
                uint sum = 0;
                uint tickets = 0;
                uint successfull = 0;
                foreach (var i in items)
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
            else
            {
                var it = inv.GetItem(item);
                if (inv.Sell(item))
                {
                    if (!it.IsBoughtFromShop && it.IsArtifact)
                    {
                        embed.WithDescription($"Sold {it.Icon}{it.Name} for {Emotes.GetIcon("Coin")} {it.SellValue}. Here's {Emotes.GetIcon("GameTicket")} {it.TicketValue} Game Ticket{(it.TicketValue > 1 ? "s" : "")} for you, as a little gift.");
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

            UserAccountProvider.StoreUser(account);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Yeet")]
        [Summary("Yeet an item and wave goodbye to it forever")]
        public async Task YeetItem([Remainder] string item)
        {
            var avatar = EntityConverter.ConvertUser(Context.User);
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(avatar);
            var inv = avatar.Inv;

            var embed = new EmbedBuilder();
            var it = inv.GetItem(item, reverse: true);

            if (inv.Remove(item))
            {
                UserAccountProvider.StoreUser(avatar);
                var maxdist = p.Stats.Atk * Math.Sqrt(p.Stats.Spd) / Math.Log(Math.Max(it.Price / 2, 2)) / 6;
                var level = Math.Min(avatar.LevelNumber, 100);
                var a = 5 + (double)level / 2;
                var b = 55 - (double)level / 2;
                var beta = new BetaDistribution(a, b);
                embed.WithDescription(
                    $"{Context.User.Username} yeets {it.Icon}{it.Name} {Math.Round(beta.Generate(1).FirstOrDefault() * maxdist, 2)} meters away.");
                embed.WithColor(it.Color);

                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                embed.WithDescription(":x: You can only get rid of unequipped items in your possession.");
                embed.WithColor(Colors.Get("Error"));
                _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            }

            await Task.CompletedTask;
        }

        [Command("GiveChest")]
        [RequireStaff]
        public async Task GiveChest(ChestQuality cq, SocketUser user = null)
        {
            var account = EntityConverter.ConvertUser(user ?? Context.User);
            var inv = account.Inv;
            inv.AwardChest(cq);
            UserAccountProvider.StoreUser(account);
            await Task.CompletedTask;
        }

        [Command("Chest")]
        [Summary("Open a chest in your inventory")]
        [Remarks("`i!chest Wooden`")]
        public async Task OpenChest(ChestQuality cq)
        {
            _ = OpenChestAsync(Context, cq);
            await Task.CompletedTask;
        }

        [Command("Chest")]
        public async Task OpenChest()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            foreach (var cq in Inventory.ChestQualities)
                if (inv.HasChest(cq))
                {
                    _ = OpenChestAsync(Context, cq);

                    break;
                }

            await Task.CompletedTask;
        }

        [Command("Daily")]
        [Summary("Open your daily chest")]
        public async Task Daily()
        {
            _ = OpenChestAsync(Context, ChestQuality.Daily);
            await Task.CompletedTask;
        }

        [Command("AddBalance")]
        [RequireStaff]
        public async Task AddBalance(uint amount, SocketUser user = null)
        {
            var account = EntityConverter.ConvertUser(user ?? Context.User);
            var inv = account.Inv;
            inv.AddBalance(amount);
            UserAccountProvider.StoreUser(account);
            await Task.CompletedTask;
        }

        [Command("giveTickets")]
        [RequireStaff]
        public async Task GiveTickets(int amount, SocketUser user = null)
        {
            var account = EntityConverter.ConvertUser(user ?? Context.User);
            var inv = account.Inv;
            if (amount > 0)
                inv.GameTickets += (uint)amount;
            else if (amount <= inv.GameTickets)
                inv.GameTickets -= (uint)amount;
            else
                inv.GameTickets = 0;
            UserAccountProvider.StoreUser(account);
            await Task.CompletedTask;
        }

        private async Task OpenChestAsync(SocketCommandContext context, ChestQuality cq)
        {
            var user = EntityConverter.ConvertUser(context.User);
            var inv = user.Inv;
            var embed = new EmbedBuilder();

            if (inv.IsFull)
            {
                embed.WithDescription(":x: Inventory capacity reached!");
                embed.WithColor(Colors.Get("Error"));
                await context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            if (!inv.TryOpenChest(cq, out Item item, user.LevelNumber))
            {
                embed.WithDescription(cq == ChestQuality.Daily
                    ? $":x: No {cq} Chests remaining! Next Daily Chest in: {DateTime.Today.AddDays(1).Subtract(DateTime.Now):hh\\h\\ mm\\m}"
                    : $":x: No {cq} Chests remaining!");

                embed.WithColor(Colors.Get("Error"));
                await context.Channel.SendMessageAsync("", false, embed.Build());
                return;
            }

            inv.Add(item);

            var tickets = (uint)Math.Min(10, inv.DailiesInARow + 1);
            if (cq == ChestQuality.Daily)
                inv.GameTickets += tickets;

            var autoSold = false;
            if (user.Preferences.AutoSell.Contains(item.Rarity))
                autoSold = inv.Sell(item.Name);

            UserAccountProvider.StoreUser(user);

            embed.WithDescription($"Opening {cq} Chest {Emotes.GetIcon(cq)}...");

            embed.WithColor(Colors.Get("Iodem"));
            var msg = await context.Channel.SendMessageAsync("", false, embed.Build());

            embed = new EmbedBuilder();
            embed.WithColor(item.Color);
            if (cq == ChestQuality.Daily)
                embed.WithFooter(
                    $"Current Reward: {inv.DailiesInARow % Inventory.DailyRewards.Length + 1}/{Inventory.DailyRewards.Length} | Overall Streak: {inv.DailiesInARow + 1}");
            embed.WithDescription($"{Emotes.GetIcon(cq)} You found a {item.Name} {item.IconDisplay}{(autoSold ? $" (Autosold)" : "")}" +
                $"{(cq == ChestQuality.Daily ? $"\nYou also obtained {Emotes.GetIcon("GameTicket")} {tickets}" : "")}");

            await Task.Delay((int)cq * 700);
            _ = msg.ModifyAsync(m => m.Embed = embed.Build());

            var message = await context.Channel.AwaitMessage(m => m.Author == context.User);
            if (message != null && message.Content.Equals("Sell", StringComparison.OrdinalIgnoreCase))
                _ = SellItem(item.Name);
        }

        [Command("Inv Sort")]
        [Summary("Sort your inventory")]
        public async Task SortInventory()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            inv.Sort();
            UserAccountProvider.StoreUser(account);
            _ = ShowInventory();
            await Task.CompletedTask;
        }

        [Command("Equip")]
        [Summary("Equips an item to Mage or Warrior set")]
        [Remarks("`i!equip warrior sol blade`, `i!equip mage iris robe`")]
        public async Task Equip(ArchType archType, [Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;

            if (!inv.HasItem(item))
            {
                await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Colors.Get("Error"))
                    .WithDescription(":x: You do not have that item.")
                    .Build());
                return;
            }

            var selectedItem = inv.GetItem(item);

            if (selectedItem.ExclusiveTo.Any() && !selectedItem.ExclusiveTo.Contains(account.Element))
            {
                _ = Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Colors.Get("Error"))
                    .WithDescription(
                        $":x: Only {string.Join(", ", selectedItem.ExclusiveTo)} Adepts can equip {selectedItem.Name}")
                    .Build());
                return;
            }

            if (inv.Equip(item, archType))
            {
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
                return;
            }

            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Error"))
                .WithDescription($":x: {archType}s cannot equip {selectedItem.ItemType}s.")
                .Build());
        }

        [Command("Unequip")]
        [Summary("Unequip an item from all sets")]
        public async Task Unequip([Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            if (inv.Unequip(item))
            {
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
            }

            await Task.CompletedTask;
        }

        [Command("item repair")]
        [Summary("Repair broken equipment")]
        public async Task Repair([Remainder] string item)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (BattleService.UserInBattle(Context.User.Id))
                return;

            if (inv.Repair(item))
            {
                UserAccountProvider.StoreUser(account);
                embed.WithDescription("Item repaired successfully");
            }
            else
            {
                embed.WithDescription(":x: No such item to repair, or not enough funds.");
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("removeCursed")]
        [Summary("Removes all cursed gear for a small fee of 5000 coins")]
        public async Task RemoveCursed()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            if (inv.RemoveCursedEquipment())
            {
                UserAccountProvider.StoreUser(account);
                _ = ShowInventory();
            }

            await Task.CompletedTask;
        }

        [Command("iteminfo")]
        [Alias("i")]
        [Cooldown(5)]
        [Summary("Gets information on a specified piece of equipment")]
        public async Task ItemInfo([Remainder] string name = "")
        {
            if (name == "") return;

            var item = ItemDatabase.GetItem(name);
            if (item.Name.Contains("NOT IMPLEMENTED"))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(
                    ":x: I asked our treasurer, the weapon smith, the priest, the librarian and a cool looking kid walking by, and no one has heard of that item!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithAuthor($"{item.Name} - {item.Rarity}{(item.IsArtifact ? " Artifact" : "")}");

            embed.AddField("Icon", item.IconDisplay, true);
            embed.AddField("Value", $"{Emotes.GetIcon("Coin")} {item.Price}\n{Emotes.GetIcon("GameTicket")} {item.TicketPrice}", true);

            embed.AddField("Type", item.ItemType, true);
            embed.AddField("Summary", item.Summary(), true);

            //if (!item.Description.IsNullOrEmpty())
            //{
            //   embed.AddField("Description",$"*{item.Description}*", inline:true);
            //}

            embed.WithColor(item.Category == ItemCategory.Weapon && item.IsUnleashable
                ?
                Colors.Get(item.Unleash.UnleashAlignment.ToString())
                : item.IsArtifact
                    ? Colors.Get("Artifact")
                    : Colors.Get("Exathi"));

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());

            if (Context.User is SocketGuildUser sgu)
            {
                _ = ServerGames.UserLookedUpItem(sgu, (SocketTextChannel)Context.Channel);
            }
            await Task.CompletedTask;
        }
    }
}