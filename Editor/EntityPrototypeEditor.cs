using Modules.Extensions.Prototypes.Editor.AddingComponents;
using ModulesFrameworkUnity.Utils;
using System;
using System.Linq;
using System.Reflection;
using ModulesFramework.Utils.Types;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor
{
    [CustomPropertyDrawer(typeof(EntityPrototype))]
    public class EntityPrototypeEditor : PropertyDrawer
    {
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

        // N.B. This works only if the inspector is fully based on UIToolkit that is wrong until the Unity 2022 version
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(property.serializedObject.targetObject);
            var root = CreateRoot(property);
            var styles = Resources.Load<StyleSheet>("ModulesPrototypesUSS");
            if (styles != null)
                root.styleSheets.Add(styles);
            root.AddToClassList("modules-proto--inspector");

            var customIdField = new PropertyField(property.FindPropertyRelative(nameof(EntityPrototype.customId)));
            customIdField.AddToClassList("modules-proto--custom-id");
            root.Add(customIdField);
            DrawAdditional(property, root);

            // do not store components container in drawer because drawer is not creating for every element of lists
            var componentsContainer = new VisualElement();
            componentsContainer.AddToClassList("modules-proto--components-container");

            root.Add(componentsContainer);
            DrawComponents(property, componentsContainer);

            DrawAddComponent(property, root, componentsContainer);

            return root;
        }

        /// <summary>
        ///     Override this method to add additional fields before the components list
        /// </summary>
        protected virtual void DrawAdditional(SerializedProperty property, VisualElement root)
        {
        }

        private Foldout CreateRoot(SerializedProperty property)
        {
            var root = new Foldout();
            if (EditorPrefs.HasKey(GetPrefKey(property)))
                root.SetValueWithoutNotify(EditorPrefs.GetBool(GetPrefKey(property)));
            else
                root.SetValueWithoutNotify(true);
            root.RegisterValueChangedCallback(ev =>
            {
#if UNITY_6000_1_OR_NEWER
                if (ev.target != root)
                    return;
#else
                if (ev.propagationPhase != PropagationPhase.AtTarget)
                    return;
#endif
                EditorPrefs.SetBool(GetPrefKey(property), ev.newValue);
            });
            root.text = property.displayName;
            root.Q<Label>().AddToClassList("modules-proto--prototype-title");

            var menuManipulator = new ContextualMenuManipulator(builder =>
            {
                builder.menu.AppendAction("Clear null refs", _ => { ClearNullRefs(property); });
            });
            root.Q<Label>().AddManipulator(menuManipulator);

            return root;
        }

        private void ClearNullRefs(SerializedProperty property)
        {
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));
            var deleted = false;
            for (var index = 0; index < componentsProp.arraySize;)
            {
                var element = componentsProp.GetArrayElementAtIndex(index);
                if (element.managedReferenceValue == null)
                {
                    componentsProp.DeleteArrayElementAtIndex(index);
                    deleted = true;
                    continue;
                }

                index++;
            }

            if (deleted)
                property.serializedObject.ApplyModifiedProperties();

            var target = property.serializedObject.targetObject;
            deleted |= SerializationUtility.ClearAllManagedReferencesWithMissingTypes(target);

            if (deleted)
                EditorUtility.SetDirty(target);
        }

        private void DrawAddComponent(
            SerializedProperty property, 
            VisualElement root, 
            VisualElement componentsContainer)
        {
            var btn = new Button();
            btn.text = "Add proto-component";
            btn.clicked += () => { ShowAddComponentModal(property, componentsContainer); };
            btn.AddToClassList("modules-proto--add-component-btn");

            root.Add(btn);
        }

        internal void ShowAddComponentModal(SerializedProperty property, VisualElement componentsContainer)
        {
            var serializedTypes = AssemblyUtils.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.GetCustomAttribute<PrototypeAttribute>() != null);

            var window = ScriptableObject.CreateInstance<AddComponentPopup>();
            window.OnAddClicked += (type) => AddComponent(property, type, componentsContainer);
            window.Show(serializedTypes);
        }

        private void AddComponent(SerializedProperty property, Type type, VisualElement componentsContainer)
        {
            var assemblies = AssemblyUtils.GetAssemblies();
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
            
            DrawComponents(property, componentsContainer);
        }

        private void DrawComponents(SerializedProperty property, VisualElement componentsContainer)
        {
            componentsContainer.Clear();
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

            for (var index = 0; index < componentsProp.arraySize; index++)
            {
                var propertyContainer = new ComponentContainer();
                propertyContainer.AddToClassList("modules-proto--property-container");
                var element = componentsProp.GetArrayElementAtIndex(index);
                if (element.managedReferenceValue == null)
                {
                    Debug.LogWarning(
                        "[Modules.Proto] There is null reference in prototype. Use 'Clear null refs' from prototype context menu.");
                    continue;
                }

                var innerComponent = element.FindPropertyRelative("component");
                var componentType = (element.managedReferenceValue as MonoComponent).ComponentType;

                if (innerComponent != null && innerComponent.hasChildren)
                {
                    var componentField = new PropertyField(innerComponent);
                    componentField.label = componentType.GetTypeName();
                    componentField.Bind(componentsProp.serializedObject);
                    propertyContainer.Add(componentField);
                }
                else
                {
                    var label = new Label(componentType.GetTypeName());
                    propertyContainer.Add(label);
                }

                propertyContainer.componentType = componentType;

                var btn = CreateRemoveBtn(property, index, componentsContainer);
                propertyContainer.Add(btn);
                componentsContainer.Add(propertyContainer);
            }

            componentsContainer.Sort((el1, el2) =>
            {
                var name1 = ((ComponentContainer)el1).componentType.GetTypeName();
                var name2 = ((ComponentContainer)el2).componentType.GetTypeName();
                return string.Compare(name1, name2, StringComparison.Ordinal);
            });
        }

        private VisualElement CreateRemoveBtn(SerializedProperty property, int index, VisualElement componentsContainer)
        {
            var btn = new Button();
            btn.text = "Remove";
            btn.AddToClassList("modules-proto--remove-btn");
            btn.clicked += () =>
            {
                RemoveWrapper(property, index);
                DrawComponents(property, componentsContainer);
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