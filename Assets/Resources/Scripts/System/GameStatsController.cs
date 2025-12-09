using UnityEngine;

public class GameStatsController : MonoBehaviour
{
    public static GameStatsController Instance;

    [Header("Settings")]
    public float scoreMultiplier = 1f;

    [Header("Star Thresholds")]
    public int oneStarScore = 100;
    public int twoStarScore = 300;
    public int threeStarScore = 600;
    public int fourStarScore = 1000;
    public int fiveStarScore = 1500;

    public float resultDistance { get; private set; }
    public int resultDocument { get; private set; }
    public int resultXPScore { get; private set; }
    private bool isGameActive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!isGameActive) return;
        if (ReferenceManager.Instance.PlayerTransform != null)
        {
            // Dùng linearVelocityX (Unity 6) hoặc velocity.x tùy phiên bản
            resultDistance += ReferenceManager.Instance.PlayerRigidbody.linearVelocity.x * Time.deltaTime;
        }
        HUDController.Instance.UpdateHUD(resultDistance, resultDocument, resultXPScore);
    }

    public void CollectLearnItem(int amount = 1)
    {
        resultDocument += amount;
        resultXPScore += amount;
    }

    public void CollectDoubleXPItem()
    {
        resultXPScore *= 2;
    }

    public void StartMap()
    {
        resultDistance = 0f;
        resultDocument = 0;
        resultXPScore = 0;
        isGameActive = true;
    }

    // --- SỬA HÀM NÀY ---
    public void FinishLevel()
    {
        isGameActive = false;

        // Tính số sao
        int starsEarned = 0;
        if (resultXPScore >= fiveStarScore) starsEarned = 5;
        else if (resultXPScore >= fourStarScore) starsEarned = 4;
        else if (resultXPScore >= threeStarScore) starsEarned = 3;
        else if (resultXPScore >= twoStarScore) starsEarned = 2;
        else if (resultXPScore >= oneStarScore) starsEarned = 1;

        Debug.Log($"Kết thúc! XP: {resultXPScore} - Sao: {starsEarned}");

        // GỌI HUD VỚI ĐẦY ĐỦ THAM SỐ
        HUDController.Instance.ShowResult(starsEarned, resultDistance, resultDocument, resultXPScore);

        // Dừng game
        Time.timeScale = 0f;
    }
}