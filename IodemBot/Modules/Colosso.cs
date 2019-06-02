using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
    public class Colosso : ModuleBase<SocketCommandContext>
    {
        private static List<string> enemies = new List<string>();
        private static List<Result> results = new List<Result>();
        private static bool lastMessageWasNuts;

        [Command("colosso")]
        [Cooldown(15)]
        [Remarks("Proof your strength by battling a random opponent in Colosso")]
        public async Task ColossoTrain()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            Matchup m = GetRandomMatchup();

            embed.WithAuthor(GetTitle(Context.User, m.Enemy));
            embed.WithDescription(GetText(Context.User, m));

            lastMessageWasNuts = false;
            if (m.Result.Text.Contains("nuts"))
            {
                lastMessageWasNuts = true;
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            if (m.Result.IsWin)
            {
                ServerGames.UserWonColosso((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }
            else
            {
                ServerGames.UserLostColosso((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }
        }

        [Command("colossoAddEnemy")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Remarks("Add an enemy to the Colosso beastiary")]
        public async Task ColossoAddEnemy([Remainder] string enemy)
        {
            if (AddEnemy(enemy))
            {
                await Context.Channel.SendMessageAsync(Utilities.GetFormattedAlert("ENEMY_ADDED", enemy));
            }
            else
            {
                await Context.Channel.SendMessageAsync(Utilities.GetAlert("ENEMY_NOT_ADDED"));
            }
        }

        [Command("colossoAddResult")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Remarks("Add a Result to the Colosso outcomes")]
        public async Task ColossoAddResult(bool isWin, [Remainder] string result)
        {
            AddResult(isWin, result);
            await Context.Channel.SendMessageAsync(Utilities.GetAlert("RESULT_ADDED"));
        }

        private string GetText(SocketUser user, Matchup m)
        {
            return String.Format(m.Result.Text, ((SocketGuildUser)user).DisplayName(), m.Enemy);
        }

        private static Matchup GetRandomMatchup()
        {
            if (enemies.Count == 0 || enemies.Count == 0)
            {
                return new Matchup("Gladiator", new Result("{0} doesn't show up and {1} goes home. \n(If you see this, something is broken, please try it later) Nuts", false));
            }

            return new Matchup(enemies.Random(), results.Random());
        }

        private static string GetTitle(SocketUser user, string enemy)
        {
            return $"{((SocketGuildUser)user).DisplayName()} is up against {enemy}!";
        }

        private struct Matchup
        {
            public Matchup(string enemy, Result result) : this()
            {
                this.Enemy = enemy;
                this.Result = result;
            }

            public string Enemy { get; set; }
            public Result Result { get; set; }
        }

        private struct Result
        {
            public Result(string text, bool isWin) : this()
            {
                Text = text;
                IsWin = isWin;
            }

            public string Text { get; set; }
            public bool IsWin { get; set; }
        }

        static Colosso()
        {
            // Load data
            if (ValidateStorageFile("SystemLang/enemies.json"))
            {
                string jsonE = File.ReadAllText("SystemLang/enemies.json");
                enemies = JsonConvert.DeserializeObject<List<string>>(jsonE);
            }
            if (ValidateStorageFile("SystemLang/results.json"))
            {
                string jsonR = File.ReadAllText("SystemLang/results.json");
                results = JsonConvert.DeserializeObject<List<Result>>(jsonR);
            }

            Global.Client.ReactionAdded += Client_ReactionAdded;
        }

        private static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {
            if (Reaction.Emote.Name == "hard_nut" && lastMessageWasNuts)
            {
                await GoldenSun.AwardClassSeries("Crusader Series", (SocketGuildUser)Reaction.User, (SocketTextChannel)Reaction.Channel);
            }
        }

        private static bool ValidateStorageFile(string file)
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "");
                SaveData();
                return false;
            }
            return true;
        }

        public static void SaveData()
        {
            // Save data
            string jsonE = JsonConvert.SerializeObject(enemies, Formatting.Indented);
            File.WriteAllText("SystemLang/enemies.json", jsonE);

            string jsonR = JsonConvert.SerializeObject(results, Formatting.Indented);
            File.WriteAllText("SystemLang/results.json", jsonR);
        }

        public static bool AddEnemy(string enemy)
        {
            if (enemies.Contains(enemy))
            {
                return false;
            }

            enemies.Add(enemy);
            SaveData();
            return true;
        }

        public static void AddResult(bool isWin, string result)
        {
            Result r = new Result(result, isWin);
            if (results.Contains(r))
            {
                return;
            }

            results.Add(r);
            SaveData();
        }
    }
}