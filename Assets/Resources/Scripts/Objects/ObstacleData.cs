using UnityEngine;

[System.Serializable]
public class ObstacleData
{
    public string id;
    public GameObject prefab;
    public bool autoCalculateSize = true;
    public float manualWidth = 2f;
    public float heightOffset = 0f; // Để chỉnh nếu pivot không nằm ở chân

    public float GetWidth()
    {
        if (!autoCalculateSize) return manualWidth;
        if (prefab == null) return 2f;

        var col = prefab.GetComponent<BoxCollider2D>();
        if (col != null) return col.size.x * prefab.transform.localScale.x;
        return 2f;
    }
}