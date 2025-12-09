using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks; // Cần thư viện này để dùng Task
using System.Linq; // Dùng để xử lý List tiện hơn

public class RandomPlacementController : MonoBehaviour
{
    public static RandomPlacementController Instance;

    [Header("Cấu hình Random")]
    [Tooltip("Mảng chứa các vật thể được random")]
    [SerializeField] private List<GameObject> objectsToRandomize;

    [Tooltip("Vị trí đầu")]
    [SerializeField] private Transform startPoint;

    [Tooltip("Vị trí cuối")]
    [SerializeField] private Transform endPoint;

    [Tooltip("Khoảng cách tối thiểu")]
    [SerializeField] private float minDistance = 10.0f;

    [Tooltip("Số lần thử tối đa")]
    [SerializeField] private int maxAttempts = 100;

    // Biến để khóa không cho spam nút khi đang tính toán
    private bool isCalculating = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    [ContextMenu("Test Randomize Async")]
    public async void RandomizeObjects()
    {
        // 1. Kiểm tra điều kiện & Khóa spam
        if (isCalculating) return;
        if (objectsToRandomize == null || objectsToRandomize.Count == 0) return;
        if (startPoint == null || endPoint == null) return;

        isCalculating = true;

        // 2. Lấy dữ liệu cần thiết ở Main Thread (Vì Thread phụ không được truy cập Transform của Unity)
        float sX = startPoint.position.x;
        float eX = endPoint.position.x;
        int objectCount = objectsToRandomize.Count;

        // Lưu lại Y và Z của từng object để gán lại sau này
        // (Giả sử các object có thể có Y, Z khác nhau, ta cache lại)
        List<Vector2> originalYZ = new List<Vector2>();
        foreach (var obj in objectsToRandomize)
        {
            if (obj != null) originalYZ.Add(new Vector2(obj.transform.position.y, obj.transform.position.z));
            else originalYZ.Add(Vector2.zero); // Placeholder cho object null
        }

        // 3. Chạy tính toán ở luồng phụ (Background Thread)
        // Lưu ý: Trong Task.Run không được dùng UnityEngine.Random, phải dùng System.Random
        List<float?> resultPositions = await Task.Run(() =>
        {
            return CalculatePositionsLogic(sX, eX, objectCount);
        });

        // 4. Quay lại Main Thread để áp dụng vị trí
        if (this == null) return; // Kiểm tra nếu script bị hủy trong lúc chờ

        for (int i = 0; i < objectsToRandomize.Count; i++)
        {
            GameObject obj = objectsToRandomize[i];
            if (obj == null) continue;

            // Lấy kết quả đã tính
            float? newX = resultPositions[i];

            if (newX.HasValue)
            {
                // Áp dụng vị trí mới, giữ nguyên Y và Z cũ
                obj.transform.position = new Vector3(newX.Value, originalYZ[i].x, originalYZ[i].y);
                obj.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Không tìm được chỗ trống cho object thứ {i}.");
                obj.SetActive(false);
            }
        }

        isCalculating = false;
        Debug.Log("Hoàn tất Random vị trí (Async)!");
    }

    // Hàm này chạy hoàn toàn trên luồng phụ, KHÔNG được đụng vào Unity API (Transform, GameObject...)
    private List<float?> CalculatePositionsLogic(float startX, float endX, int count)
    {
        // Tạo bộ Random của C# System (không dùng Unity Random)
        System.Random sysRandom = new System.Random();

        float minX = Mathf.Min(startX, endX);
        float maxX = Mathf.Max(startX, endX);
        float padding = 0.5f;
        minX += padding;
        maxX -= padding;

        List<float> usedPositions = new List<float>();
        List<float?> results = new List<float?>();

        for (int i = 0; i < count; i++)
        {
            bool found = false;
            float chosenX = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Random số thực trong khoảng (System.Random trả về 0.0 -> 1.0)
                double range = maxX - minX;
                double sample = sysRandom.NextDouble();
                float candidateX = (float)(sample * range) + minX;

                // Kiểm tra va chạm
                bool isOverlapping = false;
                foreach (float existingX in usedPositions)
                {
                    if (Mathf.Abs(candidateX - existingX) < minDistance)
                    {
                        isOverlapping = true;
                        break;
                    }
                }

                if (!isOverlapping)
                {
                    chosenX = candidateX;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                usedPositions.Add(chosenX);
                results.Add(chosenX);
            }
            else
            {
                results.Add(null); // Đánh dấu là không tìm thấy
            }
        }

        return results;
    }
}