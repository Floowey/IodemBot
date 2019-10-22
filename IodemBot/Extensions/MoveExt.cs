using Discord;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class MoveExt
    {
        public static IEmote GetEmote(this Move m)
        {
            IEmote e;
            try
            {
                if (m.Emote.StartsWith("<"))
                {
                    e = Emote.Parse(m.Emote);
                }
                else
                {
                    e = new Emoji(m.Emote);
                }
            }
            catch
            {
                e = new Emoji("⛔");
            }
            return e;
        }
    }
}