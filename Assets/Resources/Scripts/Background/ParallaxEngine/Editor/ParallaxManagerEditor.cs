using UnityEngine;
using UnityEditor;
using ParallaxEngine;

namespace ParallaxEngine.Editor
{
    [CustomEditor(typeof(ParallaxManager))]
    [CanEditMultipleObjects]
    public class ParallaxManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty targetSubject, mainCamera;
        private SerializedProperty globalMode;
        private SerializedProperty enableParallaxX, baseScrollSpeedX, velocityMultiplierX, smoothTimeX, parallaxStrengthX;
        private SerializedProperty enableParallaxY, baseScrollSpeedY, velocityMultiplierY, smoothTimeY, parallaxStrengthY;

        // Các property cấu hình Z đã được tinh giản
        private SerializedProperty foregroundLayerCount, autoSortOnValidate;

        private SerializedProperty numberOfBackgroundLayers;
        private SerializedProperty defaultBackgroundMaterial;
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

            // Chỉ còn giữ lại 2 property này
            foregroundLayerCount = serializedObject.FindProperty("foregroundLayerCount");
            autoSortOnValidate = serializedObject.FindProperty("autoSortOnValidate");

            numberOfBackgroundLayers = serializedObject.FindProperty("numberOfBackgroundLayers");
            defaultBackgroundMaterial = serializedObject.FindProperty("defaultBackgroundMaterial");
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
            EditorGUILayout.PropertyField(globalMode, new GUIContent("Global Parallax Mode", "Chế độ di chuyển chung cho tất cả các layer"));
            EditorGUILayout.EndVertical();

            DrawSectionHeader("1. System References");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(targetSubject, new GUIContent("Target (Player)", targetSubject.tooltip));
            EditorGUILayout.PropertyField(mainCamera, new GUIContent("Main Camera", mainCamera.tooltip));
            EditorGUILayout.EndVertical();

            DrawSectionHeader("2. X-Axis Behaviors");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableParallaxX, new GUIContent("Enable X Scroll", enableParallaxX.tooltip));
            if (enableParallaxX.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(baseScrollSpeedX, new GUIContent("Auto Scroll Speed (±)", baseScrollSpeedX.tooltip));
                EditorGUILayout.PropertyField(velocityMultiplierX, new GUIContent("Velocity Multiplier", velocityMultiplierX.tooltip));
                EditorGUILayout.PropertyField(smoothTimeX, new GUIContent("Movement Smoothing", smoothTimeX.tooltip));
                EditorGUILayout.PropertyField(parallaxStrengthX, new GUIContent("Depth Strength Multiplier", parallaxStrengthX.tooltip));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            DrawSectionHeader("3. Y-Axis Behaviors");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableParallaxY, new GUIContent("Enable Y Scroll", enableParallaxY.tooltip));
            if (enableParallaxY.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(baseScrollSpeedY, new GUIContent("Auto Scroll Speed (±)", baseScrollSpeedY.tooltip));
                EditorGUILayout.PropertyField(velocityMultiplierY, new GUIContent("Velocity Multiplier", velocityMultiplierY.tooltip));
                EditorGUILayout.PropertyField(smoothTimeY, new GUIContent("Movement Smoothing", smoothTimeY.tooltip));
                EditorGUILayout.PropertyField(parallaxStrengthY, new GUIContent("Depth Strength Multiplier", parallaxStrengthY.tooltip));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- GIAO DIỆN Z-DEPTH MỚI ---
            DrawSectionHeader("4. Auto Z-Depth Sorting");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Độ sâu Z được tính toán hoàn toàn tự động dựa theo Camera và Player:\n" +
                "• Foreground: Từ [Z Cam + 1] đến [Z Player - 1]\n" +
                "• Background: Từ [Z Player + 5] đến [Z = 100]",
                MessageType.None);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(foregroundLayerCount, new GUIContent("Foreground Layers", foregroundLayerCount.tooltip));
            EditorGUILayout.PropertyField(autoSortOnValidate, new GUIContent("Auto Sort On Change", autoSortOnValidate.tooltip));

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("Apply Auto Z-Depth Sort", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.SortLayersDepth();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            DrawSectionHeader("5. Layer Generation");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(numberOfBackgroundLayers, new GUIContent("Number Of Layers", "Số lượng layer Background muốn tạo."));
            EditorGUILayout.PropertyField(defaultBackgroundMaterial, new GUIContent("Default Material", "Material mặc định gán cho các layer vừa tạo."));

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Generate Background Layers", GUILayout.Height(30)))
            {
                ParallaxManager manager = (ParallaxManager)target;
                manager.GenerateBackgroundLayers();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
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