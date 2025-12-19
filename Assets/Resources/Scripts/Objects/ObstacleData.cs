using UnityEngine;

[System.Serializable]
public class ObstacleData
{
    public string id;
    public GameObject prefab;

    [Tooltip("Tự động tính chiều rộng và chiều cao từ Collider")]
    public bool autoCalculateSize = true;
    public float manualWidth = 2f;
    public float manualHeight = 2f;

    public Vector2 Size
    {
        get
        {
            if (autoCalculateSize && prefab != null)
            {
                var col = prefab.GetComponent<Collider2D>();
                if (col != null) return col.bounds.size;
            }
            return new Vector2(manualWidth, manualHeight);
        }
    }
}