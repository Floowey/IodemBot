using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord.Commands;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Extensions;

namespace IodemBot.Modules
{
    public class Colosso : ModuleBase<SocketCommandContext>
    {
        private static List<string> enemies = new List<string>();
        private static List<Result> results = new List<Result>();

        [Command("colosso")]
        [Cooldown(15)]
        [Remarks("Proof your strength by battling a random opponent in Colosso")]
        public async Task ColossoTrain()
        { 
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));

            Matchup m = getRandomMatchup();

            embed.WithAuthor(getTitle(Context.User, m.enemy));
            embed.WithDescription(getText(Context.User, m));

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            if (m.result.isWin)
            {
                ServerGames.UserWonColosso((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            } else
            {
                ServerGames.UserLostColosso((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }
        }

        [Command("colossoAddEnemy")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Remarks("Add an enemy to the Colosso beastiary")]
        public async Task ColossoAddEnemy([Remainder] string enemy)
        {
            if (addEnemy(enemy)){
                await Context.Channel.SendMessageAsync(Utilities.GetFormattedAlert("ENEMY_ADDED", enemy));
            } else
            {
                await Context.Channel.SendMessageAsync(Utilities.GetAlert("ENEMY_NOT_ADDED"));
            }
            
        }

        [Command("colossoAddResult")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Remarks("Add a Result to the Colosso outcomes")]
        public async Task colossoAddResult(bool isWin, [Remainder] string result)
        {
            addResult(isWin, result);
            await Context.Channel.SendMessageAsync(Utilities.GetAlert("RESULT_ADDED"));
        }


        private string getText(SocketUser user, Matchup m)
        {
            return String.Format(m.result.text, ((SocketGuildUser) user).DisplayName(), m.enemy);
        }

        private static Matchup getRandomMatchup()
        {
            if (enemies.Count == 0 || enemies.Count == 0)
            {
                return new Matchup("Gladiator", new Result("{0} doesn't show up and {1} goes home. \n(If you see this, something is broken, please try it later)", false));
            }
            string enemy = enemies[Global.random.Next(0, enemies.Count)];
            Result result = results[Global.random.Next(0, results.Count)];
            return new Matchup(enemy, result);
        }   

        private static string getTitle(SocketUser user, string enemy)
        {
            return $"{user.Username} is up against {enemy}!";
        }

        private struct Matchup{
            public Matchup(string enemy, Result result) : this()
            {
                this.enemy = enemy;
                this.result = result;
            }

            public string enemy { get; set; }
            public Result result { get; set; }
        }

        private struct Result
        {
            public Result(string text, bool isWin) : this()
            {
                this.text = text;
                this.isWin = isWin;
            }

            public string text { get; set; }
            public bool isWin { get; set; }
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

        public static bool addEnemy(string enemy)
        {
            if (enemies.Contains(enemy)) return false;

            enemies.Add(enemy);
            SaveData();
            return true;
        }

        public static void addResult(bool isWin, string result)
        {
            Result r = new Result(result, isWin);
            if (results.Contains(r)) return;

            results.Add(r);
            SaveData();
        }
    }
}
