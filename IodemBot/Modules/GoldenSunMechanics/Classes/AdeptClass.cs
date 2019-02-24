using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AdeptClass
    {
        public string name { get; set; }
        public string[] movepool { get; set; }
        public Stats statMultipliers { get; set; }
        public AdeptClass(string name)
        {
            this.name = name;
        }
    }
}
