using UnityEngine;

[System.Serializable]
public class PlatformData
{
    public string id;
    public GameObject prefab;

    [Tooltip("Tỉ lệ xuất hiện (Càng cao càng dễ ra)")]
    public float spawnWeight = 10f;

    [Header("Flying Settings (Cấu hình Bay)")]
    [Tooltip("Đây có phải là sàn bay không?")]
    public bool isFlying = false;

    [Tooltip("Độ cao tối thiểu so với sàn trước đó (VD: 1.0)")]
    public float minHeightDiff = 1.0f;

    [Tooltip("Độ cao tối đa so với sàn trước đó (VD: 3.0)")]
    public float maxHeightDiff = 2.5f;

    [Header("Fallback")]
    public float length = 20f; // Dùng nếu không có Collider
}