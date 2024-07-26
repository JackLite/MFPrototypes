using System;

namespace Modules.Extensions.Prototypes
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class PrototypeAttribute : Attribute
    {
        public readonly string name;

        public PrototypeAttribute(string name)
        {
            this.name = name;
        }
    }
}