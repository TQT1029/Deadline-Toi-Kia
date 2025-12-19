using UnityEngine;
using System.Collections.Generic;

public class MiniPlatformSpawner : MonoBehaviour
{
    [Header("Config")]
    public List<MiniPlatformData> library;
    public Transform container;
    public LayerMask obstacleLayer;

    [Header("Rules")]
    public float heightAboveGround = 3f;
    public float gapBetweenPlats = 1.5f;

    public void Spawn(List<BasePlatformSpawner.PitSegment> pits, List<BasePlatformSpawner.GroundSegment> grounds)
    {
        // 1. Ưu tiên: Bắc cầu qua hố (Bắt buộc)
        foreach (var pit in pits)
        {
            SpawnBridgeOverPit(pit);
        }

        // 2. Tạo đường trên cao ở đất liền (Optional)
        foreach (var ground in grounds)
        {
            if (Random.value < 0.4f) // 40% cơ hội
            {
                SpawnOverGround(ground);
            }
        }
        Physics2D.SyncTransforms();
    }

    void SpawnBridgeOverPit(BasePlatformSpawner.PitSegment pit)
    {
        float currentX = pit.startX - 2f; // Bắt đầu trước hố một chút
        float endX = pit.endX + 2f;       // Kết thúc sau hố một chút

        while (currentX < endX)
        {
            MiniPlatformData data = GetRandom();
            float len = data.GetLength();
            Vector3 pos = new Vector3(currentX + len / 2f, 0, 0); // Y sẽ tính sau

            // Check obstacle bên dưới (ở mép hố) để nâng cao lên
            float targetY = -2f + heightAboveGround; // Mặc định

            // Logic đơn giản: cứ spawn thành chuỗi đi ngang qua
            Instantiate(data.prefab, new Vector3(pos.x, targetY, 0), Quaternion.identity, container);

            currentX += len + gapBetweenPlats;
        }
    }

    void SpawnOverGround(BasePlatformSpawner.GroundSegment ground)
    {
        // Logic kiểm tra vị trí trống để đặt mini platform
        // Dùng Physics.OverlapBox để check xem có Obstacle không
        // Nếu có Obstacle -> Đặt cao hơn Obstacle. Nếu không -> Đặt thấp.
        // (Code rút gọn cho ngắn, bạn có thể mở rộng logic này)
    }

    private MiniPlatformData GetRandom() => library[Random.Range(0, library.Count)];
}