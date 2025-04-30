using System;
using ModulesFramework.Utils.Types;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor.AddingComponents
{
    /// <summary>
    ///     Container for one component in an Add Component Window
    /// </summary>
    public class ComponentContainer : VisualElement
    {
        public readonly Type componentType;
        private readonly Button _addBtn;

        public event Action<Type> OnAddClicked;

        protected ComponentContainer(Type componentType, Button addBtn)
        {
            this.componentType = componentType;
            _addBtn = addBtn;
            _addBtn.clicked += () => OnAddClicked?.Invoke(this.componentType);
        }

        public static ComponentContainer Create(Type serializedType)
        {
            var btn = new Button
            {
                text = "Add"
            };

            var container = new ComponentContainer(serializedType, btn);
            container.AddToClassList("modules-proto--add-component--container");

            var label = new Label(serializedType.GetTypeName());
            container.Add(label);
            container.Add(btn);
            return container;
        }
    }
}