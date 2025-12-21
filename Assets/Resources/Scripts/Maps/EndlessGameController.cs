using UnityEngine;

public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;
    private void Awake() => Instance = this;

    [Header("Managers")]
    public MapGenerator mapGenerator;
    public ItemGenerator itemGenerator;

    [Header("Settings")]
    public Transform player;
    public float generationDistance = 80f;

    [Header("Cleanup Settings")]
    public float destroyDistanceBehind = 50f;

    private float lastEdgeX = 0f;
    private float lastItemEdgeX = 0f;

    private void Start()
    {
        if (player == null) player = ReferenceManager.Instance.PlayerTransform;

        int oldPit = mapGenerator.pitChance;
        mapGenerator.pitChance = 0;

        for (int i = 0; i < 3; i++)
        {
            SpawnNextPiece();
        }

        mapGenerator.pitChance = oldPit;
    }

    private void Update()
    {
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnNextPiece();
        }

        CleanupOldObjects();
    }

    private void SpawnNextPiece()
    {
        // Sinh đất
        float newEdgeX = mapGenerator.SpawnNextSegment(lastEdgeX);
        lastEdgeX = newEdgeX;

        // Sync lần cuối ở controller để chắc chắn mọi thứ đã khớp collider
        Physics2D.SyncTransforms();

        // Kiểm tra xem MapGenerator đã "chốt" được đoạn nào chưa (có Obstacle/Platform)
        // Kể cả đất dài hay hố, nếu LastPopulatedEdge tăng lên, ta rải item
        if (mapGenerator.LastPopulatedEdge > lastItemEdgeX)
        {
            itemGenerator.GenerateItems(lastItemEdgeX, mapGenerator.LastPopulatedEdge);
            lastItemEdgeX = mapGenerator.LastPopulatedEdge;
        }
    }

    private void CleanupOldObjects()
    {
        Transform[] containers = {
            mapGenerator.basePlatformObjs,
            mapGenerator.obstacleObjs,
            mapGenerator.miniPlatformObjs,
            itemGenerator.itemContainer
        };

        foreach (Transform container in containers)
        {
            if (container == null) continue;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (player.position.x - child.position.x > destroyDistanceBehind)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}