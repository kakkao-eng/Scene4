using System;
using UnityEditor;
using UnityEngine;

namespace ThunderWire.Editors
{
    public class ORMMaskReplace : EditorWindow
    {
        private float Spacing => EditorGUIUtility.standardVerticalSpacing * 2;
        private string MaskMapProperty => "_MaskMap";

        private GUIStyle CenteredHelpBox
        {
            get
            {
                GUIStyle style = new(EditorStyles.helpBox);
                style.alignment = TextAnchor.MiddleLeft;
                style.wordWrap = false;
                return style;
            }
        }

        private string MaterialsPath = "";
        private string TexturesPath = "";
        private string ORMPrefix = "_ORM";
        private string MaskMapPrefix = "_MaskMap";

        [MenuItem("Tools/ORM Tools/ORM Mask Replace")]
        public static void ShowWindow()
        {
            ORMMaskReplace window = GetWindow<ORMMaskReplace>(false, "ORM Mask Replace");
            window.Show();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            headerStyle.normal.textColor = Color.white;
            GUIContent title = new("ORM Mask Replace");
            title.text = title.text.ToUpper();

            Rect rect = GUILayoutUtility.GetRect(1, 30);
            ColorUtility.TryParseHtmlString("#181818", out Color color);
            EditorGUI.DrawRect(rect, color);
            EditorGUI.LabelField(rect, title, headerStyle);
        }

        void OnGUI()
        {
            Rect editorAreaRect = new(0, 0, position.width, position.height);
            editorAreaRect.y += Spacing;
            editorAreaRect.yMax -= Spacing;
            editorAreaRect.xMin += Spacing;
            editorAreaRect.xMax -= Spacing;

            using (new GUILayout.AreaScope(editorAreaRect))
            {
                DrawHeader();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Select Materials Folder", EditorStyles.miniBoldLabel);
                DrawPathSelector(MaterialsPath, path => MaterialsPath = path);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Select MaskMap Textures Folder", EditorStyles.miniBoldLabel);
                DrawPathSelector(TexturesPath, path => TexturesPath = path);

                EditorGUILayout.Space();
                ORMPrefix = EditorGUILayout.TextField("ORM Prefix", ORMPrefix);
                MaskMapPrefix = EditorGUILayout.TextField("MaskMap Prefix", MaskMapPrefix);

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This tool finds all the materials in the folder and replaces the HDRP Lit MaskMap texture with a texture from the textures folder that has the same texture name, but the prefix is _MaskMap.", MessageType.Info);

                if (GUILayout.Button("Process Materials", GUILayout.Height(30f)))
                {
                    ProcessMaterials();
                }
            }
        }

        private void DrawPathSelector(string path, Action<string> onPathSelect)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 22f);

            string pathTitle = "Select Folder";
            if(!string.IsNullOrEmpty(path))
                pathTitle = path;

            Rect pathRect = rect;
            pathRect.xMax -= EditorGUIUtility.singleLineHeight + 2f;
            GUI.Box(pathRect, new GUIContent(pathTitle), CenteredHelpBox);

            Rect selectRect = rect;
            selectRect.xMin = selectRect.xMax - EditorGUIUtility.singleLineHeight;

            if(GUI.Button(selectRect, "..."))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select path", "", "");
                folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                onPathSelect?.Invoke(folderPath);
            }
        }

        private void ProcessMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath });
            foreach (string materialGuid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material != null)
                {
                    Debug.Log("Material: " + material.name);
                    Texture maskMap = material.GetTexture(MaskMapProperty);

                    if (maskMap != null)
                    {
                        string maskMapName = maskMap.name;
                        string newMaskMapName = maskMapName.Replace(ORMPrefix, MaskMapPrefix);
                        string[] textureGuids = AssetDatabase.FindAssets(newMaskMapName, new[] { TexturesPath });

                        if (textureGuids.Length > 0)
                        {
                            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[0]);
                            Texture newMaskMapTexture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);

                            if (newMaskMapTexture != null)
                            {
                                material.SetTexture(MaskMapProperty, newMaskMapTexture);
                                EditorUtility.SetDirty(material);
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Process Completed", "All materials have been processed.", "OK");
        }
    }
}