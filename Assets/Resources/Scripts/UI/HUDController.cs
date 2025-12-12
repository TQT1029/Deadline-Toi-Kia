using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Bắt buộc

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    // ... (Giữ nguyên các Reference Header cũ) ...
    [Header("In-Game HUD")]
    [SerializeField] private TMP_Text distanceText => UIManager.Instance.DistanceText;
    [SerializeField] private TMP_Text coinText => UIManager.Instance.CoinText;
    [SerializeField] private TMP_Text xpScoreText => UIManager.Instance.XPScoreText;

    [Header("End Game Animation")]
    [SerializeField] private GameObject resultPanel => UIManager.Instance.ResultPanel;
    [SerializeField] private GameObject[] stars => UIManager.Instance.Stars;

    [SerializeField] private Animator animatorObj1 => UIManager.Instance.AnimatorObj1;
    [SerializeField] private Animator animatorObj2 => UIManager.Instance.AnimatorObj2;

    [SerializeField] private TMP_Text resultDistanceText => UIManager.Instance.ResultDistanceText;
    [SerializeField] private TMP_Text resultCoinText => UIManager.Instance.ResultCoinText;
    [SerializeField] private TMP_Text resultXPScoreText => UIManager.Instance.ResultXPScoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    public void UpdateHUD(float distance, int learnScore, int xpScore)
    {
        if (distanceText) distanceText.text = $"{distance:F1}m";
        if (coinText) coinText.text = $"{learnScore}";
        if (xpScoreText) xpScoreText.text = $"{xpScore}";
    }

    // --- SỬA HÀM NÀY ĐỂ NHẬN ĐỦ THAM SỐ VÀ CHẠY ANIMATION ---
    public void ShowResult(int starCount, float distance, int docScore, int xpScore)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // 1. Hiển thị thông số kết quả
        if (resultDistanceText) resultDistanceText.text = $"{distance:F1}m";
        if (resultCoinText) resultCoinText.text = $"{docScore}";
        if (resultXPScoreText) resultXPScoreText.text = $"{xpScore}";

        // 2. Setup hình ảnh nhân vật (nếu có logic chọn skin)
        if (ReferenceManager.Instance.CurrentSelectedProfile != null)
        {
            if (animatorObj1)
            {
                animatorObj1.runtimeAnimatorController = ReferenceManager.Instance.CurrentSelectedProfile.inGameAnimator;
                animatorObj1.SetBool("isJump", true);
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