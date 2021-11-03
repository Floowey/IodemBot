using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    public class GuildSetting
    {
        public bool AutoSetup = false;
        public GuildSetupConfig GuildConfig;
        public ulong GuildId;
        public bool IsTestServer = false;
        public bool IsUserServer = false;
        public string Name = "";
        public bool SendLeaveMessage = false;
        public bool SendWelcomeMessage = false;

        [JsonIgnore] public IRole TeamBRole => Global.Client.GetGuild(GuildId).GetRole(GuildConfig.TeamBid);
        [JsonIgnore] public IRole FighterRole => Global.Client.GetGuild(GuildId).GetRole(GuildConfig.FighterId);

        [JsonIgnore]
        public SocketTextChannel MainChannel => (SocketTextChannel)Global.Client.GetChannel(GuildConfig.MainChannelId);

        [JsonIgnore]
        public SocketTextChannel ModChannel => (SocketTextChannel)Global.Client.GetChannel(GuildConfig.ModChannelId);

        [JsonIgnore]
        public SocketTextChannel CommandChannel =>
            (SocketTextChannel)Global.Client.GetChannel(GuildConfig.CommandChannelId);

        [JsonIgnore]
        public SocketTextChannel ColossoChannel =>
            (SocketTextChannel)Global.Client.GetChannel(GuildConfig.ColossoChannelId);

        [JsonIgnore]
        public SocketTextChannel TestCommandChannel =>
            (SocketTextChannel)Global.Client.GetChannel(GuildConfig.TestCommandChannelId);

        [JsonIgnore]
        public SocketTextChannel StreamChannel =>
            (SocketTextChannel)Global.Client.GetChannel(GuildConfig.StreamChannelId);

        [JsonIgnore]
        public SocketCategoryChannel CustomBattlesCateogry =>
            (SocketCategoryChannel)Global.Client.GetChannel(GuildConfig.CustomBattlesCateogryId);
    }

    public struct GuildSetupConfig
    {
        public ulong MainChannelId { get; set; }
        public ulong ModChannelId { get; set; }
        public ulong CommandChannelId { get; set; }
        public ulong ColossoChannelId { get; set; }
        public ulong CustomBattlesCateogryId { get; set; }
        public ulong TestCommandChannelId { get; set; }
        public ulong StreamChannelId { get; set; }
        public ulong TeamBid { get; set; }
        public ulong FighterId { get; set; }
    }
}