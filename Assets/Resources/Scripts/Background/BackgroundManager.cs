using UnityEngine;
using System.Collections.Generic; // Cần dùng List
#if UNITY_EDITOR
using UnityEditor;
#endif

// Script chạy trong Editor để tính toán Z-depth tự động
[ExecuteInEditMode]
public class BackgroundManager : MonoBehaviour
{
    [Header("Z-Sorting Settings")]
    [Tooltip("Vị trí Z của layer xa nhất (Background)")]
    [SerializeField] private float farthestZ = 100f;
    [Tooltip("Vị trí Z của layer gần nhất (Foreground)")]
    [SerializeField] private float nearestZ = 50f;

    [Header("Layer Management")]
    [SerializeField] private bool autoSortOnValidate = true;

    // Sử dụng Transform[] để tránh lỗi GameObject đã bị hủy
    public Transform[] Layers { get; private set; }

    private void Awake()
    {
        // Fetch Layers an toàn khi bắt đầu chơi
        if (Application.isPlaying)
        {
            FetchLayers();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Chạy trong Editor để cập nhật ngay lập tức khi thay đổi
        if (autoSortOnValidate)
        {
            FetchLayers();
            SortLayersDepth();
        }
    }
#endif

    public void FetchLayers()
    {
        // SỬA LỖI: Sử dụng List để lưu trữ tạm thời và lọc các Transform đã bị hủy
        List<Transform> validLayers = new List<Transform>();

        // Lặp qua tất cả các đối tượng con
        foreach (Transform child in transform)
        {
            // Kiểm tra xem đối tượng có null (đã bị hủy) hay không.
            // Trong Editor, khi xóa object, nó có thể trở thành null trong một vài frame.
            if (child != null)
            {
                validLayers.Add(child);
            }
        }

        // Cập nhật mảng Layers
        Layers = validLayers.ToArray();
    }

    [ContextMenu("Sort Layers Z-Depth")]
    public void SortLayersDepth()
    {
        // Đảm bảo Layers được cập nhật trước khi sắp xếp
        FetchLayers();

        int count = Layers.Length;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            Transform currentLayer = Layers[i];

            // KIỂM TRA LỖI LẦN NỮA: Đảm bảo Transform vẫn còn tồn tại trước khi truy cập
            if (currentLayer == null) continue;

            // Đổi tên
            currentLayer.name = $"Layer_{i}";

            // Tính toán Z position
            // t = tỉ lệ vị trí từ 0 (xa nhất) đến 1 (gần nhất)
            float t = (count <= 1) ? 0f : (float)i / (count - 1);
            float zPos = Mathf.Lerp(farthestZ, nearestZ, t);

            // Cập nhật vị trí Z
            Vector3 newPos = currentLayer.localPosition;
            newPos.z = zPos;
            currentLayer.localPosition = newPos;
        }
    }
}