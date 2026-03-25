using UnityEngine;
using UnityEditor;

namespace ProObstacleEngine.Editor
{
    [CustomEditor(typeof(ObstacleMotionControl))]
    [CanEditMultipleObjects]
    public class ObstacleMotionControlEditor : UnityEditor.Editor
    {
        private SerializedProperty groupPattern, duration, easeType, waveDelayStep;
        private SerializedProperty enableMove, moveOffset;
        private SerializedProperty enableRotate, rotateAngles, continuousSpin;
        private SerializedProperty enableScale, scaleMultiplier;

        private GUIStyle headerStyle;

        private void OnEnable()
        {
            groupPattern = serializedObject.FindProperty("groupPattern");
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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 10, 5) };
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Bộ điều khiển đa năng cho Chướng ngại vật. Hỗ trợ kết hợp nhiều hiệu ứng cùng lúc.", MessageType.Info);
            EditorGUILayout.Space(5);

            // --- GENERAL SETTINGS ---
            DrawHeader("1. Timing & Pattern");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(groupPattern);
            if (groupPattern.enumValueIndex != (int)GroupPattern.Sync && groupPattern.enumValueIndex != (int)GroupPattern.Alternating)
            {
                EditorGUILayout.PropertyField(waveDelayStep, new GUIContent("Delay Step"));
            }
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(duration, new GUIContent("Cycle Duration"));
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
                EditorGUILayout.PropertyField(rotateAngles, new GUIContent("Target Angles"));
                EditorGUILayout.PropertyField(continuousSpin, new GUIContent("Continuous Spin (360)"));
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

            // --- ACTIONS ---
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Fetch Children Parts", GUILayout.Height(30)))
            {
                ((ObstacleMotionControl)target).SetupParts();
            }

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Update Motion Live", GUILayout.Height(30)))
                {
                    ((ObstacleMotionControl)target).ApplyMotion();
                }
                GUI.backgroundColor = Color.white;
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