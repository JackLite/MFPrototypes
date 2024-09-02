using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Extensions.Prototypes
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class PrototypeAttribute : Attribute
    {
        public readonly string name;
        public readonly List<string> categories;

        public PrototypeAttribute(string name, string category = "Default")
        {
            this.name = name;
            categories = category.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}