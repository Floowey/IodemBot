using System;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ModuleOrderAttribute : Attribute
    {
        public ModuleOrderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; set; }
    }
}