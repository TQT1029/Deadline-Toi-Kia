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
    [SerializeField] private GameObject winPoint; //Prefab của winpoint

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
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnNextPiece();
        }

        CleanupOldObjects();

        distanceRan = GameStatsController.Instance.resultDistance;

        HandleBossSpawn();
        HandleBossFight();


    }

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

            // LƯU mốc khoảng cách khi boss chết
            bossDefeatedDistance = distanceRan;

            SpawnWinPoint();
        }
    }
    private void SpawnWinPoint()
    {
        if (winPoint == null || winPointSpawned) return;

        float winX = bossDefeatedDistance + winPointOffset;

        Instantiate(
            winPoint,
            new Vector2(winX, 0f),
            Quaternion.identity
        );

        winPointSpawned = true;
    }

    private void SpawnNextPiece()
    {
        // Sinh đất
        float newEdgeX = mapGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = newEdgeX;

    }

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