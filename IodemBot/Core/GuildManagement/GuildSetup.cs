using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    public class GuildSetting
    {
        public string Name = "";
        public ulong GuildID;
        public GuildSetupConfig guildConfig;
        public bool sendWelcomeMessage = false;
        public bool sendLeaveMessage = false;
        public bool isTestServer = false;
        public bool isUserServer = false;
        public bool AutoSetup = false;

        [JsonIgnore] public IRole TeamBRole { get => Global.Client.GetGuild(GuildID).GetRole(guildConfig.TeamBID); }
        [JsonIgnore] public IRole FighterRole { get => Global.Client.GetGuild(GuildID).GetRole(guildConfig.TeamBID); }
        [JsonIgnore] public SocketTextChannel MainChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.MainChannelID); } }
        [JsonIgnore] public SocketTextChannel ModChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.ModChannelID); } }
        [JsonIgnore] public SocketTextChannel CommandChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.CommandChannelID); } }
        [JsonIgnore] public SocketTextChannel ColossoChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.ColossoChannelID); } }
        [JsonIgnore] public SocketTextChannel TestCommandChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.TestCommandChannelID); } }
        [JsonIgnore] public SocketTextChannel StreamChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.StreamChannelID); } }
        [JsonIgnore] public SocketCategoryChannel CustomBattlesCateogry { get { return (SocketCategoryChannel)Global.Client.GetChannel(guildConfig.CustomBattlesCateogryID); } }
    }

    public struct GuildSetupConfig
    {
        public ulong MainChannelID { get; set; }
        public ulong ModChannelID { get; set; }
        public ulong CommandChannelID { get; set; }
        public ulong ColossoChannelID { get; set; }
        public ulong CustomBattlesCateogryID { get; set; }
        public ulong TestCommandChannelID { get; set; }
        public ulong StreamChannelID { get; set; }
        public ulong TeamBID { get; set; }
        public ulong FighterID { get; set; }
    }
}