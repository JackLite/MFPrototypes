using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor
{
    [CustomEditor(typeof(EntityProvider))]
    public class EntityProviderEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var provider = (EntityProvider)target;
            var root = new VisualElement();

            var ent = provider.entity;
            if (ent.IsAlive())
            {
                string text;
                if (ent.GetCustomId() == ent.Id.ToString(CultureInfo.InvariantCulture))
                    text = $"Entity ({ent.GetCustomId()})";
                else
                    text = $"{ent.GetCustomId()} ({ent.Id})";
                var label = new Label(text);
                root.Add(label);
            }

            var destroyProperty = serializedObject.FindProperty(nameof(provider.destroyEntityWhenDestroyed));
            var destroyField = new PropertyField(destroyProperty);
            destroyField.Bind(serializedObject);
            root.Add(destroyField);

            return root;
        }
    }
}