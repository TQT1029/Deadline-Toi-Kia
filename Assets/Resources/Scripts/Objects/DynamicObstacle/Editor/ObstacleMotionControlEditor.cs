using UnityEngine;
using UnityEditor;
using DG.Tweening;

namespace ProObstacleEngine.Editor
{
    [CustomEditor(typeof(ObstacleMotionControl))]
    [CanEditMultipleObjects]
    public class ObstacleMotionControlEditor : UnityEditor.Editor
    {
        private SerializedProperty groupPattern, loopType, duration, easeType, waveDelayStep;
        private SerializedProperty enableMove, moveOffset;
        private SerializedProperty enableRotate, rotateAngles, continuousSpin;
        private SerializedProperty enableScale, scaleMultiplier;
        private SerializedProperty enableColor, targetColor;
        private SerializedProperty enableShake, shakeStrength;

        private GUIStyle headerStyle;

        private void OnEnable()
        {
            groupPattern = serializedObject.FindProperty("groupPattern");
            loopType = serializedObject.FindProperty("loopType");
            duration = serializedObject.FindProperty("duration");
            easeType = serializedObject.FindProperty("easeType");
            waveDelayStep = serializedObject.FindProperty("waveDelayStep");

            enableMove = serializedObject.FindProperty("enableMove");
            moveOffset = serializedObject.FindProperty("moveOffset");

            enableRotate = serializedObject.FindProperty("enableRotate");
            rotateAngles = serializedObject.FindProperty("rotateAngles");
            continuousSpin = serializedObject.FindProperty("continuousSpin");

            enableScale = serializedObject.FindProperty("enableScale");
            scaleMultiplier = serializedObject.FindProperty("scaleMultiplier");

            enableColor = serializedObject.FindProperty("enableColor");
            targetColor = serializedObject.FindProperty("targetColor");

            enableShake = serializedObject.FindProperty("enableShake");
            shakeStrength = serializedObject.FindProperty("shakeStrength");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 10, 5) };
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("⚙️ ENGINE CHƯỚNG NGẠI VẬT V2 \nHỗ trợ Nhấp nháy màu, Rung lắc bẫy và quản lý Toạ độ thông minh.", MessageType.Info);
            EditorGUILayout.Space(5);

            // --- GENERAL SETTINGS ---
            DrawHeader("1. Timing & Pattern");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(groupPattern);
            if (groupPattern.enumValueIndex != (int)GroupPattern.Sync && groupPattern.enumValueIndex != (int)GroupPattern.Alternating)
            {
                EditorGUILayout.PropertyField(waveDelayStep, new GUIContent("Delay Step", "Thời gian chờ giữa các khối con"));
            }
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(duration, new GUIContent("Cycle Duration"));
            EditorGUILayout.PropertyField(loopType, new GUIContent("Loop Type"));
            EditorGUILayout.PropertyField(easeType, new GUIContent("Motion Ease"));
            EditorGUILayout.EndVertical();

            // --- MOVEMENT ---
            DrawHeader("2. Movement (Position)");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableMove, new GUIContent("Enable Move"));
            if (enableMove.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(moveOffset, new GUIContent("Target Offset"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- ROTATION ---
            DrawHeader("3. Rotation");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableRotate, new GUIContent("Enable Rotation"));
            if (enableRotate.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(continuousSpin, new GUIContent("Continuous Spin (360)"));
                EditorGUILayout.PropertyField(rotateAngles, new GUIContent(continuousSpin.boolValue ? "Spin Speed (Angles/Cycle)" : "Target Angles"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- SCALE ---
            DrawHeader("4. Scale");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableScale, new GUIContent("Enable Scale"));
            if (enableScale.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(scaleMultiplier, new GUIContent("Scale Multiplier"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- COLOR & SHAKE ---
            DrawHeader("5. Special Effects");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(enableColor, new GUIContent("Enable Color Pulse"));
            if (enableColor.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(targetColor, new GUIContent("Target Color"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.PropertyField(enableShake, new GUIContent("Enable Trap Shake"));
            if (enableShake.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(shakeStrength, new GUIContent("Shake Strength"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // --- ACTIONS ---
            EditorGUILayout.Space(10);
            if (GUILayout.Button("🔧 Fetch Children Parts", GUILayout.Height(30)))
            {
                ((ObstacleMotionControl)target).SetupParts();
                Debug.Log("Obstacle Engine: Đã cập nhật lại các bộ phận con!");
            }

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("▶ Update Motion Live", GUILayout.Height(35)))
                {
                    ((ObstacleMotionControl)target).ApplyMotion();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Hãy vào Play Mode để có thể Preview chuyển động.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(string title)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, headerStyle);
        }
    }
}