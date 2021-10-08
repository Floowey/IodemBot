using System;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ActionParameterOptionStringAttribute : Attribute
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Value{ get; set; }
    }
}
