using UnityEngine;
using System.Collections;

public class ObstacleBossController : MonoBehaviour
{
    [Header("Assets")]
    public MoveObstacleBoss obstaclePrefab;

    [Header("Settings")]
    public float baseMoveSpeed = 10f;   // Tốc độ di chuyển (Unit/giây)
    public float baseRotateSpeed = 2f;  // Tốc độ xoay (Vòng/giây)

    public enum AttackPattern
    {
        RainDown_AllAtOnce,     // Rơi thẳng hàng cùng lúc
        RainDown_Wave,          // Rơi lượn sóng (trái qua phải)
        Side_RightToLeft,       // Bay từ phải sang trái
        Cross_Screen,           // Bay chéo chữ X
        Random_Rain             // Rơi vị trí ngẫu nhiên
    }

    public void ExecuteAttack(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.RainDown_AllAtOnce:
                SpawnVerticalRow(false);
                break;
            case AttackPattern.RainDown_Wave:
                SpawnVerticalRow(true);
                break;
            case AttackPattern.Side_RightToLeft:
                SpawnHorizontalWaves();
                break;
            case AttackPattern.Cross_Screen:
                SpawnCrossPattern();
                break;
            case AttackPattern.Random_Rain:
                SpawnRandomRain();
                break;
        }
    }

    // --- CÁC LOGIC SPAWN ---

    // 1. Rơi từ trên xuống (Thẳng hàng hoặc Lượn sóng)
    private void SpawnVerticalRow(bool isWave)
    {
        int count = 5;
        float step = 1f / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float viewportX = step * i;
            float delay = isWave ? i * 0.15f : 0f;

            // Start: Y=1.2 (Trên đỉnh), End: Y=-0.2 (Dưới đáy)
            CreateObstacle(
                new Vector2(viewportX, 1.2f),
                new Vector2(viewportX, -0.2f),
                baseMoveSpeed,
                delay
            );
        }
    }

    // 2. Bay từ Phải sang Trái (Nhiều độ cao khác nhau)
    private void SpawnHorizontalWaves()
    {
        // Sinh ra 3 vật thể ở 3 độ cao khác nhau
        float[] heights = { 0.2f, 0.5f, 0.8f }; // Thấp, Giữa, Cao (theo Viewport Y)

        for (int i = 0; i < heights.Length; i++)
        {
            // Start: X=1.2 (Bên phải), End: X=-0.2 (Bên trái)
            // Delay mỗi dòng 1 chút để người chơi kịp né
            CreateObstacle(
                new Vector2(1.2f, heights[i]),
                new Vector2(-0.2f, heights[i]),
                baseMoveSpeed * 1.2f, // Bay ngang nhanh hơn chút cho khó
                i * 0.3f // Delay
            );
        }
    }

    // 3. Bay chéo chữ X (Góc màn hình lao vào góc đối diện)
    private void SpawnCrossPattern()
    {
        // Trái trên -> Phải dưới
        CreateObstacle(new Vector2(-0.2f, 1.2f), new Vector2(1.2f, -0.2f), baseMoveSpeed * 1.5f, 0f);

        // Phải trên -> Trái dưới (Delay 1 tí để không dính nhau)
        CreateObstacle(new Vector2(1.2f, 1.2f), new Vector2(-0.2f, -0.2f), baseMoveSpeed * 1.5f, 0.3f);
    }

    // 4. Rơi ngẫu nhiên lả tả
    private void SpawnRandomRain()
    {
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(0.1f, 0.9f);
            float randomDelay = Random.Range(0f, 0.5f);

            CreateObstacle(
                new Vector2(randomX, 1.2f),
                new Vector2(randomX, -0.2f),
                baseMoveSpeed,
                randomDelay
            );
        }
    }

    // Hàm helper chung
    private void CreateObstacle(Vector2 startView, Vector2 endView, float speed, float delay)
    {
        MoveObstacleBoss obj = Instantiate(obstaclePrefab);
        obj.transform.SetParent(transform);

        // Truyền speed và rotateSpeed vào
        obj.Initialize(startView, endView, speed, baseRotateSpeed, delay);
    }
}