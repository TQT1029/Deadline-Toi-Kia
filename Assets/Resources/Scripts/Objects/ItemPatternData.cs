using UnityEngine;

[System.Serializable]
public class ItemPatternData
{
    public string id;
    public enum Type { CodeGenerated, Prefab }
    public Type type;

    [Header("Prefab Settings")]
    public GameObject prefab;
    [Tooltip("Tỉ lệ biến đổi item con thành loại khác (0-1)")]
    [Range(0, 1)] public float mutationRate = 0.3f;

    [Header("Code Settings")]
    public CodePatternShape shape; // Enum: Line, Wave, Grid...
    public bool randomizeCount = true;
    public int count = 5;


    public float spawnWeight = 10f;
}

public enum CodePatternShape { Line, Wave, Parabola, Grid, Triangle }