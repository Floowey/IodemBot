using System;
using System.Collections.Generic;

namespace IodemBot.Core.UserManagement
{
    public class TrophyCase
    {
        public List<Trophy> Trophies { get; set; } = new List<Trophy>();
    }

    public class Trophy
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public DateTime ObtainedOn { get; set; }
    }
}