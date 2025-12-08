using UnityEngine;

public class GameStatsManager : Singleton<GameStatsManager>
{
    [Header("Settings")]
    public float scoreMultiplier = 1f; // Hệ số nhân điểm (mặc định là 1)

    [Header("Star Thresholds (Mốc điểm để đạt sao)")]
    public int oneStarScore = 100;  // Ví dụ: > 100 điểm = 1 sao
    public int twoStarScore = 300;  // Ví dụ: > 300 điểm = 2 sao
    public int threeStarScore = 600;// Ví dụ: > 600 điểm = 3 sao
    public int fourStarScore = 1000; // Ví dụ: > 1000 điểm = 4 sao
    public int fiveStarScore = 1500; // Ví dụ: > 1500 điểm = 5 sao

    // Các biến lưu trữ nội bộ
    private float currentDistance;
    private int currentLearnScore;
    private int currentXPScore;
    private bool isGameActive = true;

    private void Update()
    {
        if (!isGameActive) return;

        // 1. Tính khoảng cách (Giả sử runSpeed lấy từ PlayerController)
        // Nếu bạn dùng script PlayerController ở câu trả lời trước, hãy truy cập runSpeed từ đó
        // Ở đây mình ví dụ cộng dồn theo thời gian
        if (ReferenceManager.Instance.PlayerTransform != null)
        {
            // Cách 1: Tính theo vị trí X thực tế của nhân vật
            currentDistance = ReferenceManager.Instance.PlayerTransform.position.x;
        }

        // 2. Cập nhật lên HUD
        HUDController.Instance.UpdateHUD(currentDistance, currentLearnScore, currentXPScore);
    }

    // --- LOGIC ĂN VẬT PHẨM ---

    // Gọi hàm này khi ăn vật phẩm thường (Sách, Laptop...)
    public void CollectLearnItem(int amount = 1)
    {
        currentLearnScore += amount;

        // Mặc định XP tăng bằng Document Score
        currentXPScore += amount;
    }

    // Gọi hàm này khi ăn vật phẩm X2
    public void CollectDoubleXPItem()
    {
        // Yêu cầu của bạn: "nếu nhặt được vật phẩm x2 XP score thì sẽ được x2 XP Score HIỆN TẠI"
        currentXPScore *= 2;
        Debug.Log("Đã X2 XP Score! Điểm hiện tại: " + currentXPScore);
    }

    // --- LOGIC KẾT THÚC MÀN CHƠI ---

    public void FinishLevel()
    {
        isGameActive = false;

        // Tính tổng điểm cuối cùng (Tùy logic game, ở đây lấy XP Score làm chuẩn xếp hạng)
        int finalScore = currentXPScore;

        // Tính số sao
        int starsEarned = 0;
        if (finalScore >= fiveStarScore) starsEarned = 5;
        else if (finalScore >= fourStarScore) starsEarned = 4;
        else if (finalScore >= threeStarScore) starsEarned = 3;
        else if (finalScore >= twoStarScore) starsEarned = 2;
        else if (finalScore >= oneStarScore) starsEarned = 1;

        Debug.Log($"Kết thúc! Điểm: {finalScore} - Sao: {starsEarned}");

        // Hiển thị UI kết quả
        HUDController.Instance.ShowResult(starsEarned, finalScore);

        // Dừng game
        Time.timeScale = 0f;
    }
}