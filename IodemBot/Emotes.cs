using System.Collections.Generic;
using Discord;

namespace IodemBot
{
    public class Emotes
    {
        private static readonly Dictionary<string, string> MiscEmotes = new()
        {
            { "Exclamation", "<:Exclamatory:571309036473942026>" },
            { "Coin", "<:coin:569836987767324672>" },
            { "GameTicket", "<:Game_Ticket:987305720666001488>" },

            { "JoinBattle", "<:Fight:536919792813211648>" },
            { "StartBattle", "<:Battle:536954571256365096>" },

            { "Warrior", "<:Long_Sword:569813505423704117>" },
            { "Mage", "<:Wooden_Stick:569813632897253376>" },
            { "Dead", "<:curse:538074679492083742>" },

            { "LabelsOn", "<:Speech_On:899296342998913044>" },
            { "LabelsOff", "<:Speech_Off:899296342898245653>" },

            { "ClassAction", "<:Switch:896735785603194880>" },
            { "DjinnAction", "<:Djinni:896069873149554818>" },
            { "DungeonAction", "<:mapclosed:606236181486632985>" },
            { "InventoryAction", "<:Item:895957416557027369>" },
            { "LoadoutAction", "<:transfer_file:896735785187962901>" },
            { "OptionAction", "<:Options:896735785166979082>" },
            { "RemoveCursedAction", "<:Remove_Curse:896069873438957588>" },
            { "RevealEphemeralAction", "<:Reveal:909763819167944714>" },
            { "SaveLoadoutAction", "<:Save_Game:896735785351520316>" },
            { "SellAction", "<:Sell:896069873149550602>" },
            { "ShopAction", "<:Buy:896735785137614878>" },
            { "StatusAction", "<:Status:896069873124409375>" },
            { "SortInventoryAction", "<:button_inventorysort:897032416915488869>" },
            { "UpgradeInventoryAction", "<:button_inventoryupgrade:897032416626098217>" },
            { "UpgradeDjinnAction", "<:button_djinnupgrade:897032416856789043>" }
        };

        private static readonly Dictionary<Element, string> ElementEmotes = new()
        {
            { Element.Venus, "<:Venus_Element:573938340219584524>" },
            { Element.Mars, "<:Mars_Element:573938340307402786>" },
            { Element.Jupiter, "<:Jupiter_Element:573938340584488987>" },
            { Element.Mercury, "<:Mercury_Element:573938340743872513>" },
            { Element.None, "" }
        };

        private static readonly Dictionary<ChestQuality, string> ChestEmotes = new()
        {
            { ChestQuality.Wooden, "<:wooden_chest:570332670576295986>" },
            { ChestQuality.Normal, "<:chest:570332670442078219>" },
            { ChestQuality.Silver, "<:silver_chest:570332670391877678>" },
            { ChestQuality.Gold, "<:gold_chest:570332670530158593>" },
            { ChestQuality.Adept, "<:adept_chest:570332670329094146>" },
            { ChestQuality.Daily, "<:daily_chest:570332670605787157>" }
        };

        private static readonly Dictionary<Condition, string> ConditionEmotes = new()
        {
            { Condition.Down, "<:curse:538074679492083742>" },
            { Condition.Poison, "<:Poison:549526931847249920>" },
            { Condition.Venom, "<:Venom:598458704400220160>" },
            { Condition.Seal, "<:Psy_Seal:549526931465568257>" },
            { Condition.Stun, "<:Flash_Bolt:536966441862299678>" },
            { Condition.Haunt, "<:Haunted:549526931821953034>" },
            { Condition.ItemCurse, "<:Condemn:583651784040644619>" },
            { Condition.Flinch, "" },
            { Condition.Delusion, "<:delusion:549526931637534721>" },
            { Condition.Sleep, "<:Sleep:555427023519088671>" },
            { Condition.Counter, "" },
            {Condition.SpiritSeal, "<:Psy_Seal:549526931465568257>" },
            {Condition.DeathCurse, "<:DeathCurse1:583645163499552791>" }
        };

        public static string GetIcon(string emoteName, string NotFound = null)
        {
            if (NotFound != null && !MiscEmotes.ContainsKey(emoteName))
                return NotFound;
            return MiscEmotes[emoteName];
        }

        public static Emote GetEmote(string emoteName)
        {
            return Emote.Parse(GetIcon(emoteName));
        }

        public static string GetIcon(Element element, string NotFound = null)
        {
            if (NotFound != null && !ElementEmotes.ContainsKey(element))
                return NotFound;
            return ElementEmotes[element];
        }

        public static Emote GetEmote(Element element)
        {
            return Emote.Parse(GetIcon(element));
        }

        public static string GetIcon(ChestQuality quality, string NotFound = null)
        {
            if (NotFound != null && !ChestEmotes.ContainsKey(quality))
                return NotFound;
            return ChestEmotes[quality];
        }

        public static Emote GetEmote(ChestQuality quality)
        {
            return Emote.Parse(GetIcon(quality));
        }

        public static string GetIcon(Condition condition, string NotFound = null)
        {
            if (NotFound != null && !ConditionEmotes.ContainsKey(condition))
                return NotFound;
            return ConditionEmotes[condition];
        }

        public static Emote GetEmote(Condition condition)
        {
            return Emote.Parse(GetIcon(condition));
        }
    }
}