using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Bắt buộc

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("In-Game HUD Direct References (Optional)")]
    [SerializeField] private TMP_Text serializedDistanceText;
    [SerializeField] private TMP_Text serializedCoinText;
    [SerializeField] private TMP_Text serializedXpScoreText;
    [SerializeField] private TMP_Text serializedRankTitleText;
    [SerializeField] private TMP_Text serializedRankDetailText;

    private TMP_Text distanceText => serializedDistanceText != null ? serializedDistanceText : (UIManager.Instance != null ? UIManager.Instance.DistanceText : null);
    private TMP_Text coinText => serializedCoinText != null ? serializedCoinText : (UIManager.Instance != null ? UIManager.Instance.CoinText : null);
    private TMP_Text xpScoreText => serializedXpScoreText != null ? serializedXpScoreText : (UIManager.Instance != null ? UIManager.Instance.XPScoreText : null);
    private TMP_Text rankTitleText => serializedRankTitleText != null ? serializedRankTitleText : (UIManager.Instance != null ? UIManager.Instance.RankTitleText : null);
    private TMP_Text rankDetailText => serializedRankDetailText != null ? serializedRankDetailText : (UIManager.Instance != null ? UIManager.Instance.RankDetailText : null);

    [Header("Rank Effect Config")]
    [SerializeField] private Color rankUpColor = Color.green;
    [SerializeField] private Color rankDownColor = Color.red;
    [SerializeField] private float blinkDuration = 0.5f;

    [Header("End Game Animation Direct References (Optional)")]
    [SerializeField] private GameObject serializedResultPanel;
    [SerializeField] private GameObject[] serializedStars;
    [SerializeField] private Animator serializedAnimatorObj1;
    [SerializeField] private Animator serializedAnimatorObj2;
    [SerializeField] private TMP_Text serializedResultDistanceText;
    [SerializeField] private TMP_Text serializedResultXPScoreText;
    [SerializeField] private TMP_Text serializedResultRankText;

    private GameObject resultPanel => serializedResultPanel != null ? serializedResultPanel : (UIManager.Instance != null ? UIManager.Instance.ResultPanel : null);
    private GameObject[] stars => (serializedStars != null && serializedStars.Length > 0) ? serializedStars : (UIManager.Instance != null ? UIManager.Instance.Stars : null);
    private Animator animatorObj1 => serializedAnimatorObj1 != null ? serializedAnimatorObj1 : (UIManager.Instance != null ? UIManager.Instance.AnimatorObj1 : null);
    private Animator animatorObj2 => serializedAnimatorObj2 != null ? serializedAnimatorObj2 : (UIManager.Instance != null ? UIManager.Instance.AnimatorObj2 : null);
    private TMP_Text resultDistanceText => serializedResultDistanceText != null ? serializedResultDistanceText : (UIManager.Instance != null ? UIManager.Instance.ResultDistanceText : null);
    private TMP_Text resultXPScoreText => serializedResultXPScoreText != null ? serializedResultXPScoreText : (UIManager.Instance != null ? UIManager.Instance.ResultXPScoreText : null);
    private TMP_Text resultRankText => serializedResultRankText != null ? serializedResultRankText : (UIManager.Instance != null ? UIManager.Instance.ResultRankText : null);

    private int lastRank = -1;
    private int lastTotalRacers = -1;
    private float lastDistance = -1f;
    private int lastCoin = -1;
    private Color originalRankColor = Color.white;
    private Tween rankColorTween;
    private Tween rankScaleTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        AutoResolveLocalReferences();
    }

    private void AutoResolveLocalReferences()
    {
        if (serializedDistanceText == null) serializedDistanceText = transform.Find("DistanceText")?.GetComponent<TMP_Text>();
        if (serializedCoinText == null) serializedCoinText = transform.Find("CoinText")?.GetComponent<TMP_Text>();
        if (serializedXpScoreText == null) serializedXpScoreText = transform.Find("XPScoreText")?.GetComponent<TMP_Text>();
        if (serializedRankTitleText == null) serializedRankTitleText = transform.Find("RankingTitleText")?.GetComponent<TMP_Text>();
        if (serializedRankDetailText == null) serializedRankDetailText = transform.Find("RankingDetailText")?.GetComponent<TMP_Text>();
        if (serializedResultPanel == null) serializedResultPanel = transform.Find("ResultPanel")?.gameObject;
    }

    private void OnEnable()
    {
        GameEvents.OnStatsUpdated += HandleStatsUpdated;
        GameEvents.OnRankingUpdated += HandleRankingUpdated;
        GameEvents.OnLevelFinished += HandleLevelFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnStatsUpdated -= HandleStatsUpdated;
        GameEvents.OnRankingUpdated -= HandleRankingUpdated;
        GameEvents.OnLevelFinished -= HandleLevelFinished;
    }

    private void Start()
    {
        if (rankTitleText != null) originalRankColor = rankTitleText.color;
    }

    private void HandleStatsUpdated(float distance, int coin)
    {
        UpdateHUD(distance, coin);
    }

    private void HandleRankingUpdated(int currentRank, int totalRunner)
    {
        ApplyRankingUI(currentRank, totalRunner);
    }

    private void HandleLevelFinished(int starCount, float distance, int xpScore, int resultRank)
    {
        ShowResult(starCount, distance, xpScore, resultRank);
    }

    private void Update()
    {
        UpdateRankingUI();
    }

    private void ApplyRankingUI(int currentRank, int totalRunner)
    {
        // Cập nhật Text chỉ khi có thay đổi (Dirty check để tránh string allocations)
        if (currentRank != lastRank || totalRunner != lastTotalRacers)
        {
            if (rankTitleText != null) rankTitleText.text = $"TOP {currentRank}";
            if (rankDetailText != null) rankDetailText.text = $"{currentRank:00}/{totalRunner:00}";

            // Xử lý hiệu ứng Rank change
            if (lastRank != -1 && currentRank != lastRank)
            {
                if (currentRank < lastRank)
                {
                    PlayRankChangeEffect(rankUpColor);
                }
                else
                {
                    PlayRankChangeEffect(rankDownColor);
                }
            }

            lastRank = currentRank;
            lastTotalRacers = totalRunner;
        }
    }

    private void UpdateRankingUI()
    {
        if (RankingManager.Instance == null) return;
        ApplyRankingUI(RankingManager.Instance.CurrentRank, RankingManager.Instance.TotalRacers);
    }

    private void PlayRankChangeEffect(Color targetColor)
    {
        if (rankTitleText == null) return;

        rankColorTween?.Kill();
        rankScaleTween?.Kill();

        rankTitleText.transform.localScale = Vector3.one;
        rankTitleText.color = targetColor;

        rankScaleTween = rankTitleText.transform.DOPunchScale(Vector3.one * 0.5f, blinkDuration, 10, 1);
        rankColorTween = rankTitleText.DOColor(originalRankColor, blinkDuration).SetDelay(0.2f);
    }

    public void UpdateHUD(float distance, int coin)
    {
        if (Mathf.Abs(distance - lastDistance) >= 0.1f)
        {
            lastDistance = distance;
            if (distanceText != null) distanceText.text = $"{distance:F1}m";
        }

        if (coin != lastCoin)
        {
            lastCoin = coin;
            if (coinText != null) coinText.text = $"{coin}";
        }
    }

    // --- SỬA HÀM NÀY ĐỂ NHẬN ĐỦ THAM SỐ VÀ CHẠY ANIMATION ---
    public void ShowResult(int starCount, float distance, int xpScore, int resultRankText)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // 1. Hiển thị thông số kết quả
        if (resultDistanceText) resultDistanceText.text = $"{distance:F1}m";
        if (resultXPScoreText) resultXPScoreText.text = $"{xpScore}";
        if (this.resultRankText) this.resultRankText.text = $"{resultRankText}";

        // 2. Setup hình ảnh nhân vật (nếu có logic chọn skin)
        if (ReferenceManager.Instance != null && ReferenceManager.Instance.CurrentSelectedProfile != null)
        {
            if (animatorObj1)
            {
                animatorObj1.runtimeAnimatorController = ReferenceManager.Instance.CurrentSelectedProfile.endAnimation;
            }
            if (animatorObj2)
            {
                animatorObj2.runtimeAnimatorController = ReferenceManager.Instance.CurrentSelectedProfile.previewAction;
            }
        }

        // 3. ANIMATION NGÔI SAO (Đập mạnh)
        PlayStarAnimation(starCount);
    }

    private void PlayStarAnimation(int starCount)
    {
        if (stars == null || stars.Length == 0) return;

        // Reset trạng thái các sao trước khi chạy animation
        foreach (var star in stars)
        {
            if (star != null)
            {
                star.SetActive(false);
                star.transform.localScale = Vector3.zero; // Thu nhỏ về 0
            }
        }

        // Tạo Sequence để chạy hiệu ứng tuần tự
        Sequence seq = DOTween.Sequence();

        // Đảm bảo Sequence chạy kể cả khi Time.timeScale = 0 (Quan trọng vì game đang Pause)
        seq.SetUpdate(true);

        // Duyệt qua số sao đạt được
        for (int i = 0; i < starCount && i < stars.Length; i++)
        {
            int index = i; // Cache index cho lambda
            GameObject starObj = stars[index];

            // Bước 1: Bật sao lên và set scale ban đầu thật to (như đang bay từ ngoài vào mặt)
            seq.AppendCallback(() =>
            {
                starObj.SetActive(true);
                starObj.transform.localScale = Vector3.one * 3f; // Scale to gấp 3 lần

                // (Tùy chọn) Thêm âm thanh đập sao ở đây
                // AudioManager.Instance.PlaySfx("StarHit"); 
            });

            // Bước 2: Hiệu ứng đập mạnh xuống (Scale 3 -> 1) với Ease.OutBounce
            seq.Append(starObj.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce));

            // Bước 3: Nghỉ một chút trước khi đập sao tiếp theo
            seq.AppendInterval(0.2f);
        }
    }

    private void OnDestroy()
    {
        rankColorTween?.Kill();
        rankScaleTween?.Kill();
    }
}