using System.Text.Json.Serialization;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AdeptClass
    {
        public AdeptClass(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public string[] Movepool { get; set; }
        public Stats StatMultipliers { get; set; }

    }
}