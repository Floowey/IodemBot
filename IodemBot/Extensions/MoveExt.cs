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
                if (m.emote.StartsWith("<"))
                {
                    e = Emote.Parse(m.emote);
                }
                else
                {
                    e = new Emoji(m.emote);
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