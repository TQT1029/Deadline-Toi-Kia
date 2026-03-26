using UnityEngine;
using UnityEditor;
using ParallaxEngine;

namespace ParallaxEngine.Editor
{
    [CustomEditor(typeof(ParallaxManager))]
    [CanEditMultipleObjects]
    public class ParallaxManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty targetSubject, mainCamera, globalMode;
        private SerializedProperty enableParallaxX, baseScrollSpeedX, velocityMultiplierX, smoothTimeX, parallaxStrengthX;
        private SerializedProperty enableParallaxY, baseScrollSpeedY, velocityMultiplierY, smoothTimeY, parallaxStrengthY;

        // --- CÁC BIẾN MỚI ---
        private SerializedProperty bgQuadCount, bgSpriteCount, defaultBackgroundMaterial;
        private SerializedProperty enableForeground, fgQuadCount, fgSpriteCount;
        private SerializedProperty autoSortOnValidate;

        private GUIStyle headerStyle;

        private void OnEnable()
        {
            targetSubject = serializedObject.FindProperty("targetSubject");
            mainCamera = serializedObject.FindProperty("mainCamera");
            globalMode = serializedObject.FindProperty("globalMode");

            enableParallaxX = serializedObject.FindProperty("enableParallaxX");
            baseScrollSpeedX = serializedObject.FindProperty("baseScrollSpeedX");
            velocityMultiplierX = serializedObject.FindProperty("velocityMultiplierX");
            smoothTimeX = serializedObject.FindProperty("smoothTimeX");
            parallaxStrengthX = serializedObject.FindProperty("parallaxStrengthX");

            enableParallaxY = serializedObject.FindProperty("enableParallaxY");
            baseScrollSpeedY = serializedObject.FindProperty("baseScrollSpeedY");
            velocityMultiplierY = serializedObject.FindProperty("velocityMultiplierY");
            smoothTimeY = serializedObject.FindProperty("smoothTimeY");
            parallaxStrengthY = serializedObject.FindProperty("parallaxStrengthY");

            // Map các biến mới
            bgQuadCount = serializedObject.FindProperty("bgQuadCount");
            bgSpriteCount = serializedObject.FindProperty("bgSpriteCount");
            defaultBackgroundMaterial = serializedObject.FindProperty("defaultBackgroundMaterial");

            enableForeground = serializedObject.FindProperty("enableForeground");
            fgQuadCount = serializedObject.FindProperty("fgQuadCount");
            fgSpriteCount = serializedObject.FindProperty("fgSpriteCount");

            autoSortOnValidate = serializedObject.FindProperty("autoSortOnValidate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Parallax Engine - Manager\nQuản lý trung tâm cho hiệu ứng nền cuộn nhiều lớp đa hướng.", MessageType.Info);
            EditorGUILayout.Space(10);

            DrawSectionHeader("0. Global Settings");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(globalMode, new GUIContent("Global Parallax Mode"));
            EditorGUILayout.EndVertical();

            DrawSectionHeader("1. System References");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(targetSubject);
            EditorGUILayout.PropertyField(mainCamera);

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.5f, 0.3f, 1f);
            if (GUILayout.Button("Initialize Layers (Play Mode)", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.InitializeLayers();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            DrawSectionHeader("2. X-Axis Behaviors");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableParallaxX);
            if (enableParallaxX.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(baseScrollSpeedX);
                EditorGUILayout.PropertyField(velocityMultiplierX);
                EditorGUILayout.PropertyField(smoothTimeX);
                EditorGUILayout.PropertyField(parallaxStrengthX);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            DrawSectionHeader("3. Y-Axis Behaviors");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableParallaxY);
            if (enableParallaxY.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(baseScrollSpeedY);
                EditorGUILayout.PropertyField(velocityMultiplierY);
                EditorGUILayout.PropertyField(smoothTimeY);
                EditorGUILayout.PropertyField(parallaxStrengthY);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // ==========================================
            // VÙNG GIAO DIỆN LAYER GENERATION MỚI
            // ==========================================
            DrawSectionHeader("4. Layer Generation & Z-Depth Sorting");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // --- BACKGROUND ---
            EditorGUILayout.LabelField("Background Layers (Tối thiểu tổng cộng 1)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(bgQuadCount, new GUIContent("Quad Count (Mesh)"));
            EditorGUILayout.PropertyField(bgSpriteCount, new GUIContent("Sprite Count"));
            EditorGUI.indentLevel--;

            // Bắt lỗi: Nếu người dùng nhập 0 cho cả 2, ép mặc định bgQuadCount = 1
            if (bgQuadCount.intValue + bgSpriteCount.intValue < 1)
            {
                bgQuadCount.intValue = 1;
            }

            EditorGUILayout.Space(5);

            // --- FOREGROUND ---
            EditorGUILayout.PropertyField(enableForeground, new GUIContent("Enable Foreground Layers"));
            if (enableForeground.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fgQuadCount, new GUIContent("Quad Count (Mesh)"));
                EditorGUILayout.PropertyField(fgSpriteCount, new GUIContent("Sprite Count"));
                EditorGUI.indentLevel--;
            }
            else
            {
                // Nếu tắt Foreground, reset giá trị về 0 ngầm định
                fgQuadCount.intValue = 0;
                fgSpriteCount.intValue = 0;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(defaultBackgroundMaterial, new GUIContent("Default Material"));
            EditorGUILayout.PropertyField(autoSortOnValidate, new GUIContent("Auto Sort On Z-Depth Change"));

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Generate Layers", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.GenerateBackgroundLayers();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("Apply Auto Z-Depth", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.SortLayersDepth();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, headerStyle);
        }
    }
}