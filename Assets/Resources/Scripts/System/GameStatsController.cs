using UnityEngine;

public class GameStatsController : MonoBehaviour
{
    public static GameStatsController Instance;

    [Header("Settings")]
    public float scoreMultiplier = 1f;

    [Header("Star Thresholds")]
    [SerializeField] private int oneStarScore = 1000;
    [SerializeField] private int twoStarScore = 2000;
    [SerializeField] private int threeStarScore = 3000;
    [SerializeField] private int fourStarScore = 5000;
    [SerializeField] private int fiveStarScore = 7000;

    public float resultDistance { get; private set; }
    public int resultCoin { get; private set; }
    public int resultRank { get; private set; }
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
        HUDController.Instance.UpdateHUD(resultDistance, resultCoin);
    }

    public void CollectCoinItem(int amount = 1)
    {
        resultCoin += amount;
        resultRank += amount;
    }

    public void HitObstacleBoss(int minAmount, int maxAmount)
    {
        resultCoin = Mathf.Max(0, resultCoin - Random.Range(minAmount, maxAmount + 1));
    }

    public void StartMap()
    {
        resultDistance = 0f;
        resultCoin = 0;
        resultRank = RankingManager.Instance.CurrentRank;
        isGameActive = true;
    }

    // --- SỬA HÀM NÀY ---
    public void FinishLevel()
    {
        isGameActive = false;

        // Tính số sao
        int starsEarned = 0;
        if (resultCoin >= fiveStarScore) starsEarned = 5;
        else if (resultCoin >= fourStarScore) starsEarned = 4;
        else if (resultCoin >= threeStarScore) starsEarned = 3;
        else if (resultCoin >= twoStarScore) starsEarned = 2;
        else if (resultCoin >= oneStarScore) starsEarned = 1;

        Debug.Log($"Kết thúc! Rank: {resultRank} - Sao: {starsEarned}");

        resultRank = RankingManager.Instance.CurrentRank;

        // GỌI HUD VỚI ĐẦY ĐỦ THAM SỐ
        HUDController.Instance.ShowResult(starsEarned, resultDistance, resultCoin, resultRank);

        // Dừng game
        Time.timeScale = 0f;
    }
}