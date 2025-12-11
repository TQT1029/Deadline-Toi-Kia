using UnityEngine;

[System.Serializable]
public class ObstacleData
{
    public string name;
    public GameObject prefab;
    [Tooltip("Chiều rộng của vật cản (để tính khoảng cách né tránh)")]
    public float width = 2f;
    [Tooltip("Vị trí độ cao để đặt Item lên nóc (tính từ tâm vật thể)")]
    public float topHeightOffset = 1.5f;

    [Header("Item Spawn Range")]
    [Tooltip("Số lượng item tối thiểu có thể xếp trên nóc")]
    public int minItemsOnTop = 1; 
    [Tooltip("Số lượng item tối đa có thể xếp trên nóc")]
    public int maxItemsOnTop = 3; 
}