using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("Config")]
    public List<ItemData> commonItems; // Chứa coin, score value...
    public List<ItemPatternData> patterns;
    public Transform container;

    [Header("Settings")]
    public LayerMask surfaceLayer; // Gồm: Ground, Obstacle, MiniPlatform
    public float spawnInterval = 10f;
    public float raycastHeight = 20f; // Bắn từ trên trời xuống

    public void Spawn(float startX, float endX)
    {
        float currentX = startX + 2f;

        while (currentX < endX - 2f)
        {
            ItemPatternData pattern = GetRandomPattern();

            // 1. Tìm điểm đặt (Raycast xuống đất/vật cản)
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(currentX, raycastHeight), Vector2.down, 50f, surfaceLayer);

            if (hit.collider != null)
            {
                Vector3 targetPos = hit.point;
                GameObject targetObj = hit.collider.gameObject;

                // Nếu là Obstacle hoặc MiniPlatform -> Căn giữa
                // (Giả sử pivot ở center)
                if (targetObj.CompareTag("Obstacle") || targetObj.CompareTag("MiniPlatform"))
                {
                    targetPos.x = targetObj.transform.position.x; // Căn giữa X theo vật cản
                    targetPos.y = hit.collider.bounds.max.y;      // Đặt lên đỉnh
                }

                // Spawn
                if (pattern.type == ItemPatternData.Type.Prefab)
                {
                    SpawnPrefabPattern(pattern, targetPos);
                }
                else
                {
                    SpawnCodePattern(pattern, targetPos);
                }
            }

            currentX += spawnInterval + Random.Range(-2f, 2f);
        }
    }

    void SpawnPrefabPattern(ItemPatternData data, Vector3 pos)
    {
        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, container);

        // Logic Mutation: Duyệt qua các item con
        foreach (Transform child in obj.transform)
        {
            if (Random.value < data.mutationRate)
            {
                // Thay thế bằng Common Item khác
                ItemData newItem = commonItems[Random.Range(0, commonItems.Count)];
                Vector3 childPos = child.position;
                Destroy(child.gameObject);
                Instantiate(newItem.prefab, childPos, Quaternion.identity, obj.transform);
            }
        }
    }

    void SpawnCodePattern(ItemPatternData data, Vector3 origin)
    {
        switch (data.shape)
        {
            case (CodePatternShape.Line):
                {
                    int count = data.randomizeCount ? Random.Range(3, 7) : data.count;
                    float spacing = 1.0f;
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 spawnPos = origin + new Vector3(i * spacing, 0, 0);
                        ItemData item = commonItems[Random.Range(0, commonItems.Count)];
                        Instantiate(item.prefab, spawnPos, Quaternion.identity, container);
                    }
                    break;
                }
            case (CodePatternShape.Triangle):
                {
                    int count = data.randomizeCount ? Random.Range(3, 6) : data.count;
                }

        }
    }

    private ItemPatternData GetRandomPattern() => patterns[Random.Range(0, patterns.Count)];
}