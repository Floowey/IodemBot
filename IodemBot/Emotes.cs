using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace IodemBot
{
    public class Emotes
    {
        private static Dictionary<string, string> misc_emotes = new()
        {
            { "Exclamation", "<:Exclamatory:571309036473942026>" },
            { "Coin", "<:coin:569836987767324672>" },
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
            { "SaveLoadoutAction", "<:Save_Game:896735785351520316>" },
            { "ShopAction", "<:Buy:896735785137614878>" },
            { "StatusAction", "<:Status:896069873124409375>" },
            { "SortInventoryAction", "<:button_inventorysort:897032416915488869>" },
            { "UpgradeInventoryAction", "<:button_inventoryupgrade:897032416626098217>" },
            { "UpgradeDjinnAction", "<:button_djinnupgrade:897032416856789043>" },

        };

        private static readonly Dictionary<Element, string> element_emotes = new(){
            {Element.Venus, "<:Venus_Element:573938340219584524>"},
            {Element.Mars, "<:Mars_Element:573938340307402786>"},
            {Element.Jupiter, "<:Jupiter_Element:573938340584488987>" },
            {Element.Mercury, "<:Mercury_Element:573938340743872513>" }, 
            {Element.none , ""}
        };

        private static readonly Dictionary<ChestQuality, string> chest_emotes = new()
        {
            {ChestQuality.Wooden, "<:wooden_chest:570332670576295986>" },
            {ChestQuality.Normal, "<:chest:570332670442078219>" },
            {ChestQuality.Silver, "<:silver_chest:570332670391877678>" },
            {ChestQuality.Gold, "<:gold_chest:570332670530158593>" },
            {ChestQuality.Adept, "<:adept_chest:570332670329094146>" },
            {ChestQuality.Daily, "<:daily_chest:570332670605787157>" }
        };

        public static string GetIcon(string emoteName)
        {
            return misc_emotes[emoteName];
        }

        public static Emote GetEmote(string emoteName)
        {
            return Emote.Parse(GetIcon(emoteName));
        }

        public static string GetIcon(Element element)
        {
            return element_emotes[element];
        }

        public static Emote GetEmote(Element element)
        {
            return Emote.Parse(GetIcon(element));
        }

        public static string GetIcon(ChestQuality quality)
        {
            return chest_emotes[quality];
        }

        public static Emote GetEmote(ChestQuality quality)
        {
            return Emote.Parse(GetIcon(quality));
        }
    }
}
