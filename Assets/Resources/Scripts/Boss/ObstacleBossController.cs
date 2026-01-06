using UnityEngine;
using System.Collections;

public class ObstacleBossController : MonoBehaviour
{
    [Header("Assets")]
    public MoveObstacleBoss obstaclePrefab; // Prefab có gắn script MoveObstacleBoss

    [Header("Settings")]
    public float moveDuration = 3f;

    // Enum định nghĩa các kiểu tấn công
    public enum AttackPattern
    {
        RainDown_AllAtOnce, // Rơi xuống đồng loạt
        RainDown_LeftToRight, // Rơi lần lượt từ trái qua phải
        TargetPlayer // Rơi thẳng vào vị trí giữa (giả lập nhắm vào player)
    }

    public void ExecuteAttack(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.RainDown_AllAtOnce:
                SpawnRow(false); // false nghĩa là không delay giữa các object
                break;
            case AttackPattern.RainDown_LeftToRight:
                SpawnRow(true); // true nghĩa là có delay tạo hiệu ứng lượn sóng
                break;
            case AttackPattern.TargetPlayer:
                SpawnSingleAtCenter();
                break;
        }
    }

    // Pattern: Rơi thành hàng ngang
    private void SpawnRow(bool isSequential)
    {
        int count = 5; // Số lượng vật thể
        float step = 1f / (count - 1); // Chia khoảng cách viewport X

        for (int i = 0; i < count; i++)
        {
            float viewportX = step * i; // 0, 0.25, 0.5, 0.75, 1

            // Nếu là tuần tự (LeftToRight) thì delay tăng dần theo i
            float delay = isSequential ? i * 0.2f : 0f;

            CreateObstacle(
                new Vector2(viewportX, 1.2f), // Bắt đầu: Trên đỉnh màn hình (Y > 1)
                new Vector2(viewportX, -0.2f), // Kết thúc: Dưới đáy màn hình (Y < 0)
                delay
            );
        }
    }

    // Pattern: Rơi 1 cái ở giữa
    private void SpawnSingleAtCenter()
    {
        CreateObstacle(new Vector2(0.5f, 1.2f), new Vector2(0.5f, -0.2f), 0f);
    }

    // Hàm helper để sinh object và cài đặt thông số
    private void CreateObstacle(Vector2 startView, Vector2 endView, float delay)
    {
        MoveObstacleBoss obj = Instantiate(obstaclePrefab);
        // Boss Manager hoặc Controller nên quản lý parent để gọn Scene
        obj.transform.SetParent(transform);

        // Gửi lệnh cho "MoveObstacleBoss" thực thi
        obj.Initialize(startView, endView, moveDuration, delay);
    }
}