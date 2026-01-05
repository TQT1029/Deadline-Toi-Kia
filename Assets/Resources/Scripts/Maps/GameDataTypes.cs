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
public class ObstacleData
{
    public string id;
    public GameObject prefab;
    public float heightOffset = 0f;

    public Vector2 centerPos = Vector2.zero;
    // Mặc định là (1,1). Nếu bạn set tay giá trị khác trong Inspector, code sẽ dùng giá trị đó thay vì tự tính.
    public Vector2 size = Vector2.one;
    public Vector2 GetSize()
    {
        if (prefab == null) return Vector2.one;

        // Nếu đã nhập tay size khác (1,1) thì ưu tiên dùng số nhập tay
        if (size != Vector2.one && size != Vector2.zero) return size;

        // 1. Lấy tất cả Collider2D trong prefab (bao gồm cả object cha và các con)
        var colliders = prefab.GetComponentsInChildren<Collider2D>(true);

        if (colliders.Length > 0)
        {
            // Khởi tạo bounds bằng collider đầu tiên tìm thấy
            Bounds combinedBounds = colliders[0].bounds;

            // 2. Duyệt qua các collider còn lại và mở rộng bounds để bao trùm tất cả
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            //Debug.Log($"[ObstacleData] Calculated size for Obstacle '{id}': {combinedBounds.size}");
            // 3. Trả về kích thước tổng (Width, Height)
            // Bounds.size trong Unity đã tự động tính toán cả Scale của transform rồi
            return combinedBounds.size;
        }

        // Fallback: Nếu không tìm thấy collider nào, trả về mặc định
        return Vector2.one;
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
// 4. ITEM DATA CƠ BẢN
[System.Serializable]
public class ItemData
{
    public string id;
    public GameObject prefab;
    public int scoreValue = 1;
    public float spawnWeight = 10f;
}
