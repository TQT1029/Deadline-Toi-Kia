using UnityEngine;

[RequireComponent(typeof(BaseGenerator), typeof(PitObjectGenerator))]
[RequireComponent(typeof(ObstacleGenerator), typeof(ItemGenerator))]
public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance { get; private set; }
    private void Awake() => Instance = this;

    public enum PacingPhase
    {
        SafeZone,        // 0m - 50m: Duong chay phang lang, khong ho, khong vat can, rai coin khoi dau
        RhythmFlow,      // 50m - (distanceToBoss - 30m): Dan xen nhip nhang giua chuong ngai vat va san bay
        PreBossWarmup,   // (distanceToBoss - 30m) - distanceToBoss: Giam dan vat can bao hieu Boss xuat hien
        BossFight,       // Khi Boss xuat hien: Mat dat lien mach (khong ho sau), tap trung ne dan Boss
        PostBossVictory  // Sau khi ha Boss: Duong chay quang dang dan thang toi WinPoint
    }

    [Header("Pacing Phase Monitor")]
    [SerializeField] private PacingPhase currentPhase = PacingPhase.SafeZone;
    public PacingPhase CurrentPhase => currentPhase;

    [Header("Master Data Profile")]
    [SerializeField] private MapProfile mapProfile;

    [Header("Managers")]
    public MapGlobalConfig mapConfig;
    public BaseGenerator baseGenerator;
    public PitObjectGenerator pitObjectGenerator;
    public ObstacleGenerator obstacleGenerator;
    public ItemGenerator itemGenerator;

    [Header("Pacing & Progression Settings")]
    [SerializeField] private float safeZoneDistance = 50f;
    [SerializeField] private float distanceToBoss = 250f;
    [SerializeField] private float winPointOffset = 250f;
    [SerializeField] private float timeToDefeat = 90f;

    private float distanceRan;
    private float bossDefeatedDistance;
    private float startBossTime;

    private bool bossSpawned;
    private bool bossDefeated;
    private bool winPointSpawned;
    [SerializeField] private GameObject winPoint;

    [Header("Generation & Cleanup Settings")]
    public Transform player;
    public float generationDistance = 80f;
    public float destroyDistanceBehind = 50f;

    private float lastEdgeX = 0f;
    private float cleanUpTime = 0f;

    private readonly Collider2D[] _overlapResults = new Collider2D[64];
    private Transform[] _containers;

    private void Start()
    {
        if (player == null && ReferenceManager.Instance != null)
        {
            player = ReferenceManager.Instance.PlayerTransform;
        }

        if (winPoint != null) winPoint.SetActive(false);

        // 1. Khoi tao Data-Driven tu MapProfile neu co (co fallback an toan 100%)
        InitializeDataProfile();

        // 2. Prewarm cac Object Pools
        PrewarmPools();

        // 3. Khoi tao Containers de cleanup
        InitializeContainers();

        // 4. Sinh truoc doan dau an toan (Safe Zone)
        SetPacingPhase(PacingPhase.SafeZone);
        while (lastEdgeX < safeZoneDistance)
        {
            SpawnNextPiece();
        }

        ClearSafeZone(player != null ? player.position.x : 0f, safeZoneDistance);
    }

    private void InitializeDataProfile()
    {
        // Uu tien lay MapProfile tu ReferenceManager (khi load scene tu Menu chon Map)
        if (ReferenceManager.Instance != null && ReferenceManager.Instance.CurrentSelectedMap != null)
        {
            mapProfile = ReferenceManager.Instance.CurrentSelectedMap;
        }

        if (mapProfile != null)
        {
            // Chi ghi de neu gia tri trong ScriptableObject hop le (> 0 hoac khac 0)
            if (mapProfile.safeZoneDistance > 0) safeZoneDistance = mapProfile.safeZoneDistance;
            if (mapProfile.distanceToBoss > 0) distanceToBoss = mapProfile.distanceToBoss;
            if (mapProfile.timeToDefeat > 0) timeToDefeat = mapProfile.timeToDefeat;
            if (mapProfile.winPointOffset > 0) winPointOffset = mapProfile.winPointOffset;
            if (mapProfile.generationDistance > 0) generationDistance = mapProfile.generationDistance;
            if (mapProfile.destroyDistanceBehind > 0) destroyDistanceBehind = mapProfile.destroyDistanceBehind;

            if (mapConfig != null)
            {
                if (mapProfile.groundY != 0) mapConfig.groundY = mapProfile.groundY;
                if (mapProfile.pitY != 0) mapConfig.pitY = mapProfile.pitY;
                if (mapProfile.maxHeightMap > 0) mapConfig.maxHeightMap = mapProfile.maxHeightMap;
                if (mapProfile.waveFrequency > 0) mapConfig.waveFrequency = mapProfile.waveFrequency;
                if (mapProfile.pitChance > 0) mapConfig.pitChance = mapProfile.pitChance;
                mapConfig.hasPit = mapProfile.hasPit;
            }

            if (baseGenerator != null) baseGenerator.ApplyConfig(mapProfile);
            if (obstacleGenerator != null) obstacleGenerator.ApplyConfig(mapProfile);
            if (pitObjectGenerator != null) pitObjectGenerator.ApplyConfig(mapProfile);
            if (itemGenerator != null) itemGenerator.ApplyConfig(mapProfile);

            Debug.Log($"[EndlessGameController] Initialized with MapProfile: {mapProfile.mapName}, hasPit={mapProfile.hasPit}, pitChance={mapProfile.pitChance}");
        }
        else if (mapConfig != null)
        {
            // Fallback: đảm bảo hasPit được bật nếu mapConfig có hasPit = true
            Debug.Log($"[EndlessGameController] Fallback to MapGlobalConfig: hasPit={mapConfig.hasPit}, pitChance={mapConfig.pitChance}");
        }

        // Dam bao destroyDistanceBehind luon du an toan (toi thieu 40m phia sau camera)
        destroyDistanceBehind = Mathf.Max(40f, destroyDistanceBehind);
        generationDistance = Mathf.Max(60f, generationDistance);
    }

    private void PrewarmPools()
    {
        if (baseGenerator != null) baseGenerator.Prewarm(3);
        if (obstacleGenerator != null) obstacleGenerator.Prewarm(3);
        if (pitObjectGenerator != null) pitObjectGenerator.Prewarm(3);
        if (itemGenerator != null) itemGenerator.Prewarm(20);
    }

    private void InitializeContainers()
    {
        _containers = new Transform[] {
            baseGenerator != null ? baseGenerator.basePlatformObjs : null,
            pitObjectGenerator != null ? pitObjectGenerator.obstacleObjs : null,
            pitObjectGenerator != null ? pitObjectGenerator.miniPlatformObjs : null,
            obstacleGenerator != null ? obstacleGenerator.obstacleObjs : null,
            obstacleGenerator != null ? obstacleGenerator.miniPlatformObjs : null,
            itemGenerator != null ? itemGenerator.itemContainer : null
        };
    }

    private void Update()
    {
        if (winPointSpawned) return;

        if (player == null && ReferenceManager.Instance != null)
        {
            player = ReferenceManager.Instance.PlayerTransform;
        }
        if (player == null) return;

        // 1. Cap nhat quang duong chay
        if (GameStatsController.Instance != null)
        {
            distanceRan = GameStatsController.Instance.resultDistance;
        }

        // 2. Sinh map phia truoc theo generationDistance (Cap nhat Pacing theo toa do sinh thuc te lastEdgeX)
        if (player.position.x + generationDistance > lastEdgeX)
        {
            EvaluatePacingPhaseForGeneration(lastEdgeX);
            SpawnNextPiece();
        }

        // 3. Don dep vat the phia sau dinh ky (0.25s / lan)
        if (Time.time > cleanUpTime)
        {
            CleanupOldObjects();
            cleanUpTime = Time.time + 0.25f;
        }

        // 4. Xu ly Boss Fight
        HandleBossSpawn();
        HandleBossFight();
    }

    // ============================ Pacing State Machine ============================ //

    private void EvaluatePacingPhaseForGeneration(float generationX)
    {
        if (bossDefeated)
        {
            if (currentPhase != PacingPhase.PostBossVictory)
                SetPacingPhase(PacingPhase.PostBossVictory);
        }
        else if (bossSpawned)
        {
            if (currentPhase != PacingPhase.BossFight)
                SetPacingPhase(PacingPhase.BossFight);
        }
        else if (generationX >= distanceToBoss - 50f)
        {
            if (currentPhase != PacingPhase.PreBossWarmup)
                SetPacingPhase(PacingPhase.PreBossWarmup);
        }
        else if (generationX >= safeZoneDistance)
        {
            if (currentPhase != PacingPhase.RhythmFlow)
                SetPacingPhase(PacingPhase.RhythmFlow);
        }
        else
        {
            if (currentPhase != PacingPhase.SafeZone)
                SetPacingPhase(PacingPhase.SafeZone);
        }
    }

    private void SetPacingPhase(PacingPhase newPhase)
    {
        currentPhase = newPhase;

        bool mapHasPitConfig = (mapProfile != null) ? mapProfile.hasPit : (mapConfig != null && mapConfig.hasPit);

        switch (newPhase)
        {
            case PacingPhase.SafeZone:
                if (mapConfig != null) mapConfig.hasPit = false;
                if (baseGenerator != null) baseGenerator.PitChanceMultiplier = 0f;
                if (obstacleGenerator != null)
                {
                    obstacleGenerator.IsGenerationEnabled = false;
                    obstacleGenerator.DensityMultiplier = 0f;
                }
                if (pitObjectGenerator != null)
                {
                    pitObjectGenerator.IsGenerationEnabled = false;
                    pitObjectGenerator.DensityMultiplier = 0f;
                }
                if (itemGenerator != null)
                {
                    itemGenerator.IsGenerationEnabled = true;
                    itemGenerator.DensityMultiplier = 1.0f;
                }
                break;

            case PacingPhase.RhythmFlow:
                if (mapConfig != null) mapConfig.hasPit = mapHasPitConfig;
                if (baseGenerator != null) baseGenerator.PitChanceMultiplier = 1.0f;
                if (obstacleGenerator != null)
                {
                    obstacleGenerator.IsGenerationEnabled = true;
                    obstacleGenerator.DensityMultiplier = 1.0f;
                }
                if (pitObjectGenerator != null)
                {
                    pitObjectGenerator.IsGenerationEnabled = true;
                    pitObjectGenerator.DensityMultiplier = 1.0f;
                }
                if (itemGenerator != null)
                {
                    itemGenerator.IsGenerationEnabled = true;
                    itemGenerator.DensityMultiplier = 1.0f;
                }
                break;

            case PacingPhase.PreBossWarmup:
                if (mapConfig != null) mapConfig.hasPit = mapHasPitConfig;
                if (baseGenerator != null) baseGenerator.PitChanceMultiplier = 0.5f;
                if (obstacleGenerator != null)
                {
                    obstacleGenerator.IsGenerationEnabled = true;
                    obstacleGenerator.DensityMultiplier = 0.5f;
                }
                if (pitObjectGenerator != null)
                {
                    pitObjectGenerator.IsGenerationEnabled = true;
                    pitObjectGenerator.DensityMultiplier = 0.5f;
                }
                if (itemGenerator != null)
                {
                    itemGenerator.IsGenerationEnabled = true;
                    itemGenerator.DensityMultiplier = 0.8f;
                }
                break;

            case PacingPhase.BossFight:
                // [USER REQUIREMENT]: Vẫn BẬT hố, bẫy và sàn bay nhưng giảm mật độ hợp lý (60%)
                if (mapConfig != null) mapConfig.hasPit = mapHasPitConfig;
                if (baseGenerator != null) baseGenerator.PitChanceMultiplier = 0.6f;
                if (obstacleGenerator != null)
                {
                    obstacleGenerator.IsGenerationEnabled = true;
                    obstacleGenerator.DensityMultiplier = 0.6f;
                }
                if (pitObjectGenerator != null)
                {
                    pitObjectGenerator.IsGenerationEnabled = true;
                    pitObjectGenerator.DensityMultiplier = 0.6f;
                }
                if (itemGenerator != null)
                {
                    itemGenerator.IsGenerationEnabled = true;
                    itemGenerator.DensityMultiplier = 0.4f;
                }
                break;

            case PacingPhase.PostBossVictory:
                if (mapConfig != null) mapConfig.hasPit = false;
                if (baseGenerator != null) baseGenerator.PitChanceMultiplier = 0f;
                if (obstacleGenerator != null)
                {
                    obstacleGenerator.IsGenerationEnabled = true;
                    obstacleGenerator.DensityMultiplier = 0.2f;
                }
                if (pitObjectGenerator != null)
                {
                    pitObjectGenerator.IsGenerationEnabled = false;
                    pitObjectGenerator.DensityMultiplier = 0f;
                }
                if (itemGenerator != null)
                {
                    itemGenerator.IsGenerationEnabled = true;
                    itemGenerator.DensityMultiplier = 1.0f;
                }
                break;
        }
    }

    // ============================ Map Generation Core ============================ //

    private void SpawnNextPiece(float safeDistance = 0f)
    {
        if (baseGenerator == null) return;

        var result = baseGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = result.endX;

        if (result.startX < safeDistance) return;

        if (result.type == BaseGenerator.SegmentType.Pit)
        {
            if (pitObjectGenerator != null)
                pitObjectGenerator.GenerateObjectsInPit(result.startX, result.endX);
        }
        else
        {
            if (obstacleGenerator != null)
                obstacleGenerator.GenerateObstaclesOnGround(result.startX, result.endX);
        }

        Physics2D.SyncTransforms();

        if (itemGenerator != null)
        {
            itemGenerator.GenerateItems(result.startX, result.endX);
        }
    }

    private void ClearSafeZone(float startX, float endX)
    {
        Physics2D.SyncTransforms();

        float width = endX - startX;
        float height = 40f;

        Vector2 center = new Vector2(startX + width / 2f, (mapConfig != null ? mapConfig.groundY : -5f) + 10f);
        Vector2 size = new Vector2(width, height);

        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, _overlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null) continue;

            Transform targetToRecycle = GetRootInContainer(hit.transform);
            if (targetToRecycle != null)
            {
                GameObjectPool.Return(targetToRecycle.gameObject);
            }
        }
    }

    private Transform GetRootInContainer(Transform child)
    {
        Transform current = child;

        while (current != null && current.parent != null)
        {
            if ((pitObjectGenerator != null && (current.parent == pitObjectGenerator.obstacleObjs || current.parent == pitObjectGenerator.miniPlatformObjs)) ||
                (obstacleGenerator != null && (current.parent == obstacleGenerator.obstacleObjs || current.parent == obstacleGenerator.miniPlatformObjs)) ||
                (itemGenerator != null && current.parent == itemGenerator.itemContainer))
            {
                return current;
            }
            current = current.parent;
        }

        return null;
    }

    // ============================ Boss Logic ============================ //

    private void HandleBossSpawn()
    {
        if (bossSpawned || bossDefeated) return;
        if (distanceRan >= distanceToBoss)
        {
            if (BossManager.Instance != null) BossManager.Instance.StartBossFight();
            bossSpawned = true;
            startBossTime = Time.time;
            SetPacingPhase(PacingPhase.BossFight);
        }
    }

    private void HandleBossFight()
    {
        if (!bossSpawned || bossDefeated) return;

        if (Time.time - startBossTime >= timeToDefeat)
        {
            if (BossManager.Instance != null) BossManager.Instance.StopFight();
            bossDefeated = true;
            bossSpawned = false;
            bossDefeatedDistance = distanceRan;
            SetPacingPhase(PacingPhase.PostBossVictory);
            SummonWinPoint();
        }
    }

    private void SummonWinPoint()
    {
        if (winPoint == null || winPointSpawned) return;

        if (mapConfig != null) mapConfig.hasPit = false;
        Physics2D.SyncTransforms();
        SpawnNextPiece();

        float winXStart = bossDefeatedDistance + winPointOffset;

        for (int i = 0; i < 50; i++)
        {
            if (CheckWinPointValid(winXStart)) break;
            winXStart -= 10f;
        }

        winPoint.transform.position = new Vector2(winXStart, 0f);
        winPoint.SetActive(true);
        winPointSpawned = true;
    }

    private static int _platformLayerMask = -1;
    private static int PlatformLayerMask
    {
        get
        {
            if (_platformLayerMask == -1) _platformLayerMask = LayerMask.GetMask("Platform");
            return _platformLayerMask;
        }
    }

    private bool CheckWinPointValid(float startX)
    {
        float middleX = startX + 15f;
        float endX = startX + 30f;
        float distanceRays = 30f;

        RaycastHit2D hit_Start = Physics2D.Raycast(new Vector2(startX, 10f), Vector2.down, distanceRays, PlatformLayerMask);
        RaycastHit2D hit_Middle = Physics2D.Raycast(new Vector2(middleX, 10f), Vector2.down, distanceRays, PlatformLayerMask);
        RaycastHit2D hit_End = Physics2D.Raycast(new Vector2(endX, 10f), Vector2.down, distanceRays, PlatformLayerMask);

        return hit_Start && hit_Middle && hit_End;
    }

    private void CleanupOldObjects()
    {
        if (_containers == null) InitializeContainers();

        if (player == null)
        {
            if (ReferenceManager.Instance != null) player = ReferenceManager.Instance.PlayerTransform;
            if (player == null) return;
        }

        float killX = player.position.x - destroyDistanceBehind;

        for (int c = 0; c < _containers.Length; c++)
        {
            Transform container = _containers[c];
            if (container == null) continue;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                // Chi recycle nhung object da thuc su nam sau killX (va dang active)
                if (child != null && child.gameObject.activeSelf && child.position.x < killX)
                {
                    GameObjectPool.Return(child.gameObject);
                }
            }
        }
    }

    private void OnDestroy()
    {
        GameObjectPool.Clear();
    }
}