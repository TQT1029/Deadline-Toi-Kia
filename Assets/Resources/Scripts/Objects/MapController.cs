using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks; // Để dùng Async

public class MapController : MonoBehaviour
{
    public static MapController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Resources")]
    public GameObject itemPrefab;
    public List<ObstacleData> obstacles; // Kéo các loại vật cản vào đây

    [Header("Map Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public float groundY = -2f; // Độ cao mặt đất
    public GameObject obstacleObjs;
    public GameObject itemObjs;

    [Header("Spacing Settings")]
    [Tooltip("Khoảng cách tối thiểu giữa các sự kiện (Vật cản/Nhóm Item)")]
    public float minGap = 5f;
    [Tooltip("Khoảng cách tối đa")]
    public float maxGap = 8f;

    [Header("Item Logic")]
    [Range(0, 100)] public int chanceToSpawnObstacle = 40; // 40% ra vật cản, 60% ra nhóm item
    [Range(0, 100)] public int chanceItemOnObstacle = 70;   // 70% vật cản sẽ có item trên đầu
    public float itemSpacing = 1.2f;

    // Enum các kiểu xếp item
    private enum ItemPattern { Line, Wave, Grid, Arc, ZigZag, Arrow, Stairs }
    [ContextMenu("Generate Level")]
    public async void GenerateLevel()
    {
        // 1. Xóa sạch map cũ
        ClearMap();

        if (startPoint == null || endPoint == null) return;

        float currentX = startPoint.position.x;
        float endX = endPoint.position.x;
        int elementCount = 0;

        // 2. Vòng lặp rải vật thể từ đầu đến cuối map
        while (currentX < endX)
        {
            // Random khoảng cách bước nhảy tiếp theo
            float gap = Random.Range(minGap, maxGap);
            currentX += gap;

            if (currentX >= endX) break;

            // Quyết định xem spawn cái gì: Vật cản hay Nhóm Item?
            bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;

            if (spawnObstacle)
            {
                // --- SPAWN VẬT CẢN (KÈM ITEM TRÊN ĐẦU) ---
                float addedWidth = SpawnObstacleLogic(currentX);
                currentX += addedWidth; // Cộng thêm chiều rộng vật cản để con trỏ X đi tiếp
            }
            else
            {
                // --- SPAWN NHÓM ITEM BAY ---
                float addedWidth = SpawnItemPatternLogic(currentX);
                currentX += addedWidth;
            }

            elementCount++;

            // Kỹ thuật Async: Cứ 10 vật thể thì nghỉ 1 frame để game mượt
            if (elementCount % 10 == 0) await Task.Yield();
        }

        Debug.Log("Hoàn tất tạo màn chơi!");
    }

    // --- LOGIC 1: TẠO VẬT CẢN & ITEM TRÊN NÓC ---
    private float SpawnObstacleLogic(float posX)
    {
        if (obstacles == null || obstacles.Count == 0) return 0;

        // 1. Chọn random 1 loại vật cản
        ObstacleData obsData = obstacles[Random.Range(0, obstacles.Count)];

        // 2. Spawn vật cản tại mặt đất
        Vector3 spawnPos = new Vector3(posX, groundY, 0);
        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);
        if (obstacleObjs != null)
            obsObj.transform.SetParent(obstacleObjs.transform);
        else
            obsObj.transform.SetParent(this.transform);

        // 3. Xử lý Item trên nóc (Theo xác suất)
        if (Random.Range(0, 100) < chanceItemOnObstacle)
        {
            // Tính vị trí trên nóc
            float topY = groundY + obsData.topHeightOffset;
            int count = Random.Range(1, obsData.maxItemsOnTop + 1);

            // Rải item canh giữa vật cản
            float startXItem = posX - ((count - 1) * itemSpacing) / 2;

            for (int i = 0; i < count; i++)
            {
                Vector3 itemPos = new Vector3(startXItem + (i * itemSpacing), topY, 0);
                SpawnItem(itemPos);
            }
        }

        // Trả về chiều rộng vật cản để vòng lặp chính cộng dồn vào currentX
        return obsData.width;
    }

    // --- LOGIC 2: TẠO NHÓM ITEM (HÌNH DÁNG) ---
    private float SpawnItemPatternLogic(float startX)
    {
        ItemPattern pattern = (ItemPattern)Random.Range(0, 3);
        float patternWidth = 0;
        float baseItemY = groundY + 1.5f; // Item bay lơ lửng tầm thấp

        switch (pattern)
        {
            case ItemPattern.Line:
                int lineCount = Random.Range(3, 6);
                for (int i = 0; i < lineCount; i++)
                {
                    SpawnItem(new Vector3(startX + (i * itemSpacing), baseItemY, 0));
                }
                patternWidth = lineCount * itemSpacing;
                break;

            case ItemPattern.Wave:
                int waveCount = Random.Range(5, 10);
                for (int i = 0; i < waveCount; i++)
                {
                    float yOffset = Mathf.Sin(i * 0.5f) * 1.5f;
                    SpawnItem(new Vector3(startX + (i * itemSpacing), baseItemY + yOffset, 0));
                }
                patternWidth = waveCount * itemSpacing;
                break;

            case ItemPattern.Grid: // Hình vuông 3x2
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        SpawnItem(new Vector3(startX + (x * itemSpacing), baseItemY + (y * itemSpacing), 0));
                    }
                }
                patternWidth = 3 * itemSpacing;
                break;
        }

        return patternWidth;
    }

    private void SpawnItem(Vector3 pos)
    {
        if (itemPrefab)
        {
            GameObject item = Instantiate(itemPrefab, pos, Quaternion.identity);
            if (itemObjs != null)
                item.transform.SetParent(itemObjs.transform);
            else
                item.transform.SetParent(this.transform);
        }
    }

    private void ClearMap()
    {
        if (obstacleObjs == null || itemObjs == null)
        {
            // Xóa an toàn cho Async
            int childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }

        }

        if (obstacleObjs != null)
        {
            int obstacleCount = obstacleObjs.transform.childCount;
            for (int i = obstacleCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(obstacleObjs.transform.GetChild(i).gameObject);
                else DestroyImmediate(obstacleObjs.transform.GetChild(i).gameObject);
            }

        }
        if (itemObjs != null)
        {
            int itemCount = itemObjs.transform.childCount;
            for (int i = itemCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(itemObjs.transform.GetChild(i).gameObject);
                else DestroyImmediate(itemObjs.transform.GetChild(i).gameObject);
            }
        }
    }
}