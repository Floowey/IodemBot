namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AdeptClass
    {
        public string Name { get; set; }
        public string[] Movepool { get; set; }
        public Stats StatMultipliers { get; set; }

        public AdeptClass(string name)
        {
            this.Name = name;
        }
    }
}