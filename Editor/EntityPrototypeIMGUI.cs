using ModulesFramework.Utils.Types;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Modules.Extensions.Prototypes.Editor
{
    public class EntityPrototypeIMGUI
    {
        private readonly EntityPrototypeEditor _editor;
        private bool _isOpen = true;

        private const float CustomIdMarginBottom = 15;

        public EntityPrototypeIMGUI(EntityPrototypeEditor editor)
        {
            _editor = editor;
        }

        public void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            var actualLabel = EditorGUI.BeginProperty(position, label, property);
            var style = new GUIStyle(EditorStyles.foldout);
            style.fontSize = 14;
            var foldoutRect = position;
            foldoutRect.size = style.CalcSize(actualLabel);
            _isOpen = EditorGUI.Foldout(foldoutRect, _isOpen, actualLabel, true, style);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (_isOpen)
                DrawInner(position, property);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private Rect DrawCustomId(Rect position, SerializedProperty property)
        {
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var customIdProperty = property.FindPropertyRelative(nameof(EntityPrototype.customId));
            EditorGUI.PropertyField(position, customIdProperty, new GUIContent("Custom Id"), true);
            position.y += CustomIdMarginBottom;
            return position;
        }
        
        private void DrawInner(Rect position, SerializedProperty property)
        {
            position = DrawCustomId(position, property);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

            var rect = new Rect(position.x + 10, position.y, position.width - 10, position.height);

            for (var index = 0; index < componentsProp.arraySize;)
            {
                var element = componentsProp.GetArrayElementAtIndex(index);
                if (element.managedReferenceValue == null)
                {
                    componentsProp.DeleteArrayElementAtIndex(index);
                    continue;
                }

                var innerComponent = element.FindPropertyRelative("component");
                var componentType = (element.managedReferenceValue as MonoComponent).ComponentType;

                var guiContent = new GUIContent(componentType.GetTypeName());
                if (innerComponent != null && innerComponent.hasChildren)
                {
                    var removeBtnRect = rect;
                    var removeBtnContent = new GUIContent("Remove");
                    removeBtnRect.size = EditorStyles.miniButtonMid.CalcSize(removeBtnContent);
                    removeBtnRect.width += 30;
                    var propertyRect = rect;
                    propertyRect.width -= removeBtnRect.width + 20 + 40;
                    EditorGUI.PropertyField(propertyRect, innerComponent, guiContent,
                        innerComponent.hasChildren);
                    removeBtnRect.y += 0;
                    removeBtnRect.x = position.width - removeBtnRect.width - 20;
                    var removeClicked = GUI.Button(removeBtnRect, removeBtnContent);
                    rect.y += EditorGUI.GetPropertyHeight(innerComponent, innerComponent.hasChildren);
                    rect.y += 10;
                    if (removeClicked)
                        _editor.RemoveWrapper(property, index);
                }
                else if(innerComponent != null)
                {
                    var removeBtnRect = rect;
                    var removeBtnContent = new GUIContent("Remove");
                    removeBtnRect.size = EditorStyles.miniButtonMid.CalcSize(removeBtnContent);
                    removeBtnRect.width += 30;
                    var propertyRect = rect;
                    propertyRect.y += 5;
                    propertyRect.size = EditorStyles.label.CalcSize(guiContent);
                    GUI.Label(propertyRect, guiContent, EditorStyles.label);
                    removeBtnRect.y += 0;
                    removeBtnRect.x = position.width - removeBtnRect.width - 20;
                    var removeClicked = GUI.Button(removeBtnRect, removeBtnContent);
                    rect.y += EditorGUIUtility.singleLineHeight;
                    rect.y += 10;
                    if (removeClicked)
                        _editor.RemoveWrapper(property, index);
                }

                index++;
            }

            var buttonRect = rect;
            var buttonContent = new GUIContent("Add proto-component");
            buttonRect.size = EditorStyles.miniButtonMid.CalcSize(buttonContent);
            buttonRect.width += 30;
            buttonRect.height += 5;
            buttonRect.y += 10;
            buttonRect.x -= 10;
            var stub = new VisualElement();
            if (GUI.Button(buttonRect, buttonContent))
            {
                _editor.ShowAddComponentModal(property, stub);
            }
        }

        public float GetHeight(SerializedProperty property)
        {
            var result = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            if (_isOpen)
            {
                result *= 2;
                result += CustomIdMarginBottom;
                var componentsProp = property.FindPropertyRelative(nameof(EntityPrototype.components));

                for (var index = 0; index < componentsProp.arraySize; index++)
                {
                    var element = componentsProp.GetArrayElementAtIndex(index);
                    var innerComponent = element.FindPropertyRelative("component");

                    if (innerComponent != null && innerComponent.hasChildren)
                    {
                        result += EditorGUI.GetPropertyHeight(element, false);
                        result += EditorGUI.GetPropertyHeight(innerComponent, true);
                        result -= EditorGUIUtility.standardVerticalSpacing;
                    }
                    else
                    {
                        result += EditorStyles.label.CalcSize(new GUIContent("Any")).y + 10;
                        result += EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                var buttonContent = new GUIContent("Add proto-component");
                var btnSize = EditorStyles.miniButtonMid.CalcSize(buttonContent);
                result += btnSize.y + 25;
            }

            return result;
        }
    }
}