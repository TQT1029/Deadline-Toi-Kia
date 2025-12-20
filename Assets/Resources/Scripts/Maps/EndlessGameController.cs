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

    private float lastEdgeX = 0f; // Mép phải ngoài cùng của bản đồ hiện tại

    private void Start()
    {
        if (player == null) player = ReferenceManager.Instance.PlayerTransform;

        // Khởi tạo đoạn đầu tiên (Đất bằng phẳng an toàn)
        int oldPit = mapGenerator.pitChance;
        mapGenerator.pitChance = 0; // Tắt hố đoạn đầu

        // Spawn 3 đoạn đầu tiên làm nền
        for (int i = 0; i < 3; i++)
        {
            SpawnNextPiece();
            Physics2D.SyncTransforms(); // Đồng bộ vật lý ngay sau khi spawn đất để tránh lỗi va chạm
            if (mapGenerator.currentGrounds.ContainsKey(mapGenerator.groundIDCounter - 1))
                SpawnObstacle();
        }

        mapGenerator.pitChance = oldPit; // Bật lại hố
    }

    private void Update()
    {
        // Kiểm tra nếu người chơi sắp đi hết đường thì spawn tiếp
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnNextPiece();
            Physics2D.SyncTransforms(); // Đồng bộ vật lý ngay sau khi spawn đất để tránh lỗi va chạm
            if (mapGenerator.currentGrounds.ContainsKey(mapGenerator.groundIDCounter - 1))
                SpawnObstacle();
        }

        CleanupOldObjects();
    }

    private void SpawnNextPiece()
    {
        float startX = lastEdgeX;

        // BƯỚC 1: Gọi MapGenerator sinh đoạn đất/hố tiếp theo
        // Hàm này sẽ tự động sinh Obstacle và MiniPlatform đi kèm luôn
        // và trả về vị trí kết thúc mới (newEdge)
        float newEdgeX = mapGenerator.SpawnNextSegment(startX);
        // Cập nhật mép mới
        lastEdgeX = newEdgeX;
    }

    private void SpawnObstacle()
    {
        mapGenerator.SpawnObstaclesOnSegment(mapGenerator.currentGrounds[mapGenerator.groundIDCounter].startX, mapGenerator.currentGrounds[mapGenerator.groundIDCounter].endX);
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