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
        public OutputFieldMessage(IUser affectedUserData, OutputFieldMessageType messageType, string message)
        {
            AffectedUserData = affectedUserData;
            MessageType = messageType;
            Message = message;
        }

        public IUser AffectedUserData { get; set; }
        public OutputFieldMessageType MessageType { get; set; }
        public string Message { get; set; }

        public static implicit operator OutputFieldMessage(
            (IUser AffectedUserData, OutputFieldMessageType messageType, string Message) args)
        {
            return new(args.AffectedUserData, args.messageType, args.Message);
        }
    }
}