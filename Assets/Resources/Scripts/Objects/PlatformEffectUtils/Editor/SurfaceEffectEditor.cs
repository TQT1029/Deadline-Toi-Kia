using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SurfaceEffect))]
public class SurfaceEffectEditor : Editor
{
    SerializedProperty currentEffect;
    SerializedProperty validTags;
    SerializedProperty appliedForce;
    SerializedProperty forceDirection;

    SerializedProperty enableVisualSquish;
    SerializedProperty squishAmount;
    SerializedProperty downDuration;
    SerializedProperty upDuration;

    private void OnEnable()
    {
        // Liên kết các biến từ script chính
        currentEffect = serializedObject.FindProperty("currentEffect");
        validTags = serializedObject.FindProperty("validTags");
        appliedForce = serializedObject.FindProperty("appliedForce");
        forceDirection = serializedObject.FindProperty("forceDirection");

        enableVisualSquish = serializedObject.FindProperty("enableVisualSquish");
        squishAmount = serializedObject.FindProperty("squishAmount");
        downDuration = serializedObject.FindProperty("downDuration");
        upDuration = serializedObject.FindProperty("upDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Logo hoặc Header cho đẹp mắt
        EditorGUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        EditorGUILayout.LabelField("🌟 Interactive Surface Utility 🌟", titleStyle);
        EditorGUILayout.Space(10);

        // 1. Core Settings
        EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(currentEffect);
        EditorGUILayout.PropertyField(validTags, true);

        EditorGUILayout.Space(10);

        // 2. Dynamic Force Settings
        EditorGUILayout.LabelField("Physics Settings", EditorStyles.boldLabel);

        // Đổi tên label cho phù hợp với loại Enum đang chọn
        string forceLabel = (currentEffect.enumValueIndex == (int)SurfaceEffect.EffectType.Treadmill) ? "Push Force" : "Jump Force";
        EditorGUILayout.PropertyField(appliedForce, new GUIContent(forceLabel));

        // Chỉ hiển thị Force Direction nếu không phải InteractiveBounce (vì Bounce mặc định nảy lên)
        if (currentEffect.enumValueIndex != (int)SurfaceEffect.EffectType.InteractiveBounce)
        {
            EditorGUILayout.PropertyField(forceDirection);
        }

        EditorGUILayout.Space(10);

        // 3. Dynamic Visual Settings
        // Treadmill thường không có hoạt ảnh lún nảy, nên ta ẩn nó đi cho gọn
        if (currentEffect.enumValueIndex != (int)SurfaceEffect.EffectType.Treadmill)
        {
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableVisualSquish);

            // Nếu bật Squish thì mới hiện các thông số bên dưới
            if (enableVisualSquish.boolValue)
            {
                EditorGUI.indentLevel++; // Thụt lề vào cho đẹp
                EditorGUILayout.PropertyField(squishAmount);
                EditorGUILayout.PropertyField(downDuration);
                EditorGUILayout.PropertyField(upDuration);
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}