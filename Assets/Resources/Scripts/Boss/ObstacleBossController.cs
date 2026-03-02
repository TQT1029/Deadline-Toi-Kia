using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleBossController : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private List<MoveObstacleBoss> obstaclePrefabs = new List<MoveObstacleBoss>();

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
    }

    private void Start()
    {
        RecalculateScreenParams();
    }

    // --- LOGIC CỐT LÕI: TÍNH TOÁN SỐ LÀN DỰA TRÊN MÀN HÌNH ---
    private void RecalculateScreenParams()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 1. Tính kích thước thế giới thực của màn hình
        float worldHeight = cam.orthographicSize * 2f;
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
        }
    }

    // ================= IMPLEMENT DYNAMIC PATTERNS =================

    // 1. Grid Wall: Bàn cờ (Tự động scale theo số làn)
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

    // --- CÁC CHIÊU GIỮ NGUYÊN LOGIC ---
    private void SpawnCrossPattern()
    {
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.3f;
        CreateObstacle(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), speed, 0f);
        CreateObstacle(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), speed, 0.5f);
    }

    private void SpawnSniperShot()
    {
        Vector3 playerViewport = Camera.main.WorldToViewportPoint(ReferenceManager.Instance.PlayerRigidbody.position);
        float targetX = Mathf.Clamp(playerViewport.x, 0.1f, 0.9f);
        float speed = CalculateSpeed(Vector2.up) * 1.5f;

        for (int i = 0; i < 3; i++)
            CreateObstacle(new Vector2(targetX, 1.2f), new Vector2(targetX, -0.2f), speed, i * 0.4f);
    }

    private void SpawnBigCenter()
    {
        CreateObstacle(new Vector2(0.5f, 1.2f), new Vector2(0.5f, -0.2f), CalculateSpeed(Vector2.up) * 0.8f, 0f);
    }

    private void SpawnDoubleCrossFast()
    {
        CreateObstacle(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f);
        CreateObstacle(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f);
    }

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

    private void CreateObstacle(Vector2 startView, Vector2 endView, float speed, float delay)
    {
        MoveObstacleBoss obj = GetObstacleBoss();
        obj.transform.SetParent(transform);
        obj.Initialize(startView, endView, speed, baseRotateSpeed, delay);
    }

    private MoveObstacleBoss GetObstacleBoss()
    {
        return Instantiate(RandomUtils.RandomWithDistributedPercent(obstaclePrefabs, 70, 30));
    }
}