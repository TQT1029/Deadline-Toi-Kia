using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMapProfile", menuName = "Data/Map Profile")]
public class MapProfile : ScriptableObject
{
    [Header("1. Basic Info & Audio")]
    public string mapName;
    [Tooltip("Âm thanh nền của map")]
    public string idBGM;
    [Tooltip("Index dùng để xác định map")]
    public int mapIndex;

    [Header("2. Pacing & Progression Settings")]
    [Tooltip("Khoảng cách Safe Zone lúc xuất phát (m)")]
    public float safeZoneDistance = 50f;
    [Tooltip("Khoảng cách chạy đến khi gặp Boss (m)")]
    public float distanceToBoss = 500f;
    [Tooltip("Thời gian sống sót để hạ Boss (giây)")]
    public float timeToDefeat = 90f;
    [Tooltip("Khoảng cách từ điểm hạ Boss đến cổng WinPoint")]
    public float winPointOffset = 250f;
    [Tooltip("Tầm nhìn sinh map phía trước (m)")]
    public float generationDistance = 120f;
    [Tooltip("Khoảng cách dọn dẹp vật thể phía sau người chơi (m)")]
    public float destroyDistanceBehind = 60f;

    [Header("3. Coordinate & Geography Settings")]
    public float groundY = -5f;
    public float pitY = -10f;
    public float maxHeightMap = 15f;
    public bool hasPit = true;
    [Range(0, 100)] public int pitChance = 30;
    public float waveFrequency = 0.4f;

    [Header("4. Base Ground & Pit Segment Settings")]
    public float minGroundSegmentLength = 30f;
    public float maxGroundSegmentLength = 75f;
    public float minPitWidth = 3f;
    public float maxPitWidth = 6f;

    [Header("5. Obstacles & Aerial Platforms Settings")]
    [Range(0, 100)] public int ratioObstacleToAerial = 70;
    [Range(0, 100)] public int changeSpawnObstacle = 80;
    public float obstacleEdgePadding = 4f;
    public float minObstacleGap = 7f;
    public float maxObstacleGap = 12f;
    public float minimumHeight = 3f;
    public float minVerticalGap = -1f;
    public float maxVerticalGap = 3f;
    public float minHorizontalGap = 1f;
    public float maxHorizontalGap = 3f;
    public float minZoneObstacleLength = 30f;
    public float maxZoneObstacleLength = 60f;
    public float minZoneAerialLength = 30f;
    public float maxZoneAerialLength = 60f;

    [Header("6. Pit Hazards & Bridge Settings")]
    public bool isSpawnObjectInPit = true;
    [Range(0, 100)] public float ratioBridgeToObstacle = 50f;
    public float pitWidthNeedObjects = 15f;
    public float pitEdgePadding = 2f;
    public float minPitVerticalGap = -1f;
    public float maxPitVerticalGap = 2f;
    public float minPitHorizontalGap = 0.5f;
    public float maxPitHorizontalGap = 1.5f;
    public float stepGap = 0.5f;

    [Header("7. Item & Coin Settings")]
    public float itemSpacing = 1.25f;
    public float patternPadding = 2f;
    public float groundPadding = 1f;
    public float minGap = 5f;
    public float maxGap = 7f;
    [Range(0, 100)] public float obstacleChanceItems = 70f;
    [Range(0, 100)] public float platformChanceItems = 70f;

    [Header("8. Prefab Libraries (Data-Driven Asset Lists)")]
    public List<BasePlatformData> baseLibrary = new List<BasePlatformData>();
    public List<ObstacleData> obstacleLibrary = new List<ObstacleData>();
    public List<MiniPlatformData> miniPlatformLibrary = new List<MiniPlatformData>();
    public List<ObstacleData> pitObstacleLibrary = new List<ObstacleData>();
    public List<MiniPlatformData> pitMiniPlatformLibrary = new List<MiniPlatformData>();
    public List<ItemData> commonItems = new List<ItemData>();
}