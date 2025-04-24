using UnityEngine;
using UnityEditor;
using ThunderWire.Editors;
using Unity.Cinemachine;

namespace UHFPS.Editors
{
    public class PuzzleBlendEditor<T> : Editor where T : MonoBehaviour
    {
        public T Target { get; private set; }
        public PropertyCollection Properties { get; private set; }

        private SerializedProperty foldoutProperty;

        public virtual void OnEnable()
        {
            Target = target as T;
            Properties = EditorDrawing.GetAllProperties(serializedObject);
            foldoutProperty = Properties["VirtualCamera"];
        }

        public override void OnInspectorGUI()
        {
            GUIContent headerContent = EditorDrawing.IconTextContent("Puzzle Settings", "Settings");
            EditorDrawing.SetLabelColor("#E0FBFC");

            if (EditorDrawing.BeginFoldoutBorderLayout(foldoutProperty, headerContent))
            {
                EditorDrawing.ResetLabelColor();

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Puzzle Camera")))
                {
                    Properties.Draw("VirtualCamera");
                    EditorGUI.indentLevel++;
                    Properties.Draw("ControlsContexts");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(1f);

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Blend Settings")))
                {
                    SerializedProperty blendDefinition = Properties["BlendDefinition"];
                    SerializedProperty style = blendDefinition.FindPropertyRelative("Style");
                    SerializedProperty time = blendDefinition.FindPropertyRelative("Time");
                    SerializedProperty curve = blendDefinition.FindPropertyRelative("CustomCurve");

                    CinemachineBlendDefinition.Styles blendStyle = (CinemachineBlendDefinition.Styles)style.enumValueIndex;

                    EditorGUILayout.PropertyField(style);
                    EditorGUILayout.PropertyField(time);

                    if (blendStyle == CinemachineBlendDefinition.Styles.Custom)
                        EditorGUILayout.PropertyField(curve);
                }

                EditorGUILayout.Space(1f);

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Ignore Colliders")))
                {
                    EditorGUI.indentLevel++;
                    {
                        Properties.Draw("CollidersEnable");
                        Properties.Draw("CollidersDisable");
                    }
                    EditorGUI.indentLevel--;
                }

                EditorDrawing.EndBorderHeaderLayout();
            }
            EditorDrawing.ResetLabelColor();
        }
    }
}