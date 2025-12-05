using UnityEngine;

public class PanelControls : MonoBehaviour
{
    [Header("Panels References")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject pausePanel;

    private bool isPopupOpen = false;

    // Tự động tìm reference nếu quên kéo thả
    private void Awake()
    {
        if (settingPanel == null && UIManager.Instance != null) settingPanel = UIManager.Instance.SettingPanel;
        if (pausePanel == null && UIManager.Instance != null) pausePanel = UIManager.Instance.PausePanel;
    }

    // --- Mở Panel ---
    public void OpenPanel(GameObject panelToOpen)
    {
        if (isPopupOpen) return;

        panelToOpen.SetActive(true);
        isPopupOpen = true;
        Time.timeScale = 0f; // Dừng game
    }

    // --- Đóng Panel (Kèm logic thông minh) ---
    public void ClosePanel(GameObject panelToClose)
    {
        panelToClose.SetActive(false);
        isPopupOpen = false;

        // Chỉ resume game nếu KHÔNG CÓ panel Pause nào đang bật
        if (pausePanel == null || !pausePanel.activeSelf)
        {
            Time.timeScale = 1f;
        }
    }

    // --- Wrapper cho UI Buttons (Gán vào nút trong Inspector) ---
    public void OpenSettings() => OpenPanel(settingPanel);
    public void CloseSettings() => ClosePanel(settingPanel);

    public void OpenTutorial() => OpenPanel(tutorialPanel);
    public void CloseTutorial() => ClosePanel(tutorialPanel);

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);

        isPopupOpen = false;
        Time.timeScale = 1f;
    }
}