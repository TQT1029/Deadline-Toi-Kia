using UnityEngine;
using System.Threading.Tasks;

public class MapController : MonoBehaviour
{
    // Hàm này sẽ được gọi khi bắt đầu game hoặc reset màn chơi
    public async void GenerateLevel()
    {
        // 1. Chạy ObstacleSpawner và CHỜ cho đến khi nó xong
        if (ObstacleSpawner.Instance != null)
        {
            // Debug.Log("Đang xếp chướng ngại vật...");
            await ObstacleSpawner.Instance.RandomizeObjects();
        }

        // 2. QUAN TRỌNG: Chờ 1 frame để Physics cập nhật Collider
        // Nếu không chờ, ItemSpawner sẽ không thấy vật cản vừa di chuyển tới
        await Task.Yield();

        // 3. Sau khi vật cản đã yên vị, mới chạy ItemSpawner
        if (ItemSpawner.Instance != null)
        {
            // Debug.Log("Đang rải vật phẩm...");
            ItemSpawner.Instance.GenerateItems();
        }

        // Debug.Log("Hoàn tất tạo Map!");
    }

    // Test thử bằng nút bấm
    [ContextMenu("Test Generate")]
    public void TestGen()
    {
        GenerateLevel();
    }
}