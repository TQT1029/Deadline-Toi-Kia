using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Dùng cho List

public class HUDController : Singleton<HUDController>
{
    [Header("In-Game HUD")]
    [SerializeField] private TMP_Text distanceText => UIManager.Instance.DistanceText;
    [SerializeField] private TMP_Text learnScoreText => UIManager.Instance.LearnScoreText; // Điểm vật phẩm (sách/vở)
    [SerializeField] private TMP_Text xpScoreText => UIManager.Instance.XPScoreText;    // Điểm kinh nghiệm tổng

    [Header("End Game Animation")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject[] stars; // Kéo 3 ảnh ngôi sao vào đây (Star 1, 2, 3)

    [SerializeField] private TMP_Text resultDistanceText;
    [SerializeField] private TMP_Text resultLearnScoreText;
    [SerializeField] private TMP_Text resultXPScoreText;

    // Cập nhật UI liên tục
    public void UpdateHUD(float distance, int learnScore, int xpScore)
    {
        if (distanceText) distanceText.text = $"{distance:F0}m"; // F0 là làm tròn không lấy số thập phân
        if (learnScoreText) learnScoreText.text = $"{learnScore}";
        if (xpScoreText) xpScoreText.text = $"{xpScore}";
    }

    // Hiển thị bảng kết quả cuối game
    public void ShowResult(int starCount, int totalScore)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            // Tắt hết sao trước
            foreach (var star in stars) star.SetActive(false);

            // Bật số sao đạt được
            // Ví dụ starCount = 2, vòng lặp chạy i=0, i=1 -> Bật 2 ngôi sao đầu
            for (int i = 0; i < starCount && i < stars.Length; i++)
            {
                stars[i].SetActive(true);
            }

        }
    }
}