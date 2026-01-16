using UnityEngine;

public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;
    private void Awake() => Instance = this;

    [Header("Boss Settings")]
    [SerializeField] private float distanceToBoss = 100f;
    [SerializeField] private float winPointOffset = 40f; // KHOẢNG CÁCH SAU KHI BOSS CHẾT

    [SerializeField] private float timeToDefeat = 60f;

    private float distanceRan;
    private float bossDefeatedDistance;
    private float startBossTime;

    private bool bossSpawned;
    private bool bossDefeated;
    private bool winPointSpawned;
    [SerializeField] private GameObject winPoint; //Obj của winpoint

    [Header("Managers")]
    public MapGenerator mapGenerator;
    public ItemGenerator itemGenerator;

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

        int oldPit = mapGenerator.pitChance;
        mapGenerator.pitChance = 0;

        for (int i = 0; i < 3; i++)
        {
            SpawnNextPiece();
        }

        mapGenerator.pitChance = oldPit;
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

    //============================ Boss Handling ============================//

    // Xử lý sinh boss
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

    // Xử lý trận đấu với boss
    private void HandleBossFight()
    {
        if (!bossSpawned || bossDefeated) return;

        if (Time.time - startBossTime >= timeToDefeat)
        {
            BossManager.Instance.StopFight();

            bossDefeated = true;
            bossSpawned = false;

            // LƯU mốc khoảng cách khi boss chết
            bossDefeatedDistance = distanceRan;

            SummonWinPoint();
        }
    }

    // Triệu hồi Win Point
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

        //Safe check
        int safetyCounter = 0;

        while (hit_Start == default || hit_Middle == default || hit_End == default)
        {
            if (safetyCounter > 50) break;

            winXStart += 10f;
            middleX = winXStart + 15;
            endX = winXStart + 30;

            hit_Start = Physics2D.Raycast(new Vector2(winXStart, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
            hit_Middle = Physics2D.Raycast(new Vector2(middleX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));
            hit_End = Physics2D.Raycast(new Vector2(endX, 10f), Vector2.down, distanceRays, LayerMask.GetMask("Platform"));

            Debug.DrawRay(new Vector2(winXStart, 10f), Vector2.down * distanceRays, Color.red, 5f);
            Debug.DrawRay(new Vector2(middleX, 10f), Vector2.down * distanceRays, Color.green, 5f);
            Debug.DrawRay(new Vector2(endX, 10f), Vector2.down * distanceRays, Color.blue, 5f);

            safetyCounter++;
        }

        

        winPoint.transform.position = new Vector2(winXStart, 0f);
        winPoint.SetActive(true);

        winPointSpawned = true;

    }

    //============================ Map Generation ============================//

    // Sinh đoạn đất mới
    private void SpawnNextPiece()
    {
        // Sinh đất
        float newEdgeX = mapGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = newEdgeX;

    }

    //============================ Helper ============================//

    // Dọn dẹp các object cũ
    private void CleanupOldObjects()
    {
        Transform[] containers = {
            mapGenerator.basePlatformObjs,
            mapGenerator.obstacleObjs,
            mapGenerator.miniPlatformObjs,
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