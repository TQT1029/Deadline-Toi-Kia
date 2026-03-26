using UnityEngine;
using UnityEditor;
using ParallaxEngine;

namespace ParallaxEngine.Editor
{
    [CustomEditor(typeof(ParallaxLayer))]
    [CanEditMultipleObjects]
    public class ParallaxLayerEditor : UnityEditor.Editor
    {
        private SerializedProperty overrideGlobalMode;
        private SerializedProperty localMode;

        private SerializedProperty spriteWidth;
        private SerializedProperty spriteHeight;

        private SerializedProperty edgeThreshold;
        private SerializedProperty symmetryMode;

        private GUIStyle headerStyle;

        private void OnEnable()
        {
            overrideGlobalMode = serializedObject.FindProperty("overrideGlobalMode");
            localMode = serializedObject.FindProperty("localMode");

            spriteWidth = serializedObject.FindProperty("spriteWidth");
            spriteHeight = serializedObject.FindProperty("spriteHeight");

            edgeThreshold = serializedObject.FindProperty("edgeThreshold");
            symmetryMode = serializedObject.FindProperty("symmetryMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }

            ParallaxLayer layer = (ParallaxLayer)target;

            EditorGUILayout.Space(5);

            // --- 1. LAYER MODE OVERRIDE ---
            DrawSectionHeader("1. Layer Mode Override");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(overrideGlobalMode, new GUIContent("Override Global Mode"));

            if (overrideGlobalMode.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(localMode, new GUIContent("Local Parallax Mode"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- 2. SIZE SETTINGS ---
            DrawSectionHeader("2. Size Settings");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(spriteWidth, new GUIContent("Sprite Width", "Chiều rộng ảnh để tính loop (0 = tự động lấy)"));
            EditorGUILayout.PropertyField(spriteHeight, new GUIContent("Sprite Height", "Chiều cao ảnh để tính loop (0 = tự động lấy)"));
            EditorGUILayout.EndVertical();

            // --- XÁC ĐỊNH CHẾ ĐỘ HIỆN TẠI ĐỂ HIỂN THỊ LOGIC TƯƠNG ỨNG ---
            ParallaxMode currentEffectiveMode = ParallaxMode.UV_Scroll;
            if (overrideGlobalMode.boolValue)
            {
                currentEffectiveMode = (ParallaxMode)localMode.enumValueIndex;
            }
            else
            {
                ParallaxManager manager = layer.GetComponentInParent<ParallaxManager>();
                if (manager != null)
                {
                    currentEffectiveMode = manager.globalMode;
                }
            }

            // --- 3. ADVANCED REPOSITION LOGIC ---
            // Chỉ hiển thị phần Reposition nếu chế độ là Transform_Move hoặc Infinite_Reposition
            if (currentEffectiveMode == ParallaxMode.Transform_Move || currentEffectiveMode == ParallaxMode.Infinite_Reposition)
            {
                DrawSectionHeader("3. Advanced Reposition");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.PropertyField(edgeThreshold, new GUIContent("Edge Threshold", "Khoảng cách vượt qua rìa Camera trước khi lật"));
                EditorGUILayout.PropertyField(symmetryMode, new GUIContent("Symmetry Axis", "Trục đối xứng để lật vật thể"));

                string symmetryDesc = symmetryMode.enumValueIndex switch
                {
                    (int)RepositionSymmetry.VerticalAxis => "🔁 Lật qua trục dọc (Dùng cho cuộn ngang - Left/Right).",
                    (int)RepositionSymmetry.HorizontalAxis => "🔁 Lật qua trục ngang (Dùng cho cuộn dọc - Up/Down).",
                    (int)RepositionSymmetry.Point => "🔄 Lật đối xứng qua tâm Camera (Dùng cho cuộn chéo).",
                    _ => ""
                };
                EditorGUILayout.HelpBox(symmetryDesc, MessageType.Info);

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Reposition Logic bị ẩn vì Layer đang dùng chế độ UV_Scroll.", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, headerStyle);
        }
    }
}