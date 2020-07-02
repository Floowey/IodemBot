using Discord.WebSocket;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    public class GuildSetting
    {
        public ulong GuildID;
        public GuildSetupConfig guildConfig;
        public bool sendWelcomeMessage;
        public bool sendLeaveMessage;
        public bool isTestServer;
        public bool isUserServer;

        [JsonIgnore] public SocketTextChannel MainChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.MainChannelID); } }
        [JsonIgnore] public SocketTextChannel ModChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.ModChannelID); } }
        [JsonIgnore] public SocketTextChannel CommandChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.CommandChannelID); } }
        [JsonIgnore] public SocketTextChannel ColossoChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.ColossoChannelID); } }
        [JsonIgnore] public SocketTextChannel TestCommandChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.TestCommandChannelID); } }
        [JsonIgnore] public SocketTextChannel StreamChannel { get { return (SocketTextChannel)Global.Client.GetChannel(guildConfig.StreamChannelID); } }
    }

    public struct GuildSetupConfig
    {
        public ulong MainChannelID { get; set; }
        public ulong ModChannelID { get; set; }
        public ulong CommandChannelID { get; set; }
        public ulong ColossoChannelID { get; set; }
        public ulong TestCommandChannelID { get; set; }
        public ulong StreamChannelID { get; set; }
    }
}