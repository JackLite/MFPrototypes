using System;
using System.Collections.Generic;
using ModulesFramework.Utils.Types;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor.AddingComponents
{
    /// <summary>
    ///     Used when user filter components by search
    /// </summary>
    public class AddComponentFilteredView : ScrollView
    {
        private readonly Dictionary<string, VisualElement> _components = new();

        public event Action<Type> OnAddClicked;

        public void AddType(Type serializedType)
        {
            var container = CreateComponentContainer(serializedType);
            Add(container);
            _components.Add(serializedType.GetTypeName(), container);
        }

        private VisualElement CreateComponentContainer(Type serializedType)
        {
            var container = ComponentContainer.Create(serializedType);
            container.OnAddClicked += type => OnAddClicked?.Invoke(type);
            return container;
        }

        public void Filter(string value)
        {
            foreach (var (typeName, element) in _components)
            {
                if (typeName.Contains(value, StringComparison.InvariantCultureIgnoreCase))
                    element.style.display = DisplayStyle.Flex;
                else
                    element.style.display = DisplayStyle.None;
            }
        }
    }
}