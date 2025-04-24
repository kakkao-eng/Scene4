using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderWire.Editors
{
    public class ORMEditor : EditorWindow
    {
        private float Spacing => EditorGUIUtility.standardVerticalSpacing * 2;
        private enum OutputFormat { JPG, PNG, EXR, TGA }

        private readonly Color[] buttonColors =
        {
            Color.clear,
            new Color(0.22f, 0.24f, 0.25f, 1f),
            new Color(1f, 0.75f, 0.04f, 1f)
        };

        private List<Texture2D> ormTextures;
        private readonly int _textureSupportedResolutionMin = 64;
        private readonly int _textureSupportedResolutionMax = 8192;
        private readonly List<int> _textureResolutions = new List<int>();
        private readonly List<string> _textureResolutionsNames = new List<string>();   
        private Vector2 columnScrollPosition;

        private int[] rgba;
        private int resolution = 2048;
        private OutputFormat textureFormat = OutputFormat.PNG;
        private string replacePrefix = "_ORM";

        [MenuItem("Tools/ORM Tools/ORM Texture Converter")]
        public static void ShowWindow()
        {
            ORMEditor window = GetWindow<ORMEditor>(false, "ORM Texture Converter");
            window.Initialize();
        }

        private void Initialize()
        {
            ormTextures = new();
            rgba = new int[4];

            for (int i = _textureSupportedResolutionMin; i <= _textureSupportedResolutionMax; i *= 2)
            {
                _textureResolutions.Add(i);
                _textureResolutionsNames.Add(i.ToString());
            }
        }

        private Material GetMaterial()
        {
            return new(Shader.Find("Custom/ORMMaskMap"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            headerStyle.normal.textColor = Color.white;
            GUIContent title = new("ORM Texture Converter");
            title.text = title.text.ToUpper();

            Rect rect = GUILayoutUtility.GetRect(1, 30);
            ColorUtility.TryParseHtmlString("#181818", out Color color);
            EditorGUI.DrawRect(rect, color);
            EditorGUI.LabelField(rect, title, headerStyle);
        }

        private void CreateDropRectangle()
        {
            GUIStyle centeredStyle = new(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                {
                    Rect dropRect = EditorGUILayout.GetControlRect(GUILayout.Width(150f), GUILayout.Height(30f));
                    GUI.Box(dropRect, "Drop ORM Textures", centeredStyle);

                    Event evt = Event.current;
                    switch (evt.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (!dropRect.Contains(evt.mousePosition))
                                return;

                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();

                                foreach (var drag in DragAndDrop.objectReferences)
                                {
                                    if (drag is Texture2D texture2D)
                                        ormTextures.Add(texture2D);
                                }
                            }

                            Event.current.Use();
                            break;
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void OnGUI()
        {
            Rect editorAreaRect = new(0, 0, position.width, position.height);
            editorAreaRect.y += Spacing;
            editorAreaRect.yMax -= Spacing;
            editorAreaRect.xMin += Spacing;
            editorAreaRect.xMax -= Spacing;

            using(new GUILayout.AreaScope(editorAreaRect))
            {
                DrawHeader();
                EditorGUILayout.Space();

                CreateDropRectangle();
                EditorGUILayout.Space();

                Rect scrollViewRect = EditorGUILayout.GetControlRect();
                scrollViewRect.yMax = editorAreaRect.yMax - 160f;
                DrawOutline(scrollViewRect, new RectOffset(1, 1, 1, 1));

                float contentHeight = ormTextures.Count * 20f;
                Rect viewRect = new(0, 0, scrollViewRect.width - 13f, contentHeight);

                using (var scrollScope = new GUI.ScrollViewScope(scrollViewRect, columnScrollPosition, viewRect))
                {
                    columnScrollPosition = scrollScope.scrollPosition;
                    Event e = Event.current;

                    using (new GUILayout.VerticalScope())
                    {
                        for (int i = 0; i < ormTextures.Count; i++)
                        {
                            DrawRow(i, viewRect.width, e);
                        }
                    }
                }

                Rect bottomRect = editorAreaRect;
                bottomRect.xMax -= Spacing - 4f;
                bottomRect.yMin = scrollViewRect.yMax + 4f;
                bottomRect.x -= 4f;

                using (new GUILayout.AreaScope(bottomRect))
                {
                    EditorGUILayout.Space();
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Options", EditorStyles.miniBoldLabel);
                        DrawRGBAField();
                        textureFormat = (OutputFormat)EditorGUILayout.EnumPopup("Format", textureFormat);
                        resolution = EditorGUILayout.IntPopup("Resolution", resolution, _textureResolutionsNames.ToArray(), _textureResolutions.ToArray());
                        replacePrefix = EditorGUILayout.TextField("Replace Prefix", replacePrefix);
                    }

                    EditorGUILayout.Space(1f);
                    if (GUILayout.Button("Export Textures", GUILayout.Height(30f)))
                    {
                        if (ormTextures == null || ormTextures.Count == 0)
                        {
                            Debug.Log("No textures to export.");
                            return;
                        }

                        string folderPath = AssetDatabase.GetAssetPath(ormTextures[0]);
                        folderPath = string.Join('/', folderPath.Split('/')[1..^1]);
                        string absolutePath = Path.Combine(Application.dataPath, folderPath);

                        string savePath = EditorUtility.SaveFolderPanel("Save Mask Map", absolutePath, "");
                        string extension = textureFormat.ToString().ToLower();

                        if (string.IsNullOrEmpty(savePath))
                        {
                            Debug.Log("Export canceled.");
                            return;
                        }

                        foreach (var texture in ormTextures)
                        {
                            Material material = GetMaterial();
                            material.SetTexture("_ORM", texture);
                            material.SetFloat("_R", rgba[0]);
                            material.SetFloat("_G", rgba[1]);
                            material.SetFloat("_B", rgba[2]);
                            material.SetFloat("_A", rgba[3]);

                            Texture2D outputTexture = GenerateTexture(resolution, resolution, material);
                            string filename = string.IsNullOrEmpty(replacePrefix)
                                ? texture.name.Replace("_ORM", "_MaskMap")
                                : texture.name.Replace(replacePrefix, "_MaskMap");

                            string fullPath = Path.Combine(savePath, filename + "." + extension);

                            if (textureFormat == OutputFormat.JPG)
                                File.WriteAllBytes(fullPath, outputTexture.EncodeToJPG());
                            else if (textureFormat == OutputFormat.PNG)
                                File.WriteAllBytes(fullPath, outputTexture.EncodeToPNG());
                            else
                                File.WriteAllBytes(fullPath, outputTexture.EncodeToEXR());
                        }

                        Debug.Log("Texture converted successfully.");
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        private void DrawRow(int index, float width, Event e)
        {
            Texture2D texture = ormTextures[index];

            if (texture != null)
            {
                Rect rect = new(0, index * 20, width, 20);
                Color color = rect.Contains(e.mousePosition)
                    ? new Color(0.1f, 0.1f, 0.1f, 0.4f)
                    : Color.clear;

                Rect labelRect = rect;
                labelRect.xMin += 2f;

                EditorGUI.DrawRect(rect, color);
                EditorGUI.LabelField(labelRect, texture.name);

                Rect deleteRect = rect;
                deleteRect.xMin = deleteRect.xMax - EditorGUIUtility.singleLineHeight;
                deleteRect.y += 2f;

                GUIContent delete = EditorGUIUtility.TrIconContent("Toolbar Minus");
                if (GUI.Button(deleteRect, delete, EditorStyles.iconButton))
                {
                    ormTextures.RemoveAt(index);
                }
            }
        }

        private void DrawRGBAField()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("RGBA"));

            float buttonWidth = rect.width / 4;
            string[] labels = { "R", "G", "B", "A" };
            string[] rgbaType = { "", "B", "W" };

            // 0 = normal, 1 = black, 2 = white
            for (int i = 0; i < 4; i++)
            {
                Rect buttonRect = new(rect.x + buttonWidth * i, rect.y, buttonWidth, rect.height);

                Color prevColor = GUI.backgroundColor;
                if (rgba[i] != 0) GUI.backgroundColor = buttonColors[rgba[i]];
                {
                    string label = rgba[i] == 0
                        ? labels[i]
                        : labels[i] + $"({rgbaType[rgba[i]]})";

                    if (GUI.Button(buttonRect, label))
                    {
                        rgba[i] = Wrap(rgba[i] + 1, 0, 3);
                    }
                }
                GUI.backgroundColor = prevColor;
            }
        }

        private int Wrap(int value, int min, int max)
        {
            int newValue = value % max;
            if (newValue < min) newValue = max - 1;
            return newValue;
        }

        private Texture2D GenerateTexture(int width, int height, Material mat)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(Texture2D.blackTexture, tempRT, mat);

            Texture2D output = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGBA32, false);
            RenderTexture.active = tempRT;

            output.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            output.Apply();
            output.filterMode = FilterMode.Bilinear;

            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.active = null;

            return output;
        }

        private void DrawOutline(Rect rect, RectOffset border)
        {
            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if (EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color *= color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border.top), EditorGUIUtility.whiteTexture); //top
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - border.bottom, rect.width, border.bottom), EditorGUIUtility.whiteTexture); //bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, border.left, rect.height - 2 * border.left), EditorGUIUtility.whiteTexture); //left
            GUI.DrawTexture(new Rect(rect.xMax - border.right, rect.y + 1, border.right, rect.height - 2 * border.right), EditorGUIUtility.whiteTexture); //right

            GUI.color = orgColor;
        }
    }
}