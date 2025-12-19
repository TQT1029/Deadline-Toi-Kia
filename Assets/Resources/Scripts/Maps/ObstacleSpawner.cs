using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Config")]
    public List<ObstacleData> library;
    public Transform container;

    [Header("Rules")]
    public float minGap = 5f;
    public float edgePadding = 2f; // Cách mép đất tối thiểu

    public void Spawn(List<BasePlatformSpawner.GroundSegment> grounds)
    {
        foreach (var ground in grounds)
        {
            float currentX = ground.startX + edgePadding;
            float endLimit = ground.endX - edgePadding;

            while (currentX < endLimit)
            {
                if (Random.value < 0.6f) // 60% có obstacle
                {
                    ObstacleData obs = GetRandom();
                    float width = obs.GetWidth();

                    if (currentX + width <= endLimit)
                    {
                        Vector3 pos = new Vector3(currentX + width / 2f, ground.y + obs.heightOffset, 0);
                        Instantiate(obs.prefab, pos, Quaternion.identity, container);
                        currentX += width;
                    }
                }
                currentX += Random.Range(minGap, minGap * 2);
            }
        }
        Physics2D.SyncTransforms(); // Cập nhật collider obstacle
    }

    private ObstacleData GetRandom() => library[Random.Range(0, library.Count)];
}