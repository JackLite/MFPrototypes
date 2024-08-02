using System;
using System.Collections.Generic;
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

        #if !UNITY_2022_1_OR_NEWER
        private EntityPrototypeIMGUI _entityPrototypeIMGUI;

        public EntityPrototypeEditor()
        {
            _entityPrototypeIMGUI = new EntityPrototypeIMGUI(this);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _entityPrototypeIMGUI.Draw(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _entityPrototypeIMGUI.GetHeight(property);
        }
        #endif

        // N.B. This works only if the inspector is fully based on UIToolkit that is wrong until the Unity 2022 version
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(property.serializedObject.targetObject);
            var root = CreateRoot(property);
            var styles = Resources.Load<StyleSheet>("ModulesPrototypesUSS");
            root.styleSheets.Add(styles);
            root.AddToClassList("modules-proto--inspector");

            _componentsContainer = new VisualElement();
            _componentsContainer.AddToClassList("modules-proto--components-container");

            root.Add(_componentsContainer);
            DrawComponents(property);

            DrawAddComponent(property, root);

            return root;
        }

        private Foldout CreateRoot(SerializedProperty property)
        {
            var root = new Foldout();
            root.SetValueWithoutNotify(EditorPrefs.GetBool(GetPrefKey(property)));
            root.RegisterValueChangedCallback(ev =>
            {
                if (ev.propagationPhase != PropagationPhase.AtTarget)
                    return;
                EditorPrefs.SetBool(GetPrefKey(property), ev.newValue);
            });
            root.text = property.displayName;
            root.Q<Label>().AddToClassList("modules-proto--prototype-title");
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

        internal void ShowAddComponentModal(SerializedProperty property)
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

            #if UNITY_2022_1_OR_NEWER
            DrawComponents(property);
            #endif
        }

        private void DrawComponents(SerializedProperty property)
        {
            _componentsContainer.Clear();
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

            for (var index = 0; index < componentsProp.arraySize;)
            {
                var propertyContainer = new ComponentContainer();
                propertyContainer.AddToClassList("modules-proto--property-container");
                var element = componentsProp.GetArrayElementAtIndex(index);
                // if (element.managedReferenceValue == null)
                // {
                //     componentsProp.DeleteArrayElementAtIndex(index);
                //     continue;
                // }

                var innerComponent = element.FindPropertyRelative("component");
                var componentType = (element.managedReferenceValue as MonoComponent).ComponentType;

                if (innerComponent != null && innerComponent.hasChildren)
                {
                    var componentField = new PropertyField(innerComponent);
                    componentField.label = componentType.Name;
                    componentField.Bind(componentsProp.serializedObject);
                    propertyContainer.Add(componentField);
                }
                else
                {
                    var label = new Label(componentType.Name);
                    propertyContainer.Add(label);
                }

                propertyContainer.componentType = componentType;

                var btn = CreateRemoveBtn(property, index);
                propertyContainer.Add(btn);
                index++;
                _componentsContainer.Add(propertyContainer);
            }

            _componentsContainer.Sort((el1, el2) =>
            {
                var name1 = ((ComponentContainer)el1).componentType.Name;
                var name2 = ((ComponentContainer)el2).componentType.Name;
                return string.Compare(name1, name2, StringComparison.Ordinal);
            });
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

        internal void RemoveWrapper(SerializedProperty property, int index)
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

        private string GetPrefKey(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetInstanceID()
                   + property.name;
        }

        private class ComponentContainer : VisualElement
        {
            public Type componentType;
        }
    }
}