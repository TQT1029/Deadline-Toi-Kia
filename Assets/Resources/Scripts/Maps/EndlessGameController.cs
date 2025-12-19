using UnityEngine;
using System.Collections.Generic;

public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;
    private void Awake() => Instance = this;

    [Header("Config")]
    public List<BasePlatformData> basePlatforms;
    public Transform platformContainer;
    public float generationDistance = 80f;
    public float groundY = -2f;

    [Header("Spawners")]
    public ObstacleSpawner obstacleSpawner;
    public MiniPlatformSpawner miniPlatformSpawner;
    public ItemSpawner itemSpawner;

    private float lastEdgeX = 0f;
    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // Spawn đoạn đầu tiên an toàn
        SpawnPlatformSegment(20f);
    }

    private void Update()
    {
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnPlatformSegment();
        }
    }

    private void SpawnPlatformSegment(float forcedLength = 0)
    {
        // 1. Sinh Base Platform (Đất)
        BasePlatformData data = basePlatforms[Random.Range(0, basePlatforms.Count)];
        float length = (forcedLength > 0) ? forcedLength : data.Length;

        float startX = lastEdgeX;
        float endX = startX + length;
        float centerX = startX + (length / 2f);

        GameObject plat = Instantiate(data.prefab, new Vector3(centerX, groundY, 0), Quaternion.identity, platformContainer);

        // Chỉnh collider cho khớp
        var col = plat.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(length, col.size.y);
            col.offset = Vector2.zero;
        }

        // --- THỨ TỰ SINH QUAN TRỌNG ---

        // 2. Sinh Obstacle (trên đoạn đất này)
        if (obstacleSpawner != null)
            obstacleSpawner.SpawnObstacles(startX, endX, groundY);

        // 3. Sinh Mini Platform (trên đoạn đất này)
        if (miniPlatformSpawner != null)
            miniPlatformSpawner.SpawnMiniPlatforms(startX, endX, groundY);

        // 4. Sinh Item (dựa trên những gì đã có)
        if (itemSpawner != null)
            itemSpawner.SpawnItems(startX, endX, groundY);

        // Cập nhật mép
        lastEdgeX = endX;
    }
}