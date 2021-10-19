using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Core.UserManagement
{
    public class Preferences
    {
        public List<ItemRarity> AutoSell { get; set; } = new();
        public bool ShowButtonLabels { get; set; } = true;
    }
}
