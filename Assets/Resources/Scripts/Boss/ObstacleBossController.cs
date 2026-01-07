using UnityEngine;
using System.Collections;

public class ObstacleBossController : MonoBehaviour
{
    [Header("Assets")]
    public MoveObstacleBoss obstaclePrefab;

    [Header("Adaptive Difficulty Settings")]
    // Tốc độ cơ bản khi Player đứng yên hoặc chạy chậm
    public float baseObstacleSpeed = 8f;

    // Tỉ lệ ảnh hưởng của tốc độ Player lên tốc độ đạn Boss (0 = không ảnh hưởng, 1 = tăng tỉ lệ thuận 1:1)
    [Range(0f, 2f)] public float playerVelocityInfluence = 0.5f;

    // Giới hạn tốc độ tối đa của đạn để không bị quá nhanh không thể né
    public float maxObstacleSpeed = 25f;

    private float baseRotateSpeed = 2;

    [Header("Spacing & Bounds Settings")]
    [SerializeField] private float minSafeTimeGap = 0.35f;
    [SerializeField] private float obstacleHitSize = 1.2f; // Kích thước vật thể để tính toán lề màn hình

    // Lề an toàn (Viewport): 0.1 nghĩa là vật thể sẽ spawn ở 1.1 và destroy ở -0.1
    // Tự động tính toán dựa trên hitSize, nhưng có thể override
    private float verticalMargin = 0.2f;
    private float horizontalMargin = 0.2f;

    [Header("Player Reference")]
    private float playerForwardSpeed => ReferenceManager.Instance.PlayerRigidbody.linearVelocityX;

    private void Start()
    {
        // Tự động tính toán Margin dựa trên kích thước vật thể so với màn hình
        // Giúp vật thể vừa khuất bóng là destroy luôn, tối ưu hiệu năng
        CalculateDynamicMargins();
    }

    private void CalculateDynamicMargins()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Chuyển kích thước vật thể từ World sang Viewport unit (ước lượng)
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        // Cộng thêm 1 chút dư (buffer)
        verticalMargin = (obstacleHitSize / worldHeight) + 0.05f;
        horizontalMargin = (obstacleHitSize / worldWidth) + 0.05f;
    }

    public enum AttackPattern
    {
        RainDown_AllAtOnce,
        RainDown_Wave,
        Side_RightToLeft,
        Cross_Screen,
        Random_Rain
    }

    public void ExecuteAttack(AttackPattern pattern)
    {
        // Recalculate margin mỗi lần tấn công phòng trường hợp Camera Zoom thay đổi kích thước Viewport
        CalculateDynamicMargins();

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

    // --- CÁC LOGIC SPAWN ĐÃ TỐI ƯU ---

    private void SpawnVerticalRow(bool isWave)
    {
        int count = 5;
        // Padding 2 bên trái phải để không spawn sát mép màn hình quá
        float paddingX = 0.15f;

        float startY = 1f + verticalMargin; // Spawn ngay trên đỉnh màn hình
        float endY = 0f - verticalMargin;   // Destroy ngay dưới đáy màn hình

        // Tính tốc độ tối ưu cho khoảng cách này
        float currentSpeed = CalculateOptimalSpeed(Vector2.up * (startY - endY));
        float safeDelay = CalculateSafeDelay(currentSpeed);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            // Lerp từ trái qua phải (có trừ hao lề)
            float x = Mathf.Lerp(paddingX, 1f - paddingX, t);

            float delay = isWave ? (count - i) * safeDelay : 0f;

            CreateObstacle(
                new Vector2(x, startY),
                new Vector2(x, endY),
                currentSpeed,
                delay
            );
        }
    }

    private void SpawnHorizontalWaves()
    {
        int count = 4;
        float paddingY = 0f;

        float startX = 1f + horizontalMargin;
        float endX = 0f - horizontalMargin;

        // Tính tốc độ (ưu tiên nhanh hơn 1 chút vì chiều ngang màn hình dài hơn chiều dọc)
        float distVector = Mathf.Abs(startX - endX);
        float currentSpeed = CalculateOptimalSpeed(Vector2.right * distVector) * 1.2f;
        float safeDelay = CalculateSafeDelay(currentSpeed);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float y = Mathf.Lerp(paddingY, 1f - paddingY, t); // Từ dưới lên trên

            // Delay kiểu sóng
            float delay = i * safeDelay * 0.8f;

            CreateObstacle(
                new Vector2(startX, y),
                new Vector2(endX, y),
                currentSpeed,
                delay
            );
        }
    }

    private void SpawnCrossPattern()
    {
        // Tính đường chéo view
        Vector2 start1 = new Vector2(0f - horizontalMargin, 1f + verticalMargin); // Trái Trên
        Vector2 end1 = new Vector2(1f + horizontalMargin, 0f - verticalMargin);   // Phải Dưới

        Vector2 start2 = new Vector2(1f + horizontalMargin, 1f + verticalMargin); // Phải Trên
        Vector2 end2 = new Vector2(0f - horizontalMargin, 0f - verticalMargin);   // Trái Dưới

        // Tốc độ chéo cần nhanh hơn vì quãng đường dài nhất
        float currentSpeed = CalculateOptimalSpeed(start1 - end1) * 1.3f;
        float safeDelay = CalculateSafeDelay(currentSpeed);

        CreateObstacle(start1, end1, currentSpeed, 0f);
        CreateObstacle(start2, end2, currentSpeed, safeDelay); // Delay 1 tí để ko đâm nhau giữa màn hình
    }

    private void SpawnRandomRain()
    {
        int count = 4;

        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float currentSpeed = CalculateOptimalSpeed(Vector2.up * (startY - endY));
        float safeDelay = CalculateSafeDelay(currentSpeed);

        for (int i = 0; i < count; i++)
        {
            // Random X trong vùng an toàn (0.1 -> 0.9)
            float x = Random.Range(0.1f, 0.9f);

            CreateObstacle(
                new Vector2(x, startY),
                new Vector2(x, endY),
                currentSpeed,
                i * safeDelay
            );
        }
    }

    //===== LOGIC TÍNH TOÁN TỐI ƯU (CORE) =====//

    /// <summary>
    /// Tính toán tốc độ đạn dựa trên vận tốc người chơi.
    /// Player chạy càng nhanh, đạn bay càng nhanh để duy trì độ khó (Reaction Time).
    /// </summary>
    private float CalculateOptimalSpeed(Vector2 viewDistanceVector)
    {
        // 1. Tính độ khó hiện tại (Dựa trên tốc độ player)
        // Nếu player chạy nhanh, ta cộng thêm vận tốc vào đạn
        float dynamicSpeed = baseObstacleSpeed + (playerForwardSpeed * playerVelocityInfluence);

        // 2. Kẹp giá trị để không quá chậm hoặc quá nhanh
        dynamicSpeed = Mathf.Clamp(dynamicSpeed, baseObstacleSpeed, maxObstacleSpeed);

        return dynamicSpeed;
    }

    private float CalculateSafeDelay(float obstacleSpeed)
    {
        // Thời gian để vật thể đi qua hết kích thước của chính nó
        // Time = Distance / Speed
        float passThroughTime = obstacleHitSize / obstacleSpeed;

        // Đảm bảo delay tối thiểu là minSafeTimeGap
        return Mathf.Max(minSafeTimeGap, passThroughTime);
    }

    private void CreateObstacle(Vector2 startView, Vector2 endView, float speed, float delay)
    {
        MoveObstacleBoss obj = Instantiate(obstaclePrefab);
        obj.transform.SetParent(transform); // Gọn hierarchy
        obj.Initialize(startView, endView, speed, baseRotateSpeed, delay);
    }
}