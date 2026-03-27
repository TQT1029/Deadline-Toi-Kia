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
        private SerializedProperty backgroundLayers, foregroundLayers;
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

            // Ánh xạ biến List mới
            backgroundLayers = serializedObject.FindProperty("backgroundLayers");
            foregroundLayers = serializedObject.FindProperty("foregroundLayers");
            autoSortOnValidate = serializedObject.FindProperty("autoSortOnValidate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 10, 5) };
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Parallax Engine - Manager\nQuản lý trung tâm cho hiệu ứng nền cuộn nhiều lớp đa hướng.", MessageType.Info);
            EditorGUILayout.Space(10);

            DrawSectionHeader("0. Global Settings");
            EditorGUILayout.PropertyField(globalMode, new GUIContent("Global Parallax Mode"));

            DrawSectionHeader("1. System References");
            EditorGUILayout.PropertyField(targetSubject);
            EditorGUILayout.PropertyField(mainCamera);

            DrawSectionHeader("2. X-Axis Behaviors");
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

            DrawSectionHeader("3. Y-Axis Behaviors");
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

            // ==========================================
            // VÙNG GIAO DIỆN LAYER MANAGEMENT MỚI (LIST)
            // ==========================================
            DrawSectionHeader("4. Layer Management & Z-Depth");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Cho phép vẽ List trực tiếp, tham số 'true' để hiện mở rộng các phần tử con
            EditorGUILayout.PropertyField(backgroundLayers, new GUIContent("Background Layers"), true);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(foregroundLayers, new GUIContent("Foreground Layers"), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(autoSortOnValidate, new GUIContent("Auto Sort On Z-Depth Change"));
            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("Apply Auto Z-Depth", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.SortLayersDepth();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Generators", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("+ BG Quad", GUILayout.Height(25)))
            {
                ((ParallaxManager)target).AddBackgroundLayer(true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((ParallaxManager)target).gameObject.scene);
            }
            if (GUILayout.Button("+ BG Sprite", GUILayout.Height(25)))
            {
                ((ParallaxManager)target).AddBackgroundLayer(false);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((ParallaxManager)target).gameObject.scene);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.8f, 0.5f, 0.2f);
            if (GUILayout.Button("+ FG Quad", GUILayout.Height(25)))
            {
                ((ParallaxManager)target).AddForegroundLayer(true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((ParallaxManager)target).gameObject.scene);
            }
            if (GUILayout.Button("+ FG Sprite", GUILayout.Height(25)))
            {
                ((ParallaxManager)target).AddForegroundLayer(false);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((ParallaxManager)target).gameObject.scene);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("Initialize Layers", GUILayout.Height(35)))
            {
                ((ParallaxManager)target).InitializeLayers();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((ParallaxManager)target).gameObject.scene);
            }
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