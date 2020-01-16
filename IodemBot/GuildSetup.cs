using Discord.WebSocket;

namespace IodemBot
{
    public class GuildSetup
    {
        public ulong MainChannelID;
        public ulong ModChannelID;
        public ulong CommandChannelID;
        public ulong ColossoChannelID;
        public ulong TestCommandChannelID;
        public ulong StreamChannelID;

        public SocketChannel MainChannel { get { return Global.Client.GetChannel(MainChannelID); } }
        public SocketChannel ModChannel { get { return Global.Client.GetChannel(ModChannelID); } }
        public SocketChannel CommandChannel { get { return Global.Client.GetChannel(CommandChannelID); } }
        public SocketChannel ColossoChannel { get { return Global.Client.GetChannel(ColossoChannelID); } }
        public SocketChannel TestCommandChannel { get { return Global.Client.GetChannel(TestCommandChannelID); } }
        public SocketChannel StreamChannel { get { return Global.Client.GetChannel(StreamChannelID); } }
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