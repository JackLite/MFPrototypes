using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModulesFramework.Data.QueryUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Modules.Extensions.Prototypes.Editor
{
    public class AddComponentPopup : EditorWindow
    {
        private readonly Dictionary<string, VisualElement> _components = new();
        public event Action<Type> OnAddClicked;

        public void Show(IEnumerable<Type> serializedTypes)
        {
            var styles = Resources.Load<StyleSheet>("ModulesPrototypesUSS");
            rootVisualElement.styleSheets.Add(styles);

            DrawSearch();

            var scrollView = new ScrollView();
            scrollView.AddToClassList("modules-proto--add-component-modal");
            rootVisualElement.Add(scrollView);
            foreach (var serializedType in serializedTypes.OrderBy(s => s.Name))
            {
                var container = new VisualElement();
                container.AddToClassList("modules-proto--add-component--container");

                var label = new Label(serializedType.Name);
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
                scrollView.Add(container);
                _components.Add(serializedType.Name, container);
            }

            ShowAuxWindow();
        }

        private void DrawSearch()
        {
            var input = new TextField();
            input.label = "Search: ";
            input.AddToClassList("modules-proto--add-component--search-input");
            input.RegisterValueChangedCallback(ev => Filter(ev.newValue));
            rootVisualElement.Add(input);
        }

        private void Filter(string value)
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