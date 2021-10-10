using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using Discord;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot.Extensions;
using IodemBot.Discords;

namespace IodemBot.Modules
{
    public class ChangeElementAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order =1, Name ="element",Description ="el", Required =true, Type=ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Name = "Venus", Order=1, Value ="Venus")]
        [ActionParameterOptionString(Name = "Mars", Order=1, Value ="Mars")]
        [ActionParameterOptionString(Name = "Windy Boi", Order=3, Value ="Jupiter")]
        [ActionParameterOptionString(Name = "Mercury", Order=4, Value ="Mercury")]
        [ActionParameterOptionString(Name = "Exathi", Order = 0, Value = "none")]
        public Element SelectedElement { get; set; }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "element",
            Description = "Change your element",
            FillParametersAsync = options =>
            {
                if (options != null)
                    SelectedElement = Enum.Parse<Element>((string)options.FirstOrDefault().Value);
                
                return Task.CompletedTask;
            }
        };
        public override async Task RunAsync()
        {   
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Chose Element: {SelectedElement}");
        }
    }
    class StatusAction : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

     
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "status",
            Description = "Show your Status",
            FillParametersAsync = null
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetStatusEmbed(account);
            msgProps.Components = GetStatusComponent(account);
            await Task.CompletedTask;
        }
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetStatusEmbed(account);
            var component = GetStatusComponent(account);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }
        private static readonly Dictionary<Detail, char> split = new()
        {
            { Detail.none, '>' },
            { Detail.Names, ',' },
            { Detail.NameAndPrice, '\n' }
        };

        internal static Embed GetStatusEmbed(UserAccount account)
        {
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(account);

            var author = new EmbedAuthorBuilder();
            author.WithName($"{account.Name}");
            author.WithIconUrl(account.ImgUrl);


            var embed = new EmbedBuilder()
            .WithColor(Colors.Get(account.Element.ToString()))
            .WithAuthor(author)
            .WithTitle($"Level {account.LevelNumber} {account.GsClass} {string.Join("", account.TrophyCase.Trophies.Select(t => t.Icon))} (Rank {UserAccounts.GetRank(account) + 1})")
            .AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true)
            .AddField("Psynergy", p.GetMoves(false), true)
            .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

            .AddField("Stats", p.Stats.ToString(), true)
            .AddField("Elemental Stats", p.ElStats.ToString(), true)

            .AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses))

            .AddField("XP", $"{account.XP} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXP} total | {account.NewGames} resets)" : "")}", true)
            .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStats.ColossoWins} | {account.ServerStats.DungeonsCompleted}", true)
            .AddField("Endless Streaks", $"Solo: {account.ServerStats.EndlessStreak.Solo} | Duo: {account.ServerStats.EndlessStreak.Duo} \nTrio: {account.ServerStats.EndlessStreak.Trio} | Quad: {account.ServerStats.EndlessStreak.Quad}", true);

            return embed.Build();
        }

        internal static MessageComponent GetStatusComponent(UserAccount account)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            //add status menu button
            builder.WithButton(null, $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Item:895957416557027369>"));
           
            return builder.Build();
        }
    }
    // Status Action
    // Class Change Action
    // Loadout Action
}
