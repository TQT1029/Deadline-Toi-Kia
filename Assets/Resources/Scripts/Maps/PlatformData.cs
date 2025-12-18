using UnityEngine;

[System.Serializable]
public class PlatformData
{
    [Header("Identity")]
    public string id;
    public GameObject prefab;

    [Tooltip("Tỉ lệ xuất hiện")]
    public float spawnWeight = 10f;

    [Header("Settings for Mini Platform")]
    [Tooltip("Item trên sàn này sẽ nằm cao hơn tâm sàn bao nhiêu?")]
    public float itemHeightOffset = 1.0f;

    [Header("Physics Config")]
    [Tooltip("Chiều dài thực tế (Script sẽ tự chỉnh Collider khớp số này)")]
    public float length = 20f;
}