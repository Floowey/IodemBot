using System;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ActionParameterComponentAttribute : Attribute, IActionParameterAttribute
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; } = false;
        public string[] FilterCommandNames { get; set; }
    }
}
