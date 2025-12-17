using UnityEngine;

[System.Serializable]
public class PlatformData
{
    public string id; // Tên định danh (VD: "Short", "Long", "Ice")
    public GameObject prefab; // Prefab của miếng sàn

    [Tooltip("Chiều dài thực tế của miếng sàn này (để tính toán vị trí nối tiếp)")]
    public float length = 20f;

    [Tooltip("Tỉ lệ xuất hiện (Càng cao càng dễ ra)")]
    public float spawnWeight = 10f;
}