using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor.AddingComponents
{
    public class AddComponentCategoryView : ScrollView
    {
        private readonly Dictionary<string, AddComponentOneCategory> _rootCategories = new();

        public event Action<Type> OnAddClicked;

        public void AddTypes(IEnumerable<Type> serializedTypes)
        {
            foreach (var type in serializedTypes)
            {
                var protoAttr = type.GetCustomAttributes()
                    .Single(attr => attr is PrototypeAttribute) as PrototypeAttribute;

                if (protoAttr == null)
                    continue;

                var rootCat = protoAttr.categories.First();
                if (!_rootCategories.TryGetValue(rootCat, out var categoryView))
                {
                    categoryView = new AddComponentOneCategory(rootCat);
                    categoryView.OnAddClicked += t => OnAddClicked?.Invoke(t);
                    Add(categoryView);
                    _rootCategories[rootCat] = categoryView;
                }

                categoryView.AddType(type, protoAttr);
            }

            foreach (var (_, category) in _rootCategories.OrderBy(p => p.Key))
            {
                category.Sort();
                category.BringToFront();
            }
        }
    }
}