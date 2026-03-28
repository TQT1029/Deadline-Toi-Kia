using UnityEngine;

[RequireComponent(typeof(BaseGenerator), typeof(PitObjectGenerator))]
[RequireComponent(typeof(ObstacleGenerator), typeof(ItemGenerator))]
public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;
    private void Awake() => Instance = this;

    [Header("Managers")]

    public MapGlobalConfig mapConfig;
    public BaseGenerator baseGenerator;
    public PitObjectGenerator pitObjectGenerator;
    public ObstacleGenerator obstacleGenerator;
    public ItemGenerator itemGenerator;

    [Header("Boss Settings")]
    [SerializeField] private float distanceToBoss = 250;
    [SerializeField] private float winPointOffset = 250;
    [SerializeField] private float timeToDefeat = 90f;
    [SerializeField] private float distanceToGenerateObstacle = 50f; // Khoảng cách trước khi spawn vật thể

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
            while (lastEdgeX < distanceToGenerateObstacle)
            {
                SpawnNextPiece();
            }

            ClearSafeZone(player.position.x, player.position.x + distanceToGenerateObstacle);

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

    private void SpawnNextPiece(float safeDistance = 0f)
    {
        var result = baseGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = result.endX;

        // Bỏ qua bước sinh vật cản và item nếu đoạn map vừa tạo nằm trong vùng safe zone
        if (result.startX < safeDistance) return;

        if (result.type == BaseGenerator.SegmentType.Pit)
        {
            pitObjectGenerator.GenerateObjectsInPit(result.startX, result.endX);
        }
        else
        {
            obstacleGenerator.GenerateObstaclesOnGround(result.startX, result.endX);
        }

        Physics2D.SyncTransforms();
        itemGenerator.GenerateItems(result.startX, result.endX);
    }

    private void ClearSafeZone(float startX, float endX)
    {
        // Ép Unity cập nhật Collider cho các vật thể vừa Instantiate xong
        Physics2D.SyncTransforms();

        float width = endX - startX;
        float height = 40f; // Chiều cao box đủ để bao phủ từ đáy hố lên tận sàn bay cao nhất

        Vector2 center = new Vector2(startX + width / 2f, MapGlobalConfig.Instance.groundY + 10f);
        Vector2 size = new Vector2(width, height);

        // Quét toàn bộ collider nằm trong vùng an toàn
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D hit in hits)
        {
            // Kiểm tra xem collider này có thuộc về Obstacle, Item, hay MiniPlatform không
            // Hàm GetRootInContainer giúp lấy ra chính xác Prefab Root (tránh xóa nhầm part con)
            Transform targetToDestroy = GetRootInContainer(hit.transform);

            if (targetToDestroy != null)
            {
                Destroy(targetToDestroy.gameObject);
            }
        }
    }

    private Transform GetRootInContainer(Transform child)
    {
        Transform current = child;

        // Truy ngược lên cây gia phả để tìm xem nó có nằm trong các Container rác không
        while (current.parent != null)
        {
            if (current.parent == pitObjectGenerator.obstacleObjs ||
                current.parent == pitObjectGenerator.miniPlatformObjs ||
                current.parent == obstacleGenerator.obstacleObjs ||
                current.parent == obstacleGenerator.miniPlatformObjs ||
                current.parent == itemGenerator.itemContainer)
            {
                // Nếu cha của nó là Container, thì chính nó là Prefab gốc cần xóa
                return current;
            }
            current = current.parent;
        }

        // Nếu không thuộc các container trên (ví dụ như Ground, Player, Background), trả về null để tha mạng
        return null;
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

        mapConfig.hasPit = false;
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
            baseGenerator.basePlatformObjs,
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