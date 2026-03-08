using UnityEngine;

[RequireComponent(typeof(GroundGenerator), typeof(PitObjectGenerator))]
[RequireComponent(typeof(ObstacleGenerator), typeof(ItemGenerator))]
public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;
    private void Awake() => Instance = this;

    [Header("Managers")]

    public MapGlobalConfig mapConfig;
    public GroundGenerator groundGenerator;
    public PitObjectGenerator pitObjectGenerator;
    public ObstacleGenerator obstacleGenerator;
    public ItemGenerator itemGenerator;

    [Header("Boss Settings")]
    [SerializeField] private float distanceToBoss = 250;
    [SerializeField] private float winPointOffset = 250;
    [SerializeField] private float timeToDefeat = 90f;

    private float distanceRan;
    private float bossDefeatedDistance;
    private float startBossTime;

    private bool bossSpawned;
    private bool bossDefeated;
    private bool winPointSpawned;
    [SerializeField] private GameObject winPoint;

    [Header("Settings")]
    public Transform player;
    public float generationDistance = 80f;

    [Header("Cleanup Settings")]
    public float destroyDistanceBehind = 50f;

    private float lastEdgeX = 0f;

    private float cleanUpTime = 0f;

    private void Start()
    {
        if (player == null) player = ReferenceManager.Instance.PlayerTransform;
        winPoint.SetActive(false);

        // [OPTIMIZED] Logic an toàn để tắt hố ở đoạn đầu
        // Sử dụng cờ tạm thời hoặc biến cục bộ để không ghi đè dữ liệu gốc nếu có lỗi
        int originalPitChance = MapGlobalConfig.Instance.pitChance;

        try
        {
            MapGlobalConfig.Instance.pitChance = 0; // Tắt hố
            for (int i = 0; i < 3; i++)
            {
                SpawnNextPiece();
            }
        }
        finally
        {
            // Luôn luôn trả lại giá trị gốc dù có lỗi xảy ra ở SpawnNextPiece
            MapGlobalConfig.Instance.pitChance = originalPitChance;
        }
    }
    private void Update()
    {
        if (winPointSpawned) return;

        // Chỉ spawn khi cần thiết
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnNextPiece();
        }

        if (Time.time > cleanUpTime)
        {
            CleanupOldObjects();
            cleanUpTime = Time.time + 0.25f;
        }

        distanceRan = GameStatsController.Instance.resultDistance;
        HandleBossSpawn();
        HandleBossFight();
    }

    //============================ Map Generation Core ============================//

    private void SpawnNextPiece()
    {
        // BƯỚC 1: Tạo Đất/Hố
        var result = groundGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = result.endX;

        // BƯỚC 2: Tạo vật thể trên segment đó
        if (result.type == GroundGenerator.SegmentType.Pit)
        {
            pitObjectGenerator.GenerateObjectsInPit(result.startX, result.endX);
        }
        else
        {
            obstacleGenerator.GenerateObstaclesOnGround(result.startX, result.segmentLenght);
        }

        // BƯỚC 3: Đồng bộ vật lý và tạo Item
        // Bắt buộc phải sync để Collider cập nhật vị trí mới nhất cho Raycast của ItemGenerator
        Physics2D.SyncTransforms();
        itemGenerator.GenerateItems(result.startX, result.endX);
    }
    //============================ Boss Logic ============================//

    private void HandleBossSpawn()
    {
        if (bossSpawned || bossDefeated) return;
        if (distanceRan >= distanceToBoss)
        {
            BossManager.Instance.StartBossFight();
            bossSpawned = true;
            startBossTime = Time.time;
        }
    }

    private void HandleBossFight()
    {
        if (!bossSpawned || bossDefeated) return;

        if (Time.time - startBossTime >= timeToDefeat)
        {
            BossManager.Instance.StopFight();
            bossDefeated = true;
            bossSpawned = false;
            bossDefeatedDistance = distanceRan;
            SummonWinPoint();
        }
    }

    private void SummonWinPoint()
    {
        if (winPoint == null || winPointSpawned) return;

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


    //============================ Helper ============================//
    private bool CheckWinPointValid(float startX)
    {
        float middleX = startX + 15;
        float endX = startX + 30;
        float distanceRays = 30f;

        RaycastHit2D hit_Start = Physics2D.Raycast(new Vector2(startX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
        RaycastHit2D hit_Middle = Physics2D.Raycast(new Vector2(middleX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
        RaycastHit2D hit_End = Physics2D.Raycast(new Vector2(endX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));

        return hit_Start && hit_Middle && hit_End;
    }

    private void CleanupOldObjects()
    {
        // Gom mảng vào local để loop cho gọn
        Transform[] containers = {
            groundGenerator.basePlatformObjs,
            pitObjectGenerator.obstacleObjs,
            pitObjectGenerator.miniPlatformObjs,
            obstacleGenerator.obstacleObjs,
            obstacleGenerator.miniPlatformObjs,
            itemGenerator.itemContainer
        };

        float killX = player.position.x - destroyDistanceBehind;

        foreach (Transform container in containers)
        {
            if (container == null) continue;
            // Loop ngược là đúng chuẩn khi Destroy
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.position.x < killX)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}