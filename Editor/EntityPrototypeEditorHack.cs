using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
namespace Modules.Extensions.Prototypes.Editor
{
    [CustomEditor(typeof(EntityPrototypeComponent))]
    public class EntityPrototypeEditorHack : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            AddPropertyField(root, nameof(EntityPrototypeComponent.createOnStart));
            AddPropertyField(root, nameof(EntityPrototypeComponent.createEntityProvider));
            AddPropertyField(root, nameof(EntityPrototypeComponent.destroyEntityWithGameObject));

            AddPropertyField(root, nameof(EntityPrototypeComponent.prototype));

            return root;
        }

        private void AddPropertyField(VisualElement root, string propertyName)
        {
            var prototypeProp = serializedObject.FindProperty(propertyName);
            var protoField = new PropertyField(prototypeProp);
            protoField.Bind(serializedObject);
            root.Add(protoField);
        }
    }
}
#endif