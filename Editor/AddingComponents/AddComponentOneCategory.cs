using System;
using System.Collections.Generic;
using System.Linq;
using ModulesFramework.Utils.Types;
using UnityEditor;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor.AddingComponents
{
    public class AddComponentOneCategory : Foldout
    {
        public readonly string categoryName;

        private readonly Dictionary<string, AddComponentOneCategory> _subCategories = new();
        private readonly List<ComponentContainer> _components = new();
        private readonly int _level;
        private readonly string _path;

        public event Action<Type> OnAddClicked;

        public AddComponentOneCategory(string categoryName, int level = 0, string path = "")
        {
            this.categoryName = categoryName;
            text = categoryName;
            _level = level;
            _path = path + this.categoryName + "/";
            value = EditorPrefs.GetBool(_path, false);

            this.RegisterValueChangedCallback(ev =>
            {
                if (ev.target != this)
                    return;
                EditorPrefs.SetBool(_path, ev.newValue);
            });
        }

        public void AddType(Type type, PrototypeAttribute protoAttr)
        {
            // if there are no subcategories
            if (_level >= protoAttr.categories.Count - 1)
            {
                var componentContainer = ComponentContainer.Create(type);
                Add(componentContainer);
                componentContainer.OnAddClicked += t => OnAddClicked?.Invoke(t);
                _components.Add(componentContainer);
                return;
            }

            var subCategoryName = protoAttr.categories[_level + 1];
            if (!_subCategories.TryGetValue(subCategoryName, out var subCategory))
            {
                subCategory = new AddComponentOneCategory(subCategoryName, _level + 1, _path);
                subCategory.OnAddClicked += t => OnAddClicked?.Invoke(t);
                _subCategories[subCategoryName] = subCategory;
                Add(subCategory);
            }

            subCategory.AddType(type, protoAttr);
        }

        public void Sort()
        {
            foreach (var subCategory in _subCategories.Values)
                subCategory.Sort();

            foreach (var component in _components.OrderBy(c => c.componentType.GetTypeName()))
                component.BringToFront();
        }
    }
}