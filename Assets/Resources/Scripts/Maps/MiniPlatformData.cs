using UnityEngine;

[System.Serializable]
public class MiniPlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;

    public float Length
    {
        get
        {
            if (prefab != null)
            {
                var col = prefab.GetComponent<BoxCollider2D>();
                if (col != null) return col.size.x * prefab.transform.localScale.x;
            }
            return 5f; // Mặc định
        }
    }
}