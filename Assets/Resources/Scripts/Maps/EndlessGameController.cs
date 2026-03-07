using UnityEngine;

[RequireComponent (typeof(GroundGenerator), typeof(PitObjectGenerator))]
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

    private void Start()
    {
        if (player == null) player = ReferenceManager.Instance.PlayerTransform;
        winPoint.SetActive(false);

        // [SYNC] Thay đổi PitChance thông qua Config
        // Lưu lại giá trị gốc
        int oldPit = MapGlobalConfig.Instance.pitChance;

        // Set về 0 để 3 đoạn đầu không có hố
        MapGlobalConfig.Instance.pitChance = 0;

        for (int i = 0; i < 3; i++)
        {
            SpawnNextPiece();
        }

        // Trả lại giá trị gốc
        MapGlobalConfig.Instance.pitChance = oldPit;
    }

    private void Update()
    {
        if (winPointSpawned) return;

        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnNextPiece();
        }

        CleanupOldObjects();

        distanceRan = GameStatsController.Instance.resultDistance;

        HandleBossSpawn();
        HandleBossFight();
    }

    //============================ Map Generation Core ============================//

    private void SpawnNextPiece()
    {
        // BƯỚC 1: Tạo Đất hoặc Hố (GroundGenerator)
        // Hàm này trả về info: loại gì (Đất/Hố), bắt đầu từ đâu, kết thúc ở đâu
        var resultSegmment = groundGenerator.SpawnNextSegment(lastEdgeX);

        // Cập nhật mốc biên map mới
        lastEdgeX = resultSegmment.endX;

        // BƯỚC 2 & 3: Dựa vào loại map vừa tạo để gọi Generator phù hợp
        if (resultSegmment.type == GroundGenerator.SegmentType.Pit)
        {
            // Nếu là Hố -> Gọi PitObjectGenerator để tạo cầu hoặc vật cản bay giữa hố
            pitObjectGenerator.GenerateObjectsInPit(resultSegmment.startX, resultSegmment.endX);
        }
        else // resultSegmment.type == Ground
        {
            // Nếu là Đất liền -> Gọi ObstacleGenerator để tạo chướng ngại vật hoặc sàn bay
            obstacleGenerator.GenerateObstaclesOnGround(resultSegmment.startX, resultSegmment.endX);
        }

        // BƯỚC 4: Tạo Items (ItemGenerator)
        // Item luôn được tạo sau cùng để đảm bảo nó nằm trên các object vừa sinh ra
        Physics2D.SyncTransforms(); // Cập nhật vật lý để ItemGenerator Raycast chính xác
        itemGenerator.GenerateItems(resultSegmment.startX, resultSegmment.endX);

        Debug.Log("Last Edge: "+lastEdgeX);
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

        float middleX = winXStart + 15;
        float endX = winXStart + 30;
        float distanceRays = 30f;

        RaycastHit2D hit_Start = Physics2D.Raycast(new Vector2(winXStart, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
        RaycastHit2D hit_Middle = Physics2D.Raycast(new Vector2(middleX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
        RaycastHit2D hit_End = Physics2D.Raycast(new Vector2(endX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));

        int safetyCounter = 0;
        while (hit_Start == default || hit_Middle == default || hit_End == default)
        {
            if (safetyCounter > 50) break;
            winXStart -= 10f; // Lùi lại tìm đất
            middleX = winXStart + 15;
            endX = winXStart + 30;

            hit_Start = Physics2D.Raycast(new Vector2(winXStart, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
            hit_Middle = Physics2D.Raycast(new Vector2(middleX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
            hit_End = Physics2D.Raycast(new Vector2(endX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
            safetyCounter++;
        }

        winPoint.transform.position = new Vector2(winXStart, 0f);
        winPoint.SetActive(true);
        winPointSpawned = true;
    }

    //============================ Helper ============================//

    private void CleanupOldObjects()
    {
        // Cần cleanup từ tất cả các nguồn
        Transform[] containers = {
            groundGenerator.basePlatformObjs,
            pitObjectGenerator.obstacleObjs,
            pitObjectGenerator.miniPlatformObjs,
            obstacleGenerator.obstacleObjs,    
            obstacleGenerator.miniPlatformObjs,
            itemGenerator.itemContainer
        };

        foreach (Transform container in containers)
        {
            if (container == null) continue;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (player.position.x - child.position.x > destroyDistanceBehind)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}