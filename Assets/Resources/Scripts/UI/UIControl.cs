using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControl : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Kéo Panel Setting vào đây")]
    [SerializeField] private GameObject settingPanel;

    [Tooltip("Kéo Panel Pause (nếu có) vào đây")]
    [SerializeField] private GameObject pausePanel;

    [Header("Scene Names")]
    private string gameplaySceneName = "GamePlay"; // Tên scene muốn load khi Play
    private string mainMenuSceneName = "MainMenu";        // Tên scene Main Menu


    private void Awake()
    {
        if(settingPanel==null) settingPanel = UIManager.Instance.SettingPanel;
        if(pausePanel==null) pausePanel = UIManager.Instance.PausePanel;

    }

    // ====================================================
    // 1. SETTINGS LOGIC
    // ====================================================

    // Gán hàm này vào nút "Settings" (Hình bánh răng)
    public void OpenSettings()
    {
        settingPanel.SetActive(true);
        Time.timeScale = 0f; // Dừng game khi mở setting
    }

    // Gán hàm này vào nút "X" hoặc "Close" bên trong Setting Panel
    public void CloseSettings()
    {
        settingPanel.SetActive(false);

        // Logic thông minh: Chỉ cho chạy lại thời gian nếu Pause Panel KHÔNG bật
        // (Tránh trường hợp đang Pause game, mở Setting, tắt Setting -> Game tự chạy lại dù chưa resume)
        if (pausePanel == null || !pausePanel.activeSelf)
        {
            Time.timeScale = 1f;
        }
    }

    // ====================================================
    // 2. PAUSE / RESUME LOGIC
    // ====================================================

    // Gán vào nút Pause trong game
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Gán vào nút "Resume" hoặc "Continue"
    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false); // Đảm bảo tắt cả setting nếu lỡ đang bật

        Time.timeScale = 1f;
    }

    // ====================================================
    // 3. NAVIGATION (Chuyển Scene)
    // ====================================================

    public void PlayBtn()
    {
        Time.timeScale = 1f; // Luôn reset time scale khi load scene mới
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void MainMenuBtn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitBtn()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}