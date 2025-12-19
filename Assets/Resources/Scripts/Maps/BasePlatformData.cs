using UnityEngine;

[System.Serializable]
public class BasePlatformData
{
    [Header("Identity")]
    public string id;
    public GameObject prefab;

    [Tooltip("Tỉ lệ xuất hiện")]
    public float spawnWeight = 10f;

    [Header("Physics Config")]
    [Tooltip("Chiều dài của sàn (Nếu để 0 sẽ tự tính từ Collider)")]
    public float manualLength = 20f;

    public float Length
    {
        get
        {
            if (prefab != null && manualLength <= 0)
            {
                var col = prefab.GetComponent<BoxCollider2D>();
                if (col != null) return col.size.x * prefab.transform.localScale.x;
            }
            return Mathf.Max(manualLength, 1f);
        }
    }
}