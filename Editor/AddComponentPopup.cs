using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Modules.Extensions.Prototypes.Editor
{
    public class AddComponentPopup : EditorWindow
    {
        public event Action<Type> OnAddClicked;

        public void Show(IEnumerable<Type> serializedTypes)
        {
            var styles = Resources.Load<StyleSheet>("ModulesPrototypesUSS");
            rootVisualElement.styleSheets.Add(styles);
            rootVisualElement.AddToClassList("modules-proto--add-component-modal");
            foreach (var serializedType in serializedTypes.OrderBy(GetSerializedName))
            {
                var container = new VisualElement();
                container.AddToClassList("modules-proto--add-component--container");

                var label = new Label(GetSerializedName(serializedType));
                container.Add(label);

                var btn = new Button
                {
                    text = "Add"
                };
                btn.clicked += () =>
                {
                    OnAddClicked?.Invoke(serializedType);
                };
                container.Add(btn);
                rootVisualElement.Add(container);
            }

            ShowAuxWindow();
        }

        private static string GetSerializedName(Type type)
        {
            var attr = type.GetCustomAttribute<PrototypeAttribute>();
            return attr != null ? attr.name : type.Name;
        }
    }
}