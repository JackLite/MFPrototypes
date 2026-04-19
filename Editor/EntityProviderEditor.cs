using System.Globalization;
using ModulesFramework.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor
{
    [CustomEditor(typeof(EntityProvider))]
    public class EntityProviderEditor : UnityEditor.Editor
    {
        private Entity _entity;
        public override VisualElement CreateInspectorGUI()
        {
            var provider = (EntityProvider)target;
            var root = new VisualElement();
            root.styleSheets.Add(Resources.Load<StyleSheet>("ModulesPrototypesUSS"));

            _entity = provider.entity;
            if (_entity.IsAlive())
            {
                var entityContainer = new VisualElement();
                entityContainer.AddToClassList("modules-proto--entity-provider--entity-container");
                string text;
                if (_entity.GetCustomId() == _entity.Id.ToString(CultureInfo.InvariantCulture))
                    text = $"Entity ({_entity.GetCustomId()})";
                else
                    text = $"{_entity.GetCustomId()} ({_entity.Id})";
                var label = new Label(text);
                entityContainer.Add(label);
                DrawGoToEntityBtn(entityContainer);
                root.Add(entityContainer);
            }

            var destroyProperty = serializedObject.FindProperty(nameof(provider.destroyEntityWhenDestroyed));
            var destroyField = new PropertyField(destroyProperty);
            destroyField.Bind(serializedObject);
            root.Add(destroyField);

            return root;
        }

        private void DrawGoToEntityBtn(VisualElement root)
        {
            if (!_entity.IsAlive() && EntityPrototypesEventBus.goToEntityClick != null)
                return;

            var button = new Button
            {
                text = "Go to Entity"
            };
            button.AddToClassList("modules-proto--go-to-entity-btn");
            button.clicked += () => { EntityPrototypesEventBus.goToEntityClick(_entity); };
            root.Add(button);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }
    }
}
