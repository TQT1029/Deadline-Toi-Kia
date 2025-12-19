using UnityEngine;

[System.Serializable]
public class PatternData
{
    public string id;
    public enum PatternType { CodeGenerated, PrefabBased }
    public PatternType type;

    [Header("For Prefab Based")]
    [Tooltip("Prefab chứa các item xếp sẵn (ShapeVLU, ShapeAplus...)")]
    public GameObject patternPrefab;
    [Tooltip("Tỉ lệ thay đổi item trong prefab thành item khác (Mutation)")]
    [Range(0f, 1f)] public float mutationChance = 0.3f;

    [Header("For Code Generated")]
    public CodePatternType codePattern;

    [Tooltip("Tỉ lệ xuất hiện của pattern này")]
    public float spawnWeight = 10f;
}

public enum CodePatternType
{
    None, Line, Grid, Wave, Parabola, ZigZag, Diamond, RectHollow
}