using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Bắt buộc

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("In-Game HUD")]
    [SerializeField] private TMP_Text distanceText => UIManager.Instance.DistanceText;
    [SerializeField] private TMP_Text coinText => UIManager.Instance.CoinText;
    [SerializeField] private TMP_Text xpScoreText => UIManager.Instance.XPScoreText;

    [Header("Ranking UI")]
    [Tooltip("Text hiển thị chữ 'TOP 1'")]
    [SerializeField] private TMP_Text rankTitleText => UIManager.Instance.RankTitleText;
    [Tooltip("Text hiển thị số '01/25'")]
    [SerializeField] private TMP_Text rankDetailText => UIManager.Instance.RankDetailText;

    [Header("Rank Effect Config")]
    [SerializeField] private Color rankUpColor = Color.green; // Màu khi lên hạng (Tốt)
    [SerializeField] private Color rankDownColor = Color.red; // Màu khi tụt hạng (Tệ)
    [SerializeField] private float blinkDuration = 0.5f; // Thời gian nháy

    [Header("End Game Animation")]
    [SerializeField] private GameObject resultPanel => UIManager.Instance.ResultPanel;
    [SerializeField] private GameObject[] stars => UIManager.Instance.Stars;

    [SerializeField] private Animator animatorObj1 => UIManager.Instance.AnimatorObj1;
    [SerializeField] private Animator animatorObj2 => UIManager.Instance.AnimatorObj2;

    [SerializeField] private TMP_Text resultDistanceText => UIManager.Instance.ResultDistanceText;
    [SerializeField] private TMP_Text resultXPScoreText => UIManager.Instance.ResultXPScoreText;
    [SerializeField] private TMP_Text resultRankText => UIManager.Instance.ResultRankText;

    private int lastRank = -1; // Lưu currentRank frame trước để so sánh
    private Color originalRankColor; // Lưu màu gốc để trả về
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
    }
    private void Start()
    {
        // Lưu màu gốc ban đầu của Text Rank
        if (rankTitleText != null) originalRankColor = rankTitleText.color;
    }
    private void Update()
    {
        // Cập nhật Rank liên tục mỗi khung hình
        UpdateRankingUI();
    }

    private void UpdateRankingUI()
    {
        // Nếu không có RankingManager thì thôi
        if (RankingManager.Instance == null) return;

        int currentRank = RankingManager.Instance.CurrentRank;
        int totalRunner = RankingManager.Instance.TotalRacers;

        // 1. Cập nhật Text
        if (rankTitleText != null) rankTitleText.text = $"TOP {currentRank}";
        if (rankDetailText != null) rankDetailText.text = $"{currentRank:00}/{totalRunner:00}";

        // 2. XỬ LÝ HIỆU ỨNG THAY ĐỔI RANK
        // Chỉ chạy nếu currentRank thay đổi và không phải lần đầu tiên (lastRank != -1)
        if (lastRank != -1 && currentRank != lastRank)
        {
            if (currentRank < lastRank)
            {
                // Rank số nhỏ hơn nghĩa là thứ hạng cao hơn (VD: 2 -> 1) => RANK UP (Tốt)
                PlayRankChangeEffect(rankUpColor);
            }
            else
            {
                // Rank số lớn hơn nghĩa là tụt hạng (VD: 1 -> 3) => RANK DOWN (Tệ)
                PlayRankChangeEffect(rankDownColor);
            }
        }

        // Cập nhật lại lastRank
        lastRank = currentRank;
    }

    private void PlayRankChangeEffect(Color targetColor)
    {
        if (rankTitleText == null) return;

        // Kill các tween cũ đang chạy dở để tránh xung đột
        rankColorTween?.Kill();
        rankScaleTween?.Kill();

        // Đảm bảo scale về gốc trước khi bump
        rankTitleText.transform.localScale = Vector3.one;
        rankTitleText.color = targetColor; // Đổi màu ngay lập tức để người chơi thấy rõ

        // Hiệu ứng Scale: Phóng to lên 1.5 lần rồi thu về 1 (PunchScale)
        rankScaleTween = rankTitleText.transform.DOPunchScale(Vector3.one * 0.5f, blinkDuration, 10, 1);

        // Hiệu ứng Màu: Từ từ chuyển lại về màu trắng gốc
        rankColorTween = rankTitleText.DOColor(originalRankColor, blinkDuration)
            .SetDelay(0.2f); // Delay một chút để giữ màu xanh/đỏ lâu hơn xíu
    }
    public void UpdateHUD(float distance, int learnScore)
    {
        if (distanceText) distanceText.text = $"{distance:F1}m";
        if (coinText) coinText.text = $"{learnScore}";
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
        if (ReferenceManager.Instance.CurrentSelectedProfile != null)
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
        // Reset trạng thái các sao trước khi chạy animation
        foreach (var star in stars)
        {
            star.SetActive(false);
            star.transform.localScale = Vector3.zero; // Thu nhỏ về 0
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
}