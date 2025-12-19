using UnityEngine;

[System.Serializable]
public class MiniPlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;

    public float GetLength()
    {
        if (prefab == null) return 5f;
        var col = prefab.GetComponent<BoxCollider2D>();
        // Scale mặc định của prefab khi chưa instantiate
        if (col != null) return col.size.x * prefab.transform.localScale.x;
        return 5f;
    }
}