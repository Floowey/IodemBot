using Discord;

namespace IodemBot.Discords
{
    public enum OutputFieldMessageType
    {
        Invalid,
        Protection,
        BreakingProtection,
        Modification,
        Result,
        Bonus
    }

    public class OutputFieldMessage
    {
        public IUser AffectedUserData { get; set; }
        public OutputFieldMessageType MessageType { get; set; }
        public string Message { get; set; }

        public OutputFieldMessage(IUser affectedUserData, OutputFieldMessageType messageType, string message)
        {
            AffectedUserData = affectedUserData;
            MessageType = messageType;
            Message = message;
        }

        public static implicit operator OutputFieldMessage((IUser AffectedUserData, OutputFieldMessageType messageType, string Message) args) => new OutputFieldMessage(args.AffectedUserData, args.messageType, args.Message);
    }
}