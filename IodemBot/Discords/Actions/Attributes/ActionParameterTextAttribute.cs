using System;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ActionParameterTextAttribute : Attribute, IActionParameterAttribute
    {
        public string[] FilterCommandNames { get; set; }
        public int Order { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsMultiple { get; set; }
        public bool IsRemainder { get; set; }
        public object DefaultValue { get; set; }

        //public TypeReader TypeReader { get; set; }
        public Type ParameterType { get; set; }

        public bool Required { get; set; } = false;
    }
}