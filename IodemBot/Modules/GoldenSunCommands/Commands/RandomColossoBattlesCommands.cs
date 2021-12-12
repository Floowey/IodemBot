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
        private static readonly List<string> Enemies = new();
        private static readonly List<Result> Results = new();

        static RandomColossoBattlesCommands()
        {
            // Load data
            if (ValidateStorageFile("SystemLang/enemies.json"))
            {
                var jsonE = File.ReadAllText("SystemLang/enemies.json");
                Enemies = JsonConvert.DeserializeObject<List<string>>(jsonE);
            }

            if (ValidateStorageFile("SystemLang/results.json"))
            {
                var jsonR = File.ReadAllText("SystemLang/results.json");
                Results = JsonConvert.DeserializeObject<List<Result>>(jsonR);
            }
        }

        public static Embed ColossoTrain(SocketGuildUser user, IMessageChannel channel)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            var m = GetRandomMatchup();

            embed.WithAuthor(GetTitle(user, m.Enemy));
            embed.WithDescription(GetText(user, m));

            if (m.Result.IsWin)
                ServerGames.UserWonColosso(user, channel);
            else
                ServerGames.UserLostColosso(user, channel);
            return embed.Build();
        }

        private static string GetText(SocketUser user, Matchup m)
        {
            return string.Format(m.Result.Text, ((SocketGuildUser)user).DisplayName(), m.Enemy);
        }

        private static Matchup GetRandomMatchup()
        {
            if (Enemies.Count == 0 || Enemies.Count == 0)
                return new Matchup("Gladiator",
                    new Result(
                        "{0} doesn't show up and {1} goes home. \n(If you see this, something is broken, please try it later) Nuts",
                        false));

            return new Matchup(Enemies.Random(), Results.Random());
        }

        private static string GetTitle(SocketUser user, string enemy)
        {
            return $"{((SocketGuildUser)user).DisplayName()} is up against {enemy}!";
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
            var jsonE = JsonConvert.SerializeObject(Enemies, Formatting.Indented);
            File.WriteAllText("SystemLang/enemies.json", jsonE);

            var jsonR = JsonConvert.SerializeObject(Results, Formatting.Indented);
            File.WriteAllText("SystemLang/results.json", jsonR);
        }

        private struct Matchup
        {
            public Matchup(string enemy, Result result) : this()
            {
                Enemy = enemy;
                Result = result;
            }

            public string Enemy { get; }
            public Result Result { get; }
        }

        private struct Result
        {
            public Result(string text, bool isWin) : this()
            {
                Text = text;
                IsWin = isWin;
            }

            public string Text { get; }
            public bool IsWin { get; }
        }
    }
}