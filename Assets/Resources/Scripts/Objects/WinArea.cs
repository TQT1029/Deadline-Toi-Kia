using UnityEngine;

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
        Vector2 pointA = areaCollider.bounds.min;
        Vector2 pointB = areaCollider.bounds.max;
        Collider2D[] allOverlapObj = Physics2D.OverlapAreaAll(pointA, pointB);
        foreach (var obj in allOverlapObj)
        {
            if (obj.gameObject.CompareTag("Obstacle") || obj.gameObject.CompareTag("MiniPlatform") || obj.gameObject.CompareTag("Item"))
            {
                Destroy(obj.gameObject);
            }
        }
    }

}
