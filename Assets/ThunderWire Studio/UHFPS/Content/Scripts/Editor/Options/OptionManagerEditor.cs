using UnityEngine;
using UnityEditor;
using UHFPS.Runtime;
using ThunderWire.Editors;

namespace UHFPS.Editors
{
    [CustomEditor(typeof(OptionsManager))]
    public class OptionManagerEditor : InspectorEditor<OptionsManager>
    {
        public override void OnInspectorGUI()
        {
            EditorDrawing.DrawInspectorHeader(new GUIContent("Options Manager"), Target);
            EditorGUILayout.Space();

            serializedObject.Update();
            {
                using (new EditorDrawing.BorderBoxScope(new GUIContent("Settings")))
                {
                    Properties.Draw("OptionsAsset");
                    Properties.Draw("GlobalVolume");

                    Properties.Draw("ApplyAndSaveInputs");
                    Properties.Draw("ShowDebug");
                }

                EditorGUILayout.Space();

                using (new EditorDrawing.BorderBoxScope(new GUIContent("Option Links")))
                {
                    var optionLinks = Properties["OptionLinks"];

                    if (optionLinks.arraySize > 0)
                    {
                        EditorGUILayout.HelpBox("Assign the section parent transforms to which the options will be parented based on the section name.", MessageType.Warning);
                        EditorGUILayout.Space();

                        for (int i = 0; i < optionLinks.arraySize; i++)
                        {
                            var link = optionLinks.GetArrayElementAtIndex(i);
                            var parent = link.FindPropertyRelative("SectionParent");
                            string name = link.Find("SectionReference.Name").stringValue;
                            EditorGUILayout.PropertyField(parent, new GUIContent(name));
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please click the Refresh button first to get the Options Sections.", MessageType.Warning);
                    }
                }

                EditorGUILayout.Space();

                using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.HelpBox("When you add or remove options from an asset, always click the refresh and build button to update the option reference list, which will be used to reference options at runtime.", MessageType.Warning);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Refresh Options", GUILayout.Height(25f)))
                    {
                        Target.RefreshOptions();
                    }
                    if (GUILayout.Button("Build Options", GUILayout.Height(25f)))
                    {
                        Target.BuildOptions();
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}