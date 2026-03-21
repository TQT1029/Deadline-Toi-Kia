using UnityEngine;

[RequireComponent (typeof(Collider2D))]
public class WinArea : MonoBehaviour
{
    [SerializeField] private Collider2D areaCollider;

    private void Start()
    {
        if (areaCollider == null) areaCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        ClearExitArea();
    }

    // Xóa các vật cản trở khu vực win
    private void ClearExitArea()
    {
        if (areaCollider == null) return;

        // [QUAN TRỌNG] Đồng bộ vật lý ngay lập tức để chắc chắn rằng
        // các vật thể vừa được SpawnNextPiece tạo ra ở frame này sẽ bị quét trúng
        Physics2D.SyncTransforms();

        Vector2 pointA = areaCollider.bounds.min;
        Vector2 pointB = areaCollider.bounds.max;

        // Quét toàn bộ collider dính vào khu vực WinArea
        Collider2D[] allOverlapObj = Physics2D.OverlapAreaAll(pointA, pointB);

        foreach (Collider2D hit in allOverlapObj)
        {
            // Thay vì xóa trực tiếp hit.gameObject (rất dễ xóa nhầm collider con),
            // ta gọi hàm tìm gốc Prefab.
            Transform targetToDestroy = GetPrefabRoot(hit.transform);

            if (targetToDestroy != null)
            {
                Destroy(targetToDestroy.gameObject);
            }
        }
    }

    // Hàm "leo cây" để tìm chính xác Prefab gốc
    private Transform GetPrefabRoot(Transform child)
    {
        Transform current = child;
        Transform rootToDestroy = null;

        // Leo ngược lên các object cha
        while (current != null)
        {
            // Nếu phát hiện bản thân nó hoặc cha của nó có tag hợp lệ, ghi nhớ lại
            if (current.CompareTag("Obstacle") || current.CompareTag("MiniPlatform") || current.CompareTag("Item"))
            {
                rootToDestroy = current;
            }
            // Tiếp tục leo lên xem cha của nó còn Tag không
            // Điều này giúp lấy được Object ngoài cùng nhất của cụm Prefab
            current = current.parent;
        }

        // Trả về object cha cao nhất mang Tag hợp lệ
        // (Nếu đụng trúng đất nền Ground, nó sẽ không có tag này và trả về null -> tha mạng)
        return rootToDestroy;
    }
}

