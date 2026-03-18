using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectiesBossController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float baseObstacleSpeed = 8f;
    [Range(0f, 2f)] public float playerVelocityInfluence = 0.5f;
    [SerializeField] private float maxObstacleSpeed = 25f;
    [SerializeField] private float baseRotateSpeed = 2;

    [Header("Dynamic Spacing Settings")]
    [Tooltip("Kích thước va chạm thực tế của vật thể (dùng để tính toán làn)")]
    [SerializeField] private float obstacleHitSize = 1.2f;

    [Tooltip("Khoảng cách đệm giữa các vật thể (1.0 = sát nhau, 1.2 = hở 20%)")]
    [SerializeField] private float spacingDensity = 1.1f;

    [Tooltip("Lề màn hình nhỏ (0.05 = 5%)")]
    [SerializeField] private float screenEdgeMargin = 0.05f;

    [SerializeField] private float minSafeTimeGap = 0.35f;

    // --- BIẾN TÍNH TOÁN DYNAMIC ---
    private int currentLaneCount = 4; // Số làn đường tính toán được
    private float verticalMargin;
    private float horizontalMargin;

    private float playerForwardSpeed => ReferenceManager.Instance.PlayerRigidbody.linearVelocityX;

    public enum AttackPattern
    {
        RainDown_AllAtOnce,
        RainDown_Wave,
        Side_RightToLeft_Wave,
        Cross_Screen_X,
        Random_Rain,
        Grid_Wall,
        V_Shape_Attack,
        Sniper_AimPlayer,
        Big_Crush_Center,
        Double_Cross_Fast,
        Horizontal_Stream,
        Fan_Spread_Down,
        Tunnel_Vision,      // 1. Ép 2 bên lề, chỉ chừa đường giữa
        Alternating_Checkers, // 2. Thả bàn cờ so le (mật độ cao)
        Converging_Stream,  // 3. Hai dòng chảy từ góc chụm vào giữa
        Spiral_Rain,        // 4. Mưa xoắn ốc (Sine wave)
        Corner_Ambush       // 5. Tấn công chéo từ 4 góc
    }

    private void Start()
    {
        RecalculateScreenParams();
    }

    // ---  TÍNH TOÁN SỐ LÀN DỰA TRÊN MÀN HÌNH ---
    private void RecalculateScreenParams()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 1. Tính kích thước thế giới thực của màn hình
        float worldHeight = cam.orthographicSize * 2.5f;
        float worldWidth = worldHeight * cam.aspect;

        // 2. Tính lề an toàn để spawn object ngoài màn hình
        verticalMargin = (obstacleHitSize / worldHeight) + 0.1f;
        horizontalMargin = (obstacleHitSize / worldWidth) + 0.1f;

        // 3. Tính số lượng làn (Lane) tối đa có thể nhét vào chiều ngang
        // Công thức: Chiều rộng / (Kích thước vật + khoảng hở)
        float availableWidth = worldWidth * (1f - (screenEdgeMargin * 2)); // Trừ hao lề 2 bên
        float singleLaneWidth = obstacleHitSize * spacingDensity;

        int calculatedLanes = Mathf.FloorToInt(availableWidth / singleLaneWidth);

        // Kẹp giá trị để không bị quá ít (dễ quá) hoặc quá nhiều (lag/khó quá)
        // Ví dụ: Tablet có thể lên tới 8 làn, điện thoại dọc có thể là 4-5 làn
        currentLaneCount = Mathf.Clamp(calculatedLanes, 3, 8);
    }

    // Helper: Lấy vị trí X (Viewport 0-1) của làn thứ i
    private float GetLaneCenterViewport(int laneIndex)
    {
        // Chia đoạn từ [margin] đến [1-margin] thành các phần bằng nhau
        float t = (float)laneIndex / (currentLaneCount - 1);
        return Mathf.Lerp(screenEdgeMargin, 1f - screenEdgeMargin, t);
    }

    public void ExecuteAttack(AttackPattern pattern)
    {
        RecalculateScreenParams(); // Luôn tính lại trước khi đánh để khớp với Zoom

        switch (pattern)
        {
            case AttackPattern.RainDown_AllAtOnce: SpawnVerticalRow(false); break;
            case AttackPattern.RainDown_Wave: SpawnVerticalRow(true); break;
            case AttackPattern.Side_RightToLeft_Wave: SpawnHorizontalWaves(); break;
            case AttackPattern.Cross_Screen_X: SpawnCrossPattern(); break;
            case AttackPattern.Random_Rain: SpawnRandomRainSafe(); break;
            case AttackPattern.Grid_Wall: SpawnGridWall(); break;
            case AttackPattern.V_Shape_Attack: SpawnVShape(); break;
            case AttackPattern.Sniper_AimPlayer: SpawnSniperShot(); break;
            case AttackPattern.Big_Crush_Center: SpawnBigCenter(); break;
            case AttackPattern.Double_Cross_Fast: SpawnDoubleCrossFast(); break;
            case AttackPattern.Horizontal_Stream: SpawnHorizontalStream(); break;
            case AttackPattern.Fan_Spread_Down: SpawnFanSpread(); break;
            case AttackPattern.Tunnel_Vision: SpawnTunnelVision(); break;
            case AttackPattern.Alternating_Checkers: SpawnAlternatingCheckers(); break;
            case AttackPattern.Converging_Stream: SpawnConvergingStream(); break;
            case AttackPattern.Spiral_Rain: SpawnSpiralRain(); break;
            case AttackPattern.Corner_Ambush: SpawnCornerAmbush(); break;
        }
    }

    // ================= IMPLEMENT DYNAMIC PATTERNS =================

    // 1. Grid Wall: Bàn cờ 
    private void SpawnGridWall()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float currentSpeed = CalculateOptimalSpeed(Vector2.up * (startY - endY)) * 0.9f;

        // Hàng 1: Spawn ở các làn CHẴN
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateObstacle(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), currentSpeed, 0f);
        }

        // Hàng 2: Spawn ở các làn LẺ (Delay để tạo kẽ hở chéo)
        float rowDelay = 0.6f;
        for (int i = 1; i < currentLaneCount; i += 2)
        {
            CreateObstacle(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), currentSpeed, rowDelay);
        }
    }

    // 2. Vertical Row (Tường rơi): Đảm bảo luôn có khe hở
    private void SpawnVerticalRow(bool isWave)
    {
        // Logic chọn khe hở thông minh dựa trên số làn
        List<int> safeLanes = new List<int>();

        // Luôn có ít nhất 1 khe
        safeLanes.Add(Random.Range(0, currentLaneCount));

        // Nếu màn hình to (nhiều làn), thêm khe thứ 2 để người chơi dễ thở
        if (currentLaneCount >= 5)
        {
            int secondSafe;
            do { secondSafe = Random.Range(0, currentLaneCount); } while (safeLanes.Contains(secondSafe));
            safeLanes.Add(secondSafe);
        }

        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float currentSpeed = CalculateOptimalSpeed(Vector2.up * (startY - endY));
        float safeDelay = CalculateSafeDelay(currentSpeed);

        for (int i = 0; i < currentLaneCount; i++)
        {
            if (safeLanes.Contains(i)) continue; // Bỏ qua làn an toàn

            float x = GetLaneCenterViewport(i);

            // Nếu là Wave, delay tỏa ra từ khe hở đầu tiên
            float delay = isWave ? Mathf.Abs(i - safeLanes[0]) * safeDelay * 0.5f : 0f;

            CreateObstacle(new Vector2(x, startY), new Vector2(x, endY), currentSpeed, delay);
        }
    }

    // 3. V Shape: Tự động căn giữa bất kể số làn
    private void SpawnVShape()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateSpeed(Vector2.up * 2f);

        int centerIndex = currentLaneCount / 2; // Làn giữa

        // Spawn từ giữa ra 2 bên
        // Giới hạn chỉ spawn tối đa 3 lớp V để không quá rộng
        int layers = Mathf.Min(3, centerIndex + 1);

        for (int offset = 0; offset < layers; offset++)
        {
            float delay = offset * 0.3f;

            // Bên trái
            int leftIndex = centerIndex - offset;
            if (leftIndex >= 0)
                CreateObstacle(new Vector2(GetLaneCenterViewport(leftIndex), startY), new Vector2(GetLaneCenterViewport(leftIndex), endY), speed, delay);

            // Bên phải (chỉ spawn nếu không trùng với bên trái - tức là khác 0)
            if (offset != 0)
            {
                int rightIndex = centerIndex + offset;
                if (rightIndex < currentLaneCount)
                    CreateObstacle(new Vector2(GetLaneCenterViewport(rightIndex), startY), new Vector2(GetLaneCenterViewport(rightIndex), endY), speed, delay);
            }
        }
    }

    // 4. Mưa ngẫu nhiên an toàn
    private void SpawnRandomRainSafe()
    {
        // Số lượng hạt mưa = Tổng số làn - 1 (Luôn chừa 1 làn trống)
        int rainCount = Mathf.Max(1, currentLaneCount - 1);

        List<int> availableLanes = new List<int>();
        for (int i = 0; i < currentLaneCount; i++) availableLanes.Add(i);

        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up);

        for (int i = 0; i < rainCount; i++)
        {
            if (availableLanes.Count == 0) break;

            int rIndex = Random.Range(0, availableLanes.Count);
            int lane = availableLanes[rIndex];
            availableLanes.RemoveAt(rIndex);

            float delay = i * 0.15f;
            CreateObstacle(new Vector2(GetLaneCenterViewport(lane), startY), new Vector2(GetLaneCenterViewport(lane), endY), speed, delay);
        }
    }

    // 5. Fan Spread: Rải đều theo số làn
    private void SpawnFanSpread()
    {
        Vector2 startPoint = new Vector2(0.5f, 1.2f);
        float speed = CalculateOptimalSpeed(Vector2.up);

        for (int i = 0; i < currentLaneCount; i++)
        {
            float targetX = GetLaneCenterViewport(i);

            // Tạo khe hở ở giữa màn hình (Lane giữa)
            // Nếu i là lane giữa hoặc sát giữa thì bỏ qua
            if (Mathf.Abs(i - (currentLaneCount / 2f)) < 0.6f) continue;

            CreateObstacle(startPoint, new Vector2(targetX, -0.2f), speed, i * 0.1f);
        }
    }

    // 6. Horizontal Waves: Tính lại độ cao Y an toàn
    private void SpawnHorizontalWaves()
    {
        // Chia chiều dọc thành 3 phần (Thấp, Giữa, Cao)
        float[] fixedY = { 0.15f, 0.5f, 0.85f };

        float startX = 1f + horizontalMargin;
        float endX = 0f - horizontalMargin;
        float speed = CalculateOptimalSpeed(Vector2.right * 2f) * 1.2f;

        // Pattern ngẫu nhiên như cũ
        int patternType = Random.Range(0, 3);
        switch (patternType)
        {
            case 0: // Thấp + Cao
                CreateObstacle(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0);
                CreateObstacle(new Vector2(startX, fixedY[2]), new Vector2(endX, fixedY[2]), speed, 0);
                break;
            case 1: // Cầu thang
                CreateObstacle(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0);
                CreateObstacle(new Vector2(startX, fixedY[1]), new Vector2(endX, fixedY[1]), speed, 0.4f);
                CreateObstacle(new Vector2(startX, fixedY[2]), new Vector2(endX, fixedY[2]), speed, 0.8f);
                break;
            case 2: // So le
                CreateObstacle(new Vector2(startX, fixedY[1]), new Vector2(endX, fixedY[1]), speed, 0);
                CreateObstacle(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0.5f);
                break;
        }
    }

    // 7. Tunnel Vision: Tạo 2 bức tường dày ở 2 bên lề, ép người chơi chạy vào giữa
    private void SpawnTunnelVision()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.1f;

        // Chỉ chừa lại khoảng 1-2 làn ở giữa là an toàn
        int centerIndex = currentLaneCount / 2;
        int safeWidth = 1; // Số làn an toàn ở giữa

        for (int i = 0; i < currentLaneCount; i++)
        {
            // Nếu làn hiện tại nằm trong vùng an toàn ở giữa -> Bỏ qua
            if (Mathf.Abs(i - centerIndex) <= safeWidth / 2) continue;

            // Spawn liên tiếp 3 object để tạo thành bức tường dài
            for (int j = 0; j < 3; j++)
            {
                CreateObstacle(
                    new Vector2(GetLaneCenterViewport(i), startY + (j * 0.25f)), // Spawn chồng lên nhau theo trục Y
                    new Vector2(GetLaneCenterViewport(i), endY),
                    speed,
                    j * 0.1f // Delay nhỏ để tạo hiệu ứng dây chuyền
                );
            }
        }
    }

    // 8. Alternating Checkers: Thả 2 lớp so le chẵn lẻ với tốc độ cao (Khó hơn Grid Wall cũ)
    private void SpawnAlternatingCheckers()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.2f; // Nhanh hơn bình thường

        // Lớp 1: Các làn Chẵn (0, 2, 4...)
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateObstacle(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0f);
        }

        // Lớp 2: Các làn Lẻ (1, 3, 5...) - Delay ngắn để người chơi phải lạng lách nhanh
        for (int i = 1; i < currentLaneCount; i += 2)
        {
            CreateObstacle(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0.35f);
        }

        // Lớp 3 (Optional): Lặp lại Chẵn để ép góc thêm lần nữa
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateObstacle(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0.7f);
        }
    }

    // 9. Converging Stream: Hai luồng đạn từ 2 góc trên lao chéo vào giữa đáy màn hình
    private void SpawnConvergingStream()
    {
        Vector2 targetBottom = new Vector2(0.5f, -0.2f); // Điểm hội tụ
        float speed = CalculateSpeed(Vector2.up) * 1.3f;
        int bulletCount = 4; // Số lượng đạn mỗi bên

        for (int i = 0; i < bulletCount; i++)
        {
            float delay = i * 0.2f;

            // Luồng bên Trái: Từ (0, 1.2) -> Giữa đáy
            CreateObstacle(new Vector2(0f, 1.2f), targetBottom, speed, delay);

            // Luồng bên Phải: Từ (1, 1.2) -> Giữa đáy
            CreateObstacle(new Vector2(1f, 1.2f), targetBottom, speed, delay);
        }
    }

    // 10. Spiral Rain: Rơi từ trên xuống nhưng vị trí X uốn lượn theo hình Sin (như con rắn)
    private void SpawnSpiralRain()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up);

        int count = 6; // Số lượng vật thể

        for (int i = 0; i < count; i++)
        {
            // Tính toán vị trí X dựa trên hàm Sin
            // i càng lớn thì góc càng thay đổi -> tạo hình lượn sóng
            float t = (float)i / count; // 0 -> 1
            float sinX = 0.5f + (Mathf.Sin(t * Mathf.PI * 2) * 0.4f); // Dao động quanh trục 0.5 với biên độ 0.4

            // Kẹp vào trong màn hình cho an toàn
            sinX = Mathf.Clamp(sinX, screenEdgeMargin, 1f - screenEdgeMargin);

            CreateObstacle(new Vector2(sinX, startY), new Vector2(sinX, endY), speed, i * 0.15f);
        }
    }

    // 11. Corner Ambush: 4 vật thể bay từ 4 góc màn hình cắt chéo qua tâm
    private void SpawnCornerAmbush()
    {
        float speed = CalculateSpeed(Vector2.up) * 1.5f; // Tốc độ rất nhanh
        float delayStep = 0.25f;

        // Góc trên trái -> Góc dưới phải
        CreateObstacle(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), speed, 0f);

        // Góc trên phải -> Góc dưới trái
        CreateObstacle(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), speed, delayStep);

        // (Khó hơn) Góc dưới trái -> Góc trên phải (Bay ngược lên)
        CreateObstacle(new Vector2(0f, -0.2f), new Vector2(1f, 1.2f), speed, delayStep * 2);

        // (Khó hơn) Góc dưới phải -> Góc trên trái
        CreateObstacle(new Vector2(1f, -0.2f), new Vector2(0f, 1.2f), speed, delayStep * 3);
    }

    // 12.
    private void SpawnCrossPattern()
    {
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.3f;
        CreateObstacle(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), speed, 0f);
        CreateObstacle(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), speed, 0.5f);
    }

    // 13.
    private void SpawnSniperShot()
    {
        Vector3 playerViewport = Camera.main.WorldToViewportPoint(ReferenceManager.Instance.PlayerRigidbody.position);
        float targetX = Mathf.Clamp(playerViewport.x + 0.1f, 0.1f, 0.9f);
        float speed = CalculateSpeed(Vector2.up) * 1.5f;

        for (int i = 0; i < 3; i++)
            CreateObstacle(new Vector2(targetX, 1.2f), new Vector2(targetX, -0.2f), speed, i * 0.4f);
    }

    // 14.
    private void SpawnBigCenter()
    {
        CreateObstacle(new Vector2(0.5f, 1.2f), new Vector2(0.5f, -0.2f), CalculateSpeed(Vector2.up) * 0.8f, 0f);
    }

    // 15.
    private void SpawnDoubleCrossFast()
    {
        CreateObstacle(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f);
        CreateObstacle(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f);
    }

    // 16.
    private void SpawnHorizontalStream()
    {
        for (int i = 0; i < 3; i++)
            CreateObstacle(new Vector2(1.2f, 0.2f + i * 0.25f), new Vector2(-0.2f, 0.2f + i * 0.25f), CalculateSpeed(Vector2.right) * 1.2f, i * 0.2f);
    }

    // --- HELPERS ---
    private float CalculateSpeed(Vector2 distVec) => CalculateOptimalSpeed(distVec);

    private float CalculateOptimalSpeed(Vector2 viewDistanceVector)
    {
        float dynamicSpeed = baseObstacleSpeed + (playerForwardSpeed * playerVelocityInfluence);
        return Mathf.Clamp(dynamicSpeed, baseObstacleSpeed, maxObstacleSpeed);
    }

    private float CalculateSafeDelay(float obstacleSpeed)
    {
        return Mathf.Max(minSafeTimeGap, obstacleHitSize / obstacleSpeed);
    }

    // Trong ProjectiesBossController.cs
    private void CreateObstacle(Vector2 startView, Vector2 endView, float speed, float delay)
    {
        TargetProjectiesBoss obj = GetObstacleBoss();
        obj.transform.SetParent(transform);

        Camera cam = Camera.main;
        float zDepth = 10f; // Hoặc -cam.transform.position.z

        // Controller chịu trách nhiệm chuyển đổi tọa độ
        Vector3 startWorld = cam.ViewportToWorldPoint(new Vector3(startView.x, startView.y, zDepth));
        Vector3 endWorld = cam.ViewportToWorldPoint(new Vector3(endView.x, endView.y, zDepth));

        // Đảm bảo Z = 0
        startWorld.z = 0;
        endWorld.z = 0;

        // Truyền tọa độ World vào script TargetProjectiesBoss mới
        obj.Initialize(startWorld, endWorld, speed, baseRotateSpeed, delay);
    }

    private TargetProjectiesBoss GetObstacleBoss()
    {
        return Instantiate(RandomUtils.RandomWithDistributedPercent(BossManager.currentBossData.projectiesObstacle, 80, 20));
    }
}