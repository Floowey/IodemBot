using System.Collections.Generic;

namespace IodemBot.Core.UserManagement
{
    public class Preferences
    {
        public List<ItemRarity> AutoSell { get; set; } = new();
        public bool ShowButtonLabels { get; set; } = true;
    }
}