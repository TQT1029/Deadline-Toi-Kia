using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BackgroundManager : MonoBehaviour
{
    [Header("Z-Depth Config")]
    [Tooltip("Z xa nhất của Background")]
    [SerializeField] private float bgFarthestZ = 100f;
    [Tooltip("Z gần nhất của Background (sát Player)")]
    [SerializeField] private float bgNearestZ = 10f;

    [Space]
    [Tooltip("Số lượng layer dùng làm tiền cảnh (Foreground). Đặt 0 nếu không có.")]
    [SerializeField] private int foregroundLayerCount = 0;

    [Tooltip("Z bắt đầu của Foreground (Ví dụ -5)")]
    [SerializeField] private float fgStartZ = -5f;
    [Tooltip("Khoảng cách Z giữa các lớp Foreground (nếu có nhiều lớp)")]
    [SerializeField] private float fgSpacing = -5f;

    [Header("Editor")]
    [SerializeField] private bool autoSortOnValidate = false;

    public List<Transform> Layers { get; private set; } = new List<Transform>();

    private void Awake()
    {
        if (Application.isPlaying) FetchLayers();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoSortOnValidate)
        {
            FetchLayers();
            SortLayersDepth();
        }
    }
#endif

    public void FetchLayers()
    {
        Layers.Clear();
        // Lấy tất cả object con đang active
        foreach (Transform child in transform)
        {
            if (child != null && child.gameObject.activeSelf)
                Layers.Add(child);
        }
    }

    [ContextMenu("Auto Sort Z-Depth")]
    public void SortLayersDepth()
    {
        FetchLayers();
        int totalCount = Layers.Count;
        if (totalCount == 0) return;

        // Đảm bảo số lượng foreground không vượt quá tổng số layer
        int safeFgCount = Mathf.Clamp(foregroundLayerCount, 0, totalCount);
        int bgCount = totalCount - safeFgCount;

        for (int i = 0; i < totalCount; i++)
        {
            Transform layer = Layers[i];
            if (layer == null) continue;

            float zPos;

            // Xử lý Background (Các layer nằm đầu danh sách)
            if (i < bgCount)
            {
                layer.name = $"Layer_BG_{i:00}";
                // Phân bố đều từ Xa -> Gần
                float t = (bgCount <= 1) ? 0f : (float)i / (bgCount - 1);
                zPos = Mathf.Lerp(bgFarthestZ, bgNearestZ, t);
            }
            // Xử lý Foreground (Các layer nằm cuối danh sách)
            else
            {
                int fgIndex = i - bgCount; // Index riêng của nhóm FG (0, 1, 2...)
                layer.name = $"Layer_FG_{fgIndex:00}";

                // Foreground càng về sau thì càng gần camera (Z càng âm)
                zPos = fgStartZ + (fgIndex * fgSpacing);
            }

            Vector3 newPos = layer.localPosition;
            newPos.z = zPos;
            layer.localPosition = newPos;
        }
    }
}