using UnityEngine;

public static class GameUtils
{
    /// <summary>
    /// Tính toán bao giới hạn (Bounds) bao trùm toàn bộ vật thể và các con của nó.
    /// </summary>
    public static Bounds GetBounds(GameObject obj)
    {
        var colliders = obj.GetComponentsInChildren<Collider2D>(true); // true để lấy cả object đang inactive
        var renderers = obj.GetComponentsInChildren<Renderer>(true);

        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        // Ưu tiên lấy theo Collider
        if (colliders.Length > 0)
        {
            combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                combinedBounds.Encapsulate(colliders[i].bounds);
            hasBounds = true;
        }
        // Nếu không có collider (ví dụ item chỉ có sprite), lấy theo Renderer
        else if (renderers.Length > 0)
        {
            combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combinedBounds.Encapsulate(renderers[i].bounds);
            hasBounds = true;
        }

        if (!hasBounds)
        {
            // Fallback nếu object rỗng
            combinedBounds.center = obj.transform.position;
            combinedBounds.size = Vector3.zero;
        }

        return combinedBounds;
    }

    /// <summary>
    /// Tìm Object cha cao nhất (Root) đại diện cho vật cản (tránh lấy nhầm child collider).
    /// </summary>
    public static GameObject GetObstacleRoot(Transform child)
    {
        Transform current = child;
        while (current.parent != null)
        {
            // Dừng lại nếu gặp Container tổng hoặc một object không có tag cụ thể
            if (current.parent.CompareTag("Container") || current.parent.name.Contains("Container"))
            {
                return current.gameObject;
            }
            current = current.parent;
        }
        return current.gameObject;
    }

    /// <summary>
    /// Lấy điểm chạm bề mặt (ưu tiên Obstacle/Platform).
    /// </summary>
    public static RaycastHit2D GetSurfaceHit(float xPos, LayerMask layerMask)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(xPos, 20f), Vector2.down, 50f, layerMask);

        foreach (var h in hits)
        {
            if (h.collider.CompareTag("Obstacle") || h.collider.CompareTag("MiniPlatform"))
                return h;
        }

        if (hits.Length > 0) return hits[Random.Range(0,hits.Length)];
        return default;
    }
}