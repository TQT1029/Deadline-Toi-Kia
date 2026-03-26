using UnityEngine;

[System.Serializable]
public class BasePlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;
    public float manualLength = 0f;

    public float GetLength()
    {
        if (prefab == null) return 20f;
        if (manualLength > 0) return manualLength;
        var col = prefab.GetComponent<BoxCollider2D>();
        return col != null ? col.size.x * prefab.transform.lossyScale.x : 20f;
    }
}

[System.Serializable]
public class MiniPlatformData
{
    public string id;
    public GameObject prefab;
    public float spawnWeight = 10f;
    public float manualLength = 0f;

    public float GetLength()
    {
        if (prefab == null) return 5f;

        // Nếu đã nhập tay thì lấy luôn, không cần tính
        if (manualLength > 0f) return manualLength;

        var boxCol = prefab.GetComponent<BoxCollider2D>();
        if (boxCol != null)
            return boxCol.size.x * prefab.transform.localScale.x;

        return 5f; // Giá trị mặc định nếu không tìm thấy collider nào
    }
}

[System.Serializable]
public class ObstacleData
{
    public string id;
    public GameObject prefab;
    public Vector2 manualSize = Vector2.zero; // Đổi tên size cũ thành manualSize để tránh nhầm lẫn
    [Tooltip("Chỉ cần bật khi dùng vật thể trong Pit Obj.")] public bool useGroundY = true;

    // BIẾN MỚI ĐỂ LƯU KẾT QUẢ
    private Vector2 _cachedSize;
    private bool _isCalculated = false;

    // Hàm khởi tạo (Gọi 1 lần duy nhất lúc Start game)
    public void Initialize()
    {
        if (_isCalculated) return; // Đã tính rồi thì thôi

        // 1. Nếu có nhập tay thì lấy số nhập tay
        if (manualSize != Vector2.zero)
        {
            _cachedSize = manualSize;
        }
        else if (prefab != null)
        {
            // 2. Nếu không nhập tay -> Tính toán chính xác bằng cách tạo vật thể tạm
            // Vì chỉ chạy 1 lần lúc Loading nên Instantiate thoải mái
            GameObject temp = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);

            temp.SetActive(true);

            var colliders = temp.GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
                _cachedSize = bounds.size;
            }
            else
            {
                _cachedSize = Vector2.one; // Fallback
            }

            // Xóa ngay lập tức
#if UNITY_EDITOR
            GameObject.DestroyImmediate(temp);
#else
            GameObject.Destroy(temp);
#endif
        }

        _isCalculated = true; // Đánh dấu là đã tính xong
        //Debug.Log($"[Cache] Obstacle {id} size: {_cachedSize}");
    }

    // Hàm GetSize bây giờ siêu nhẹ, chỉ trả về biến đã lưu
    public Vector2 GetSize()
    {
        // Phòng hờ nếu quên gọi Initialize ở Start thì tự tính lần đầu
        if (!_isCalculated) Initialize();
        return _cachedSize;
    }
}
// 4. ITEM DATA CƠ BẢN
[System.Serializable]
public class ItemData
{
    public string id;
    public GameObject prefab;
    public int scoreValue = 1;
    public float spawnChance = 10f;
}
