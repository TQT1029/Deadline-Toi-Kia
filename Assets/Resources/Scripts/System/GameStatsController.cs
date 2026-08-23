using UnityEngine;

public class GameStatsController : MonoBehaviour
{
    public static GameStatsController Instance;

    [Header("Settings")]
    public float scoreMultiplier = 1f;

    [Header("Star Thresholds")]
    private int oneStarScore = 0;
    private int twoStarScore = 400;
    private int threeStarScore = 800;
    private int fourStarScore = 1500;
    private int fiveStarScore = 2700;

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

        if (ReferenceManager.Instance != null && ReferenceManager.Instance.PlayerRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            float velX = ReferenceManager.Instance.PlayerRigidbody.linearVelocity.x;
#else
            float velX = ReferenceManager.Instance.PlayerRigidbody.velocity.x;
#endif
            if (velX > 0f)
            {
                resultDistance += velX * Time.deltaTime;
            }
        }

        // Bắn event cập nhật số liệu
        GameEvents.TriggerStatsUpdated(resultDistance, resultCoin);

        // Fallback trực tiếp
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateHUD(resultDistance, resultCoin);
        }
    }

    public void CollectCoinItem(int amount = 1)
    {
        resultCoin += amount;
        GameEvents.TriggerStatsUpdated(resultDistance, resultCoin);
    }

    public void HitObstacleBoss(int minAmount, int maxAmount)
    {
        resultCoin = Mathf.Max(0, resultCoin - Random.Range(minAmount, maxAmount + 1));
        GameEvents.TriggerStatsUpdated(resultDistance, resultCoin);
    }

    public void StartMap()
    {
        resultDistance = 0f;
        resultCoin = 0;
        resultRank = (RankingManager.Instance != null) ? RankingManager.Instance.CurrentRank : 1;
        isGameActive = true;
        GameEvents.TriggerStatsUpdated(resultDistance, resultCoin);
    }

    public void FinishLevel()
    {
        isGameActive = false;
        resultRank = (RankingManager.Instance != null) ? RankingManager.Instance.CurrentRank : 1;

        // Tính số sao (Float calculation)
        int starsEarned = 0;
        float rankMultiplier = Mathf.Max(0.8f, (float)resultRank / 10f);
        float resultScores = (resultDistance + (resultCoin * rankMultiplier)) * scoreMultiplier;

        if (resultScores >= fiveStarScore) starsEarned = 5;
        else if (resultScores >= fourStarScore) starsEarned = 4;
        else if (resultScores >= threeStarScore) starsEarned = 3;
        else if (resultScores >= twoStarScore) starsEarned = 2;
        else if (resultScores >= oneStarScore) starsEarned = 1;

        Debug.Log($"[GameStatsController] Finish! Rank: {resultRank} - Score: {resultScores:F1} - Stars: {starsEarned}");

        // Bắn event kết thúc màn
        GameEvents.TriggerLevelFinished(starsEarned, resultDistance, (int)resultScores, resultRank);

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowResult(starsEarned, resultDistance, (int)resultScores, resultRank);
        }

        Time.timeScale = 0f;
    }
}