//
// Texture3D Importer for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture3DAtlasImportPipeline
//
#pragma warning disable IDE1006, IDE0017
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine.Rendering;

namespace Oddworm.EditorFramework
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Texture3DAtlasImporter), true)]
    class Texture3DAtlasImporterInspector : ScriptedImporterEditor
    {
        class Styles
        {
            public readonly GUIContent[] previewButtonContents =
            {
                EditorGUIUtility.TrIconContent("PreTexRGB"),
                EditorGUIUtility.TrIconContent("PreTexR"),
                EditorGUIUtility.TrIconContent("PreTexG"),
                EditorGUIUtility.TrIconContent("PreTexB"),
                EditorGUIUtility.TrIconContent("PreTexA")
            };

            public readonly GUIStyle toolbarButton = "toolbarbutton";
            public readonly GUIStyle preButton = "RL FooterButton";
            public readonly Texture2D popupIcon = EditorGUIUtility.FindTexture("_Popup");
            public readonly Texture2D errorIcon = EditorGUIUtility.FindTexture("console.erroricon.sml");
            public readonly Texture2D warningIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
            public readonly GUIContent textureTypeLabel = new GUIContent("Texture Type");
            public readonly GUIContent textureTypeValue = new GUIContent("Default");
            public readonly GUIContent textureShapeLabel = new GUIContent("Texture Shape");
            public readonly GUIContent textureShapeValue = new GUIContent("3D");
            public readonly GUIContent wrapModeLabel = new GUIContent("Wrap Mode", "Select how the Texture behaves when tiled.");
            public readonly GUIContent filterModeLabel = new GUIContent("Filter Mode", "Select how the Texture is filtered when it gets stretched by 3D transformations.");
            public readonly GUIContent anisoLevelLabel = new GUIContent("Aniso Level", "Increases Texture quality when viewing the Texture at a steep angle. Good for floor and ground Textures.");
            public readonly GUIContent anisotropicFilteringDisable = new GUIContent("Anisotropic filtering is disabled for all textures in Quality Settings.");
            public readonly GUIContent anisotropicFilteringForceEnable = new GUIContent("Anisotropic filtering is enabled for all textures in Quality Settings.");
            public readonly GUIContent texturesHeaderLabel = new GUIContent("Textures", "Drag&drop one or multiple textures here to add them to the list.");
            public readonly GUIContent removeItemButton = new GUIContent("", EditorGUIUtility.FindTexture("Toolbar Minus"), "Remove from list.");
            public readonly GUIStyle stepSlice = "TimeScrubberButton";
            public readonly GUIStyle sliceScrubber = "TimeScrubber";
            public static GUIStyle preSlider = "preSlider";
            public static GUIStyle preSliderThumb = "preSliderThumb";
            public static GUIStyle preLabel = "preLabel";
            public static GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public static GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public static GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public static GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
        }

        static Styles s_Styles;
        Styles styles
        {
            get
            {
                s_Styles = s_Styles ?? new Styles();
                return s_Styles;
            }
        }

        SerializedProperty m_WrapMode = null;
        SerializedProperty m_FilterMode = null;
        SerializedProperty m_AnisoLevel = null;
        SerializedProperty m_Textures = null;
        ReorderableList m_TextureList = null;

        enum PreviewMode
        {
            RGB = ColorWriteMask.All,
            R = ColorWriteMask.Red | ColorWriteMask.Alpha,
            G = ColorWriteMask.Green | ColorWriteMask.Alpha,
            B = ColorWriteMask.Blue | ColorWriteMask.Alpha,
            A = ColorWriteMask.Alpha,
        }

        PreviewMode m_PreviewMode = PreviewMode.RGB;
        float m_PreviewDepth = 0;
        float m_PreviewMipLevel = 0;
        bool m_PreviewValid = false;
        bool m_PreviewInitialized = false;
        Texture3D m_PreviewTexture = null;
        Material m_PreviewMaterial = null;

        public override void OnEnable()
        {
            base.OnEnable();

            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_FilterMode = serializedObject.FindProperty("m_FilterMode");
            m_AnisoLevel = serializedObject.FindProperty("m_AnisoLevel");
            m_Textures = serializedObject.FindProperty("m_Textures");

            m_TextureList = new ReorderableList(serializedObject, m_Textures);
            m_TextureList.displayRemove = false;
            m_TextureList.drawElementCallback += OnDrawElement;
            m_TextureList.drawHeaderCallback += OnDrawHeader;

            m_PreviewValid = false;
            m_PreviewInitialized = false;
        }

        public override void OnDisable()
        {
            if (m_PreviewMaterial != null)
            {
                DestroyImmediate(m_PreviewMaterial);
                m_PreviewMaterial = null;
            }

            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // This is just some visual nonsense to make it look&feel 
            // similar to Unity's Texture Inspector.
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.LabelField(styles.textureTypeLabel, styles.textureTypeValue, EditorStyles.popup);
                EditorGUILayout.LabelField(styles.textureShapeLabel, styles.textureShapeValue, EditorStyles.popup);
                EditorGUILayout.Separator();
            }

            EditorGUILayout.PropertyField(m_WrapMode, styles.wrapModeLabel);
            EditorGUILayout.PropertyField(m_FilterMode, styles.filterModeLabel);
            EditorGUILayout.PropertyField(m_AnisoLevel, styles.anisoLevelLabel);

            // If Aniso is used, check quality settings and displays some info.
            // I've only added this, because Unity is doing it in the Texture Inspector as well.
            if (m_AnisoLevel.intValue > 1)
            {
                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
                    EditorGUILayout.HelpBox(styles.anisotropicFilteringDisable.text, MessageType.Info);

                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable)
                    EditorGUILayout.HelpBox(styles.anisotropicFilteringForceEnable.text, MessageType.Info);
            }

            // Draw the reorderable texture list only if a single asset is selected.
            // This is to avoid issues drawing the list if it contains a different amount of textures.
            if (!serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.Separator();
                m_TextureList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        void OnDrawHeader(Rect rect)
        {
            var label = rect; label.width -= 16;
            var popup = rect; popup.x += label.width; popup.width = 20;

            // Display textures list header
            EditorGUI.LabelField(label, styles.texturesHeaderLabel);

            // Show popup button to open a context menu
            using (new EditorGUI.DisabledGroupScope(m_Textures.hasMultipleDifferentValues))
            {
                if (GUI.Button(popup, styles.popupIcon, EditorStyles.label))
                    ShowHeaderPopupMenu();
            }

            // Handle drag&drop on header label
            if (CanAcceptDragAndDrop(label))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (Event.current.type == EventType.DragPerform)
                    AcceptDragAndDrop();
            }
        }

        void ShowHeaderPopupMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Select Textures"), false, delegate ()
            {
                var importer = target as Texture3DAtlasImporter;
                Selection.objects = importer.textures;
            });

            menu.ShowAsContext();
        }


        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_Textures.arraySize <= index)
                return;

            rect.y += 1;
            rect.height -= 2;

            var r = rect;

            var importer = target as Texture3DAtlasImporter;
            var textureProperty = m_Textures.GetArrayElementAtIndex(index);

            var errorMsg = importer.GetVerifyString(index);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                r = rect;
                rect.width = 24;
                switch (importer.Verify(index))
                {
                    case Texture3DAtlasImporter.VerifyResult.Valid:
                    case Texture3DAtlasImporter.VerifyResult.MasterNull:
                        break;

                    default:
                        EditorGUI.LabelField(rect, new GUIContent(styles.errorIcon, errorMsg));
                        break;
                }
                rect = r;
                rect.width -= 24;
                rect.x += 24;
            }
            else
            {

                r = rect;
                rect.width = 24;
                EditorGUI.LabelField(rect, new GUIContent(string.Format("{0}", index), "Slice"), isFocused ? EditorStyles.whiteLabel : EditorStyles.label);
                rect = r;
                rect.width -= 24;
                rect.x += 24;
            }

            r = rect;
            rect.width -= 18;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, textureProperty, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                // We have to apply modification here, so that Texture3DImporter.Verify has the just changed values
                serializedObject.ApplyModifiedProperties();

                // Make sure we assign assets that exist on disk only.
                // During my tests, when selecting built-in assets,
                // Unity reimports the texture array asset infinitely, which is probably an Unity bug.
                var result = importer.Verify(index);
                if (result == Texture3DAtlasImporter.VerifyResult.NotAnAsset)
                {
                    textureProperty.objectReferenceValue = null;
                    var msg = importer.GetVerifyString(index);
                    Debug.LogError(msg, importer);
                }
            }

            rect = r;
            rect.x += rect.width - 15;
            rect.y += 2;
            rect.width = 20;
            if (GUI.Button(rect, styles.removeItemButton, styles.preButton))
                textureProperty.DeleteCommand();
        }

        bool CanAcceptDragAndDrop(Rect rect)
        {
            if (!rect.Contains(Event.current.mousePosition))
                return false;

            foreach (var obj in DragAndDrop.objectReferences)
            {
                var tex2d = obj as Texture2D;
                if (tex2d != null)
                    return true;
            }

            return false;
        }

        void AcceptDragAndDrop()
        {
            serializedObject.Update();

            // Add all textures from the drag&drop operation
            foreach (var obj in DragAndDrop.objectReferences)
            {
                var tex2d = obj as Texture2D;
                if (tex2d != null)
                {
                    m_Textures.InsertArrayElementAtIndex(m_Textures.arraySize);
                    var e = m_Textures.GetArrayElementAtIndex(m_Textures.arraySize - 1);
                    e.objectReferenceValue = tex2d;
                }
            }

            serializedObject.ApplyModifiedProperties();
            DragAndDrop.AcceptDrag();
        }

        void InitPreview()
        {
            if (m_PreviewInitialized)
                return;

            m_PreviewValid = false;
            m_PreviewInitialized = true;
            m_PreviewTexture = null;
            m_PreviewMipLevel = 0;
            m_PreviewMode = PreviewMode.RGB;

            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return; // various inspector related preview functionality inside Unity requires this

            if (!SystemInfo.supports3DTextures)
                return;

            var shader = Shader.Find("Hidden/Internal-Texture3D-Preview");
            if (shader == null)
                return;

            m_PreviewMaterial = new Material(shader);
            if (m_PreviewMaterial == null)
                return;

            var importer = target as AssetImporter;
            if (importer == null)
                return;

            m_PreviewTexture = AssetDatabase.LoadAssetAtPath<Texture3D>(importer.assetPath);
            if (m_PreviewTexture == null)
                return;

            m_PreviewValid = true;
        }

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
            
            InitPreview();
            if (!m_PreviewValid)
                return;

            // Color mask buttons
            m_PreviewMode = GUILayout.Toggle(m_PreviewMode == PreviewMode.RGB, styles.previewButtonContents[0], styles.toolbarButton) ? PreviewMode.RGB : m_PreviewMode;
            m_PreviewMode = GUILayout.Toggle(m_PreviewMode == PreviewMode.R, styles.previewButtonContents[1], styles.toolbarButton) ? PreviewMode.R : m_PreviewMode;
            m_PreviewMode = GUILayout.Toggle(m_PreviewMode == PreviewMode.G, styles.previewButtonContents[2], styles.toolbarButton) ? PreviewMode.G : m_PreviewMode;
            m_PreviewMode = GUILayout.Toggle(m_PreviewMode == PreviewMode.B, styles.previewButtonContents[3], styles.toolbarButton) ? PreviewMode.B : m_PreviewMode;
            m_PreviewMode = GUILayout.Toggle(m_PreviewMode == PreviewMode.A, styles.previewButtonContents[4], styles.toolbarButton) ? PreviewMode.A : m_PreviewMode;

            // Display mipmap slider
            using (new EditorGUI.DisabledGroupScope(m_PreviewTexture.mipmapCount == 1))
            {
                GUILayout.Box(Styles.smallZoom, Styles.preLabel);
                m_PreviewMipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_PreviewMipLevel, m_PreviewTexture.mipmapCount - 1, 0, Styles.preSlider, Styles.preSliderThumb, GUILayout.MaxWidth(64)));
                GUILayout.Box(Styles.largeZoom, Styles.preLabel);
            }
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);
            InitPreview();

            // Draw toolbar background
            var toolbarRect = r;
            toolbarRect.height = styles.sliceScrubber.CalcHeight(new GUIContent("Wg"), 50);
            if (Event.current.type == EventType.Repaint)
            {
                styles.sliceScrubber.Draw(toolbarRect, GUIContent.none, -1);
            }

            // Draw depth slider
            var sliderRect = toolbarRect;
            sliderRect.x += 4;
            sliderRect.width -= 4+2;
            sliderRect.y += 1; sliderRect.height -= 2;

            m_PreviewDepth = EditorGUI.Slider(sliderRect, m_PreviewDepth, 0, 1);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!m_PreviewValid)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Can't render Texture3D preview.");
                return;
            }

            // Display the currently selected mipmap level
            if (m_PreviewMipLevel != 0)
            {
                var infoRect = r;
                infoRect.y += 8;
                infoRect.height = 30;

                EditorGUI.DropShadowLabel(infoRect, string.Format("Mip {0}", m_PreviewMipLevel));
            }

            // Display the actual preview
            var previewRect = r;
            previewRect.y += 42;
            previewRect.height -= 42 + 24;

            m_PreviewMaterial.SetFloat("_Depth", m_PreviewDepth);

            EditorGUI.DrawPreviewTexture(previewRect,
                m_PreviewTexture,
                m_PreviewMaterial,
                ScaleMode.ScaleToFit,
                0, // default image aspect
                m_PreviewMipLevel,
                (ColorWriteMask)m_PreviewMode);
        }
    }
}
