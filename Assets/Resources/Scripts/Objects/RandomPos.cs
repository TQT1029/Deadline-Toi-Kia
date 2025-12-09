using UnityEngine;
using System.Collections.Generic;
public class RandomPos : MonoBehaviour
{
    public static RandomPos Instance;

    [Tooltip("Mảng chứa các vật thể được random")]
    [SerializeField] private List<GameObject> objectsToRandomize ;

    [Tooltip("Vị trí đầu - cuối cần random")]
    [SerializeField] private Transform startPoint, endPoint;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void RandomizeObjects()
    {
        if (objectsToRandomize.Count == 0 || startPoint == null || endPoint == null)
        {
            Debug.LogWarning("Vui lòng kiểm tra lại các tham số đã được gán đúng chưa!");
            return;
        }
        float minX = Mathf.Min(startPoint.localPosition.x, endPoint.localPosition.x);
        float maxX = Mathf.Max(startPoint.localPosition.x, endPoint.localPosition.x);
        float fixedY = startPoint.localPosition.y; // Giữ y cố định

        Vector3 startPosition = new Vector3(minX, fixedY, 0);
        Vector3 endPosition = new Vector3(maxX, fixedY, 0);
        foreach (GameObject obj in objectsToRandomize)
        {
            if (obj == null) continue;

            float randomX = Vector3.Lerp(startPosition, endPosition, Random.Range(0f, 1f)).x;
            obj.transform.localPosition = new Vector3(randomX, fixedY, obj.transform.localPosition.z);

        }
    }
}
