using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;
    private void Awake() => Instance = this;

    [Header("Sub-Spawners")]
    public BasePlatformSpawner baseSpawner;
    public ObstacleSpawner obstacleSpawner;
    public MiniPlatformSpawner miniPlatformSpawner;
    public ItemSpawner itemSpawner;

    [Header("Settings")]
    public Transform player;
    public float generationDistance = 100f;
    public float chunkLength = 50f; // Độ dài mỗi lần sinh map
    public float groundY = -2f;

    private float lastGenX = 0f;

    private void Start()
    {
        // Sinh đoạn đầu tiên (chắc chắn an toàn, không hố)
        GenerateChunk(safe: true);
    }

    private void Update()
    {
        if (player.position.x + generationDistance > lastGenX)
        {
            GenerateChunk(safe: false);
        }
    }

    void GenerateChunk(bool safe)
    {
        float startX = lastGenX;
        float endX = startX + chunkLength;

        // BƯỚC 1: TẠO ĐẤT & HỐ (NỀN TẢNG)
        List<BasePlatformSpawner.GroundSegment> grounds;
        List<BasePlatformSpawner.PitSegment> pits;

        // Nếu safe mode thì không tạo hố
        if (safe) baseSpawner.pitChance = 0;
        baseSpawner.Spawn(startX, endX, groundY, out grounds, out pits);
        if (safe) baseSpawner.pitChance = 30; // Reset lại

        // BƯỚC 2: TẠO VẬT CẢN (TRÊN ĐẤT)
        // Chỉ spawn trên ground, tránh mép
        obstacleSpawner.Spawn(grounds);

        // BƯỚC 3: TẠO MINI PLATFORM (QUA HỐ & TRÊN CAO)
        miniPlatformSpawner.Spawn(pits, grounds);

        // BƯỚC 4: TẠO ITEM (TRÊN TẤT CẢ)
        // ItemSpawner sẽ raycast xuống để tìm vị trí Obstacle/Platform đã sinh ở B2, B3
        itemSpawner.Spawn(startX, endX);

        lastGenX = endX;
    }
}