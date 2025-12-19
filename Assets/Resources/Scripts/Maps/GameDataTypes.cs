using UnityEngine;

[System.Serializable]
public class BasePlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;
    public float manualLength = 0f;

    public float GetLength()
    {
        if (manualLength > 0) return manualLength;
        if (prefab == null) return 20f;
        var col = prefab.GetComponent<BoxCollider2D>();
        return col != null ? col.size.x * prefab.transform.lossyScale.x : 20f;
    }
}

[System.Serializable]
public class ObstacleData
{
    public string id;
    public GameObject prefab;
    public float heightOffset = 0f;

    public Vector2 GetSize()
    {
        if (prefab == null) return Vector2.one;
        var col = prefab.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            // Dùng scale cục bộ của prefab
            return new Vector2(col.size.x * prefab.transform.localScale.x, col.size.y * prefab.transform.localScale.y);
        }
        return Vector2.one;
    }
}

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
        return col != null ? col.size.x * prefab.transform.localScale.x : 5f;
    }
}

// 4. ITEM DATA CƠ BẢN
[System.Serializable]
public class ItemData
{
    public string id;
    public GameObject prefab;
    public int scoreValue = 1;
    public float spawnWeight = 10f;
}