using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner Instance;

    [Header("Cấu hình Khoảng Cách (Quan trọng)")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 chướng ngại vật")]
    [SerializeField] private float minGap = 8.0f;
    [Tooltip("Khoảng cách tối đa giữa 2 chướng ngại vật")]
    [SerializeField] private float maxGap = 15.0f;
    // Gợi ý: Set minGap = 8, maxGap = 15 sẽ khớp với nhịp của ItemSpawner (distanceBetweenGroups = 12)

    [Header("Danh sách Vật Cản")]
    [SerializeField] private List<GameObject> objectsToRandomize;

    [Header("Phạm vi")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    private bool isCalculating = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm Async Task để LevelGenerator gọi
    public async Task RandomizeObjects()
    {
        if (isCalculating) return;
        if (objectsToRandomize == null || objectsToRandomize.Count == 0) return;
        if (startPoint == null || endPoint == null) return;

        isCalculating = true;

        // 1. Lấy dữ liệu Main Thread
        float sX = startPoint.position.x;
        float eX = endPoint.position.x;
        int count = objectsToRandomize.Count;

        // Cache lại Y, Z cũ (để giữ độ cao của máy giặt/bàn ghế nếu bạn đã chỉnh sẵn)
        List<Vector2> originalYZ = new List<Vector2>();
        foreach (var obj in objectsToRandomize)
        {
            if (obj != null) originalYZ.Add(new Vector2(obj.transform.position.y, obj.transform.position.z));
            else originalYZ.Add(Vector2.zero);
        }

        // 2. Chạy tính toán tuyến tính ở Background Thread
        List<float?> resultPositions = await Task.Run(() =>
        {
            return CalculateLinearPositions(sX, eX, count);
        });

        // 3. Gán vị trí ở Main Thread
        if (this == null) return;

        for (int i = 0; i < objectsToRandomize.Count; i++)
        {
            GameObject obj = objectsToRandomize[i];
            if (obj == null) continue;

            float? newX = resultPositions[i];

            if (newX.HasValue)
            {
                // Gán vị trí mới (X mới, Y cũ, Z cũ)
                obj.transform.position = new Vector3(newX.Value, originalYZ[i].x, originalYZ[i].y);
                obj.SetActive(true);
            }
            else
            {
                // Nếu hết đường (vượt quá EndPoint) thì tắt bớt các vật thừa đi
                obj.SetActive(false);
            }
        }

        isCalculating = false;
        // Debug.Log("Đã rải vật cản xong!");
    }

    // --- LOGIC MỚI: Rải đều theo đường thẳng ---
    private List<float?> CalculateLinearPositions(float startX, float endX, int count)
    {
        System.Random sysRandom = new System.Random();
        List<float?> results = new List<float?>();

        // Bắt đầu từ vị trí Start + một khoảng nhỏ
        float currentCursor = startX + 5f;

        for (int i = 0; i < count; i++)
        {
            // 1. Random khoảng cách bước nhảy (Gap)
            // Công thức: Random * (Max - Min) + Min
            double randomGap = sysRandom.NextDouble() * (maxGap - minGap) + minGap;

            // 2. Cộng dồn vào vị trí hiện tại
            currentCursor += (float)randomGap;

            // 3. Kiểm tra xem đã vượt quá EndPoint chưa
            if (currentCursor < endX)
            {
                results.Add(currentCursor);
            }
            else
            {
                // Hết đường rồi -> Trả về null để tắt vật thể này đi
                results.Add(null);
            }
        }

        return results;
    }
}