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
    public float chunkLength = 40f;

    private float lastEdgeX = 0f;

    private void Start()
    {
        if (player == null) player = ReferenceManager.Instance.PlayerTransform;
        
        // Spawn đoạn đầu tiên (An toàn)
        // Để an toàn, ta có thể tạm tắt pitChance trong MapGenerator rồi bật lại
        int oldPit = mapGenerator.pitChance;
        mapGenerator.pitChance = 0;
        SpawnChunk();
        mapGenerator.pitChance = oldPit;
    }

    private void Update()
    {
        if (player.position.x + generationDistance > lastEdgeX)
        {
            SpawnChunk();
        }
    }

    private void SpawnChunk()
    {
        float startX = lastEdgeX;
        float endX = startX + chunkLength;

        // --- THỨ TỰ TUYỆT ĐỐI ---
        
        // 1, 2, 3. Tạo phần cứng (Đất -> Obstacle -> MiniPlatform)
        // Logic này nằm gọn trong MapGenerator để đảm bảo chúng biết vị trí của nhau
        mapGenerator.GenerateChunk(startX, endX);

        // 4. Tạo Item (Dựa trên kết quả của bước trên)
        // ItemGenerator sẽ bắn raycast vào map vừa tạo để tìm điểm đặt
        itemGenerator.GenerateItems(startX, endX);

        lastEdgeX = endX;
    }
}