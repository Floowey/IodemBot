using System.Collections.Generic;
using System.IO;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules
{
    [Name("Text Battles")]
    public class RandomColossoBattlesCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly List<string> enemies = new List<string>();
        private static readonly List<Result> results = new List<Result>();

        public static Embed ColossoTrain(SocketGuildUser user, IMessageChannel channel)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            Matchup m = GetRandomMatchup();

            embed.WithAuthor(GetTitle(user, m.Enemy));
            embed.WithDescription(GetText(user, m));

            if (m.Result.IsWin)
            {
                ServerGames.UserWonColosso(user, channel);
            }
            else
            {
                ServerGames.UserLostColosso(user, channel);
            }
            return embed.Build();
        }

        private static string GetText(SocketUser user, Matchup m)
        {
            return string.Format(m.Result.Text, ((SocketGuildUser)user).DisplayName(), m.Enemy);
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

        static RandomColossoBattlesCommands()
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
    }
}