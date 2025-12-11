using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string name;
    public GameObject prefab;

    [Tooltip("Giá trị điểm/tiền khi ăn được")]
    public int scoreValue = 10;

    [Tooltip("Tỉ lệ xuất hiện. Bạn có thể điền số nhỏ như 0.01")]
    public float spawnWeight = 10f; // Đổi từ int sang float
}