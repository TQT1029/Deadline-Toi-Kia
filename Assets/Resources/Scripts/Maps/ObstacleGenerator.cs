using UnityEngine;
using System.Collections.Generic;

public class ObstacleGenerator : MonoBehaviour
{
    public static ObstacleGenerator Instance;
    private void Awake() => Instance = this;

    [Header("Config")]
    public List<ObstacleData> obstacleLibrary;
    public Transform objectContainer;

    [Header("Settings")]
    [Range(0, 100)] public int spawnChance = 60;
    public float minGap = 5f;
    public float maxGap = 15f;

    // Lưu trữ danh sách các vật cản đã sinh trong lượt này để ItemGenerator check
    public List<Collider2D> spawnedObstacles = new List<Collider2D>();

    public void SpawnObstacles(float startX, float endX, float groundY)
    {
        spawnedObstacles.Clear();
        float currentX = startX + UnityEngine.Random.Range(2f, 5f);

        while (currentX < endX - 2f)
        {
            if (UnityEngine.Random.Range(0, 100) < spawnChance)
            {
                ObstacleData data = GetRandomObstacle();
                if (data != null)
                {
                    // Check xem đủ chỗ không
                    if (currentX + data.Size.x <= endX)
                    {
                        // Tính vị trí (Pivot thường ở đáy hoặc tâm, ở đây giả sử tâm)
                        // Để đặt lên mặt đất: y = groundY + height/2
                        float spawnY = groundY + (data.Size.y / 2f);
                        Vector3 pos = new Vector3(currentX + data.Size.x / 2f, spawnY, 0);

                        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity);
                        obj.transform.SetParent(objectContainer);

                        Collider2D col = obj.GetComponent<Collider2D>();
                        if (col != null) spawnedObstacles.Add(col);

                        currentX += data.Size.x;
                    }
                }
            }

            currentX += UnityEngine.Random.Range(minGap, maxGap);
        }
    }

    private ObstacleData GetRandomObstacle()
    {
        if (obstacleLibrary.Count == 0) return null;
        return obstacleLibrary[UnityEngine.Random.Range(0, obstacleLibrary.Count)];
    }
}