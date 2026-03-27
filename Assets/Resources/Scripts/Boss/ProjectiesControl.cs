using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectiesControl : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float baseObstacleSpeed = 8f;
    [Range(0f, 2f)] public float targetVelocityInfluence = 0.5f;
    [SerializeField] private float maxObstacleSpeed = 25f;
    [SerializeField] private float baseRotateSpeed = 2;

    [Header("Dynamic Spacing Settings")]
    [Tooltip("Kích thước va chạm thực tế của vật thể (dùng để tính toán làn)")]
    [SerializeField] private float obstacleHitSize = 1.2f;

    [Tooltip("TĂNG TỪ 1.2 LÊN 1.45 ĐỂ LÀN RỘNG HƠN, ÍT ĐẠN HƠN")]
    [SerializeField] private float spacingDensity = 1.45f; // Cũ: 1.2f

    [Tooltip("Lề màn hình nhỏ")]
    [SerializeField] private float screenEdgeMargin = 0.08f; // Cũ: 0.05f - Bóp lề nhỏ lại để hạn chế đạn sát mép

    [Tooltip("TĂNG TỪ 0.35 LÊN 0.55 ĐỂ ĐẠN RƠI CHẬM NHỊP HƠN THEO CHIỀU DỌC")]
    [SerializeField] private float minSafeTimeGap = 0.55f; // Cũ: 0.35f

    // --- BIẾN TÍNH TOÁN DYNAMIC ---
    private int currentLaneCount = 4; // Số làn đường tính toán được
    private float verticalMargin;
    private float horizontalMargin;

    private Rigidbody2D currentTargetRB;

    // Lấy vận tốc của mục tiêu thay vì luôn lấy của Player
    private float targetForwardSpeed => currentTargetRB != null ? currentTargetRB.linearVelocityX : 0f;


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
        AcquireRandomTarget();
        RecalculateScreenParams();
    }

    // Hàm Target mục tiêu
    private void AcquireRandomTarget()
    {
        BaseRunner[] allRacers = ReferenceManager.Instance.Racers;
        if (allRacers.Length > 0)
        {
            int randomIndex = Random.Range(0, allRacers.Length);
            currentTargetRB = allRacers[randomIndex].GetComponent<Rigidbody2D>();
        }
        else
        {
            // Fallback an toàn nếu không tìm thấy ai
            currentTargetRB = ReferenceManager.Instance.PlayerRigidbody;
        }
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
        // Ví dụ: Tablet có thể lên tới 12 làn, điện thoại dọc có thể là 4-5 làn
        currentLaneCount = Mathf.Clamp(calculatedLanes, 3, 8);
    }

    // Helper: Lấy vị trí X (Viewport 0-1) của làn thứ i
    private float GetLaneCenterViewport(int laneIndex)
    {
        // Chia đoạn từ [margin] đến [1-margin] thành các phần bằng nhau
        float t = (float)laneIndex / (currentLaneCount - 1);
        return Mathf.Lerp(screenEdgeMargin, 1f - screenEdgeMargin, t);
    }

    // Lấy tọa độ X (0.0 -> 1.0) của mục tiêu trên màn hình
    private float GetTargetViewportX()
    {
        if (currentTargetRB == null) return 0.5f;
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(currentTargetRB.position);
        return Mathf.Clamp(viewportPos.x, screenEdgeMargin, 1f - screenEdgeMargin);
    }

    // Quy đổi tọa độ X của mục tiêu ra Lane Index tương ứng
    private int GetTargetLaneIndex()
    {
        float targetX = GetTargetViewportX();
        float t = (targetX - screenEdgeMargin) / (1f - 2f * screenEdgeMargin);
        return Mathf.Clamp(Mathf.RoundToInt(t * (currentLaneCount - 1)), 0, currentLaneCount - 1);
    }

    public void ExecuteAttack(AttackPattern pattern)
    {
        AcquireRandomTarget();
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
        MoveProjecties projecties = GetProjecties();

        // Hàng 1: Spawn ở các làn CHẴN
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateProjecties(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), currentSpeed, 0f, projecties);
        }

        // Hàng 2: Spawn ở các làn LẺ (Delay để tạo kẽ hở chéo)
        float rowDelay = 0.9f;
        for (int i = 1; i < currentLaneCount; i += 2)
        {
            CreateProjecties(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), currentSpeed, rowDelay, projecties);
        }
    }

    // 2. Vertical Row (Tường rơi): Đảm bảo luôn có khe hở
    private void SpawnVerticalRow(bool isWave)
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float currentSpeed = CalculateOptimalSpeed(Vector2.up);
        MoveProjecties projecties = GetProjecties();
        float safeDelay = CalculateSafeDelay(currentSpeed);

        // Nếu là Wave, lấy vị trí mục tiêu làm tâm tỏa ra (thay vì tâm khe hở)
        int centerIndex = GetTargetLaneIndex();

        for (int i = 0; i < currentLaneCount; i++)
        {
            // ĐÃ XÓA dòng: if (safeLanes.Contains(i)) continue;

            float x = GetLaneCenterViewport(i);
            float delay = isWave ? Mathf.Abs(i - centerIndex) * safeDelay * 0.5f : 0f;

            CreateProjecties(new Vector2(x, startY), new Vector2(x, endY), currentSpeed, delay, projecties);
        }
    }
    // 3. V Shape: Tự động căn giữa bất kể số làn
    private void SpawnVShape()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateSpeed(Vector2.up * 2f);
        MoveProjecties projecties = GetProjecties();

        int targetIndex = GetTargetLaneIndex(); // Lấy lane của target làm tâm
        int layers = Mathf.Min(3, currentLaneCount); // Số lớp giới hạn

        for (int offset = 0; offset < layers; offset++)
        {
            float delay = offset * 0.3f;

            int leftIndex = targetIndex - offset;
            if (leftIndex >= 0)
                CreateProjecties(new Vector2(GetLaneCenterViewport(leftIndex), startY), new Vector2(GetLaneCenterViewport(leftIndex), endY), speed, delay, projecties);

            if (offset != 0)
            {
                int rightIndex = targetIndex + offset;
                if (rightIndex < currentLaneCount)
                    CreateProjecties(new Vector2(GetLaneCenterViewport(rightIndex), startY), new Vector2(GetLaneCenterViewport(rightIndex), endY), speed, delay, projecties);
            }
        }
    }
    // 4. Mưa ngẫu nhiên an toàn
    private void SpawnRandomRainSafe()
    {
        // Số lượng hạt mưa = Tổng số làn - 1 (Luôn chừa 1 làn trống)
        int rainCount = currentLaneCount;

        List<int> availableLanes = new List<int>();
        for (int i = 0; i < currentLaneCount; i++) availableLanes.Add(i);

        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up);
        MoveProjecties projecties = GetProjecties();

        for (int i = 0; i < rainCount; i++)
        {
            if (availableLanes.Count == 0) break;

            int rIndex = Random.Range(0, availableLanes.Count);
            int lane = availableLanes[rIndex];
            availableLanes.RemoveAt(rIndex);

            float delay = i * 0.15f;
            CreateProjecties(new Vector2(GetLaneCenterViewport(lane), startY), new Vector2(GetLaneCenterViewport(lane), endY), speed, delay, projecties);
        }
    }

    // 5. Fan Spread: Rải đều theo số làn
    private void SpawnFanSpread()
    {
        float startX = GetTargetViewportX(); // Tâm tỏa ra từ target
        Vector2 startPoint = new Vector2(startX, 1.2f);
        float speed = CalculateOptimalSpeed(Vector2.up);
        MoveProjecties projecties = GetProjecties();
        int targetIndex = GetTargetLaneIndex();

        for (int i = 0; i < currentLaneCount; i++)
        {
            float endX = GetLaneCenterViewport(i);
            CreateProjecties(startPoint, new Vector2(endX, -0.2f), speed, Mathf.Abs(i - targetIndex) * 0.1f, projecties);
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
        MoveProjecties projecties = GetProjecties();

        // Pattern ngẫu nhiên như cũ
        int patternType = Random.Range(0, 3);
        switch (patternType)
        {
            case 0: // Thấp + Cao
                CreateProjecties(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0, projecties);
                CreateProjecties(new Vector2(startX, fixedY[2]), new Vector2(endX, fixedY[2]), speed, 0, projecties);
                break;
            case 1: // Cầu thang
                CreateProjecties(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0, projecties);
                CreateProjecties(new Vector2(startX, fixedY[1]), new Vector2(endX, fixedY[1]), speed, 0.4f, projecties);
                CreateProjecties(new Vector2(startX, fixedY[2]), new Vector2(endX, fixedY[2]), speed, 0.8f, projecties);
                break;
            case 2: // So le
                CreateProjecties(new Vector2(startX, fixedY[1]), new Vector2(endX, fixedY[1]), speed, 0, projecties);
                CreateProjecties(new Vector2(startX, fixedY[0]), new Vector2(endX, fixedY[0]), speed, 0.5f, projecties);
                break;
        }
    }

    // 7. Tunnel Vision: Tạo 2 bức tường dày ở 2 bên lề, ép người chơi chạy vào giữa
    private void SpawnTunnelVision()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.1f;
        MoveProjecties projecties = GetProjecties();

        int targetIndex = GetTargetLaneIndex();
        int safeWidth = 1;

        for (int i = 0; i < currentLaneCount; i++)
        {
            if (Mathf.Abs(i - targetIndex) <= safeWidth / 2) continue;

            for (int j = 0; j < 2; j++)
            {
                CreateProjecties(
                    new Vector2(GetLaneCenterViewport(i), startY + (j * 0.25f)),
                    new Vector2(GetLaneCenterViewport(i), endY),
                    speed,
                    j * 0.1f, projecties
                );
            }
        }
    }
    // 8. Alternating Checkers: Thả 2 lớp so le chẵn lẻ với tốc độ cao (Khó hơn Grid Wall cũ)
    private void SpawnAlternatingCheckers()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.2f; 
        MoveProjecties projecties = GetProjecties();

        // Lớp 1: Các làn Chẵn (0, 2, 4...)
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateProjecties(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0f, projecties);
        }

        // Lớp 2: Các làn Lẻ (1, 3, 5...) - Delay ngắn để người chơi phải lạng lách nhanh
        for (int i = 1; i < currentLaneCount; i += 2)
        {
            CreateProjecties(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0.5f, projecties);
        }

        // Lớp 3 (Optional): Lặp lại Chẵn để ép góc thêm lần nữa
        for (int i = 0; i < currentLaneCount; i += 2)
        {
            CreateProjecties(new Vector2(GetLaneCenterViewport(i), startY), new Vector2(GetLaneCenterViewport(i), endY), speed, 0.7f, projecties);
        }
    }

    // 9. Converging Stream: Hai luồng đạn từ 2 góc trên lao chéo vào giữa đáy màn hình
    private void SpawnConvergingStream()
    {
        Vector2 targetBottom = new Vector2(0.5f, -0.2f); // Điểm hội tụ
        float speed = CalculateSpeed(Vector2.up) * 1.3f;
        MoveProjecties projecties = GetProjecties();
        int bulletCount = 4; // Số lượng đạn mỗi bên

        for (int i = 0; i < bulletCount; i++)
        {
            float delay = i * 0.2f;

            // Luồng bên Trái: Từ (0, 1.2) -> Giữa đáy
            CreateProjecties(new Vector2(0f, 1.2f), targetBottom, speed, delay, projecties);

            // Luồng bên Phải: Từ (1, 1.2) -> Giữa đáy
            CreateProjecties(new Vector2(1f, 1.2f), targetBottom, speed, delay, projecties);
        }
    }

    // 10. Spiral Rain: Rơi từ trên xuống nhưng vị trí X uốn lượn theo hình Sin (như con rắn)
    private void SpawnSpiralRain()
    {
        float startY = 1f + verticalMargin;
        float endY = 0f - verticalMargin;
        float speed = CalculateOptimalSpeed(Vector2.up);
        MoveProjecties projecties = GetProjecties();

        int count = 6; // Số lượng vật thể

        for (int i = 0; i < count; i++)
        {
            // Tính toán vị trí X dựa trên hàm Sin
            // i càng lớn thì góc càng thay đổi -> tạo hình lượn sóng
            float t = (float)i / count; // 0 -> 1
            float sinX = 0.5f + (Mathf.Sin(t * Mathf.PI * 2) * 0.4f); // Dao động quanh trục 0.5 với biên độ 0.4

            // Kẹp vào trong màn hình cho an toàn
            sinX = Mathf.Clamp(sinX, screenEdgeMargin, 1f - screenEdgeMargin);

            CreateProjecties(new Vector2(sinX, startY), new Vector2(sinX, endY), speed, i * 0.15f, projecties);
        }
    }

    // 11. Corner Ambush: 4 vật thể bay từ 4 góc màn hình cắt chéo qua tâm
    private void SpawnCornerAmbush()
    {
        float speed = CalculateSpeed(Vector2.up) * 1.5f; 
        MoveProjecties projecties = GetProjecties();
        float delayStep = 0.25f;

        // Góc trên trái -> Góc dưới phải
        CreateProjecties(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), speed, 0f, projecties);

        // Góc trên phải -> Góc dưới trái
        CreateProjecties(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), speed, delayStep, projecties);

        // (Khó hơn) Góc dưới trái -> Góc trên phải (Bay ngược lên)
        CreateProjecties(new Vector2(0f, -0.2f), new Vector2(1f, 1.2f), speed, delayStep * 2, projecties);

        // (Khó hơn) Góc dưới phải -> Góc trên trái
        CreateProjecties(new Vector2(1f, -0.2f), new Vector2(0f, 1.2f), speed, delayStep * 3, projecties);
    }

    // 12.
    private void SpawnCrossPattern()
    {
        float speed = CalculateOptimalSpeed(Vector2.up) * 1.3f;
        MoveProjecties projecties = GetProjecties();

        CreateProjecties(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), speed, 0f, projecties);
        CreateProjecties(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), speed, 0.5f, projecties);
    }

    // 13.
    private void SpawnSniperShot()
    {
        if (currentTargetRB == null) return;

        Vector3 targetViewport = Camera.main.WorldToViewportPoint(currentTargetRB.position);
        float targetX = Mathf.Clamp(targetViewport.x + 0.1f, 0.1f, 0.9f);
        float speed = CalculateSpeed(Vector2.up) * 1.1f;
        MoveProjecties projecties = GetProjecties();

        for (int i = 0; i < 3; i++)
            CreateProjecties(new Vector2(targetX, 1.2f), new Vector2(targetX, -0.2f), speed, i * 0.4f, projecties);
    }

    // 14.
    private void SpawnBigCenter()
    {
        float targetX = GetTargetViewportX(); // Lấy trục X của target
        CreateProjecties(new Vector2(targetX, 1.2f), new Vector2(targetX, -0.2f), CalculateSpeed(Vector2.up) * 0.8f, 0f, GetProjecties());
    }

    // 15.
    private void SpawnDoubleCrossFast()
    {
        MoveProjecties projecties = GetProjecties();

        CreateProjecties(new Vector2(0f, 1.2f), new Vector2(1f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f, projecties);
        CreateProjecties(new Vector2(1f, 1.2f), new Vector2(0f, -0.2f), CalculateSpeed(Vector2.up) * 1.8f, 0f, projecties);
    }

    // 16.
    private void SpawnHorizontalStream()
    {
        MoveProjecties projecties = GetProjecties();

        for (int i = 0; i < 3; i++)
            CreateProjecties(new Vector2(1.2f, 0.2f + i * 0.25f), new Vector2(-0.2f, 0.2f + i * 0.25f), CalculateSpeed(Vector2.right) * 1.2f, i * 0.2f, projecties);
    }

    // --- HELPERS ---
    private float CalculateSpeed(Vector2 distVec) => CalculateOptimalSpeed(distVec);

    private float CalculateOptimalSpeed(Vector2 viewDistanceVector)
    {
        float dynamicSpeed = baseObstacleSpeed + (targetForwardSpeed * targetVelocityInfluence);
        return Mathf.Clamp(dynamicSpeed, baseObstacleSpeed, maxObstacleSpeed);
    }

    private float CalculateSafeDelay(float obstacleSpeed)
    {
        return Mathf.Max(minSafeTimeGap, obstacleHitSize / obstacleSpeed);
    }

    // Trong ProjectiesControl.cs
    private void CreateProjecties(Vector2 startView, Vector2 endView, float speed, float delay, MoveProjecties prefab)
    {
        MoveProjecties obj = Instantiate(prefab);
        obj.transform.SetParent(transform);

        Camera cam = Camera.main;
        float zDepth = 10f;

        // Controller chịu trách nhiệm chuyển đổi tọa độ
        Vector3 startWorld = cam.ViewportToWorldPoint(new Vector3(startView.x, startView.y, zDepth));
        Vector3 endWorld = cam.ViewportToWorldPoint(new Vector3(endView.x, endView.y, zDepth));

        startWorld.z = 0;
        endWorld.z = 0;

        // Truyền tọa độ World vào script MoveProjecties mới
        obj.Initialize(startWorld, endWorld, speed, baseRotateSpeed, delay);
    }
        
    private MoveProjecties GetProjecties()
    {
        var projectilesList = BossManager.currentBossData.projectiesObstacle;
        int randomIndex = Random.Range(0, projectilesList.Count);
        return projectilesList[randomIndex];
    }
}