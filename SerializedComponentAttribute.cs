using System;

namespace Modules.Extensions.Prototypes
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class SerializedComponentAttribute : Attribute
    {
        public readonly string name;

        public SerializedComponentAttribute(string name)
        {
            this.name = name;
        }
    }
}