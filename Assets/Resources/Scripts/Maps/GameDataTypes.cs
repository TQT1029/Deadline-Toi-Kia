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
    public float heightOffset = 0f;

    // Mặc định là (1,1). Nếu bạn set tay giá trị khác trong Inspector, code sẽ dùng giá trị đó thay vì tự tính.
    public Vector2 size = Vector2.one;
    public Vector2 GetSize()
    {
        if (prefab == null) return Vector2.one;

        // Ưu tiên dùng số nhập tay
        if (size != Vector2.one && size != Vector2.zero) return size;

        // --- BƯỚC SỬA: Tạo vật thể tạm ---
        GameObject tempObj = GameObject.Instantiate(prefab);
        tempObj.transform.position = Vector3.zero;
        tempObj.transform.rotation = Quaternion.identity;
        // Đảm bảo object bật lên để collider hoạt động
        tempObj.SetActive(true);

        var colliders = tempObj.GetComponentsInChildren<Collider2D>(true);
        Vector2 finalSize = Vector2.one;

        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            finalSize = combinedBounds.size;
        }

        // --- Xóa vật thể tạm đi ---
        // Dùng DestroyImmediate nếu chạy trong Editor/ScriptableObject, Destroy nếu chạy runtime
#if UNITY_EDITOR
        GameObject.DestroyImmediate(tempObj);
#else
    GameObject.Destroy(tempObj);
#endif

        return finalSize;
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
