namespace IodemBot.Discords.Actions.Attributes
{
    public interface IActionParameterAttribute
    {
        string[] FilterCommandNames { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        int Order { get; set; }
        bool Required { get; set; }
    }
}