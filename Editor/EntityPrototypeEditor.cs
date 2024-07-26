using System;
using System.Linq;
using System.Reflection;
using ModulesFrameworkUnity.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor
{
    [CustomPropertyDrawer(typeof(EntityPrototype))]
    public class EntityPrototypeEditor : PropertyDrawer
    {
        private readonly UnityAssemblyFilter _assemblyFilter = new();
        private VisualElement _componentsContainer;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(property.serializedObject.targetObject);
            var root = new VisualElement();
            var styles = Resources.Load<StyleSheet>("ModulesPrototypesUSS");
            root.styleSheets.Add(styles);
            root.AddToClassList("modules-proto--inspector");

            _componentsContainer = new VisualElement();
            _componentsContainer.AddToClassList("modules-proto--components-container");

            var title = new Label(property.displayName);
            title.AddToClassList("modules-proto--prototype-title");
            root.Add(title);

            root.Add(_componentsContainer);
            DrawComponents(property);

            DrawAddComponent(property, root);

            return root;
        }

        private void DrawAddComponent(SerializedProperty property, VisualElement root)
        {
            var btn = new Button();
            btn.text = "Add proto-component";
            btn.clicked += () =>
            {
                ShowAddComponentModal(property);
            };
            btn.AddToClassList("modules-proto--add-component-btn");

            root.Add(btn);
        }

        private void ShowAddComponentModal(SerializedProperty property)
        {
            var serializedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => _assemblyFilter.Filter(asm))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.GetCustomAttribute<PrototypeAttribute>() != null);

            var window = ScriptableObject.CreateInstance<AddComponentPopup>();
            window.OnAddClicked += (type) => AddComponent(property, type);
            window.Show(serializedTypes);
        }

        private void AddComponent(SerializedProperty property, Type type)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => _assemblyFilter.Filter(asm));
            var allWrappers = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(IsWrapper);

            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

            foreach (var wrapper in allWrappers)
            {
                if (wrapper.BaseType.GetGenericArguments()[0] != type)
                    continue;

                var component = Activator.CreateInstance(wrapper) as MonoComponent;
                componentsProp.InsertArrayElementAtIndex(componentsProp.arraySize);
                componentsProp.GetArrayElementAtIndex(componentsProp.arraySize - 1).managedReferenceValue = component;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(componentsProp.serializedObject.targetObject);
                break;
            }

            DrawComponents(property);
        }

        private void DrawComponents(SerializedProperty property)
        {
            _componentsContainer.Clear();
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

            for (var index = 0; index < componentsProp.arraySize;)
            {
                var propertyContainer = new VisualElement();
                propertyContainer.AddToClassList("modules-proto--property-container");
                var element = componentsProp.GetArrayElementAtIndex(index);
                if (element.boxedValue == null)
                {
                    componentsProp.DeleteArrayElementAtIndex(index);
                    continue;
                }

                var componentField = new PropertyField(element);
                var componentType = (element.managedReferenceValue as MonoComponent).ComponentType;
                componentField.label = componentType.Name;
                componentField.Bind(componentsProp.serializedObject);
                propertyContainer.Add(componentField);

                var btn = CreateRemoveBtn(property, index);
                propertyContainer.Add(btn);
                index++;
                _componentsContainer.Add(propertyContainer);
            }
        }

        private VisualElement CreateRemoveBtn(SerializedProperty property, int index)
        {
            var btn = new Button();
            btn.text = "Remove";
            btn.AddToClassList("modules-proto--remove-btn");
            btn.clicked += () =>
            {
                RemoveWrapper(property, index);
                DrawComponents(property);
            };
            return btn;
        }

        private void RemoveWrapper(SerializedProperty property, int index)
        {
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));
            componentsProp.DeleteArrayElementAtIndex(index);
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        private static bool IsWrapper(Type type)
        {
            var genericType = typeof(MonoComponent<>);
            if (type.BaseType == genericType)
                return true;

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                var checkType = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
                if (checkType == genericType)
                    return true;
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}