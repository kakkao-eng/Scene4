using UnityEngine;
using UnityEditor;
using UHFPS.Runtime;

namespace UHFPS.Editors
{
    //[CustomPropertyDrawer(typeof(NameValue<>))]
    public class NameValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty name = property.FindPropertyRelative("Name");
            SerializedProperty value = property.FindPropertyRelative("Value");

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            position.xMax -= EditorGUIUtility.singleLineHeight + 2f;
            {
                float halfWidth = position.width / 2f;
                position = EditorGUI.PrefixLabel(position, GUIContent.none);

                // draw the name field
                Rect nameRect = new(position.x, position.y + 1f, halfWidth, position.height - 2f);
                EditorGUI.PropertyField(nameRect, name, GUIContent.none);

                // draw the value field
                Rect valueRect = new(position.x + halfWidth, position.y + 1f, halfWidth, position.height - 2f);
                EditorGUI.PropertyField(valueRect, value, GUIContent.none);
            }
            EditorGUI.EndProperty();
        }
    }
}