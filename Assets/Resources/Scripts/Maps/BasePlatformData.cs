using UnityEngine;

[System.Serializable]
public class BasePlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;

    [Tooltip("Nếu = 0 sẽ tự tính từ Collider")]
    public float manualLength = 0f;

    public float GetLength()
    {
        if (manualLength > 0) return manualLength;
        if (prefab == null) return 20f;

        var col = prefab.GetComponent<BoxCollider2D>();
        // Sử dụng lossyScale để lấy scale toàn cục chính xác nhất
        if (col != null) return col.size.x * prefab.transform.lossyScale.x;

        return 20f;
    }
}