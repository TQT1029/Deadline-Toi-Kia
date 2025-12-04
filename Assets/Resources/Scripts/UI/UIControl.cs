using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControl : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Kéo Panel Setting vào đây")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject tutorialPanel;

    [Tooltip("Kéo Panel Pause (nếu có) vào đây")]
    [SerializeField] private GameObject pausePanel;

    private bool isPopupOpen = false;

    [Header("Scene Names")]
    private string selectedCharacterSceneName = "CharacterSelection"; // Tên scene chọn nhân vật
    private string gameplaySceneName = "MapCafe"; // Tên scene muốn load khi Play
    private string mainMenuSceneName = "MainMenu";        // Tên scene Main Menu


    private void Awake()
    {
        if (settingPanel == null) settingPanel = UIManager.Instance.SettingPanel;
        if (pausePanel == null) pausePanel = UIManager.Instance.PausePanel;

    }

    // ====================================================
    // 1. PANEL LOGIC
    // ====================================================

    // Gán hàm này vào nút "Settings" (Hình bánh răng)
    public void OpenSettings()
    {
        if (isPopupOpen) return; // Ngăn chặn mở nhiều popup cùng lúc
        settingPanel.SetActive(true);
        isPopupOpen = true;
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
        isPopupOpen = false;
    }
    public void OpenTutorial()
    {
        if (isPopupOpen) return; // Ngăn chặn mở nhiều popup cùng lúc
        tutorialPanel.SetActive(true);
        isPopupOpen = true;
        Time.timeScale = 0f; // Dừng game khi mở tutorial
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        // Logic thông minh: Chỉ cho chạy lại thời gian nếu Pause Panel KHÔNG bật
        // (Tránh trường hợp đang Pause game, mở Setting, tắt Setting -> Game tự chạy lại dù chưa resume)
        if (pausePanel == null || !pausePanel.activeSelf)
        {
            Time.timeScale = 1f;
        }
        isPopupOpen = false;
    }

    public void LanguageUpdate(int languageIndex)
    {

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
        SceneManager.LoadScene(selectedCharacterSceneName);
    }

    public void GoBtn()
    {
        Time.timeScale = 1f;
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

    // ====================================================
    // 4. Character Selection LOGIC
    // ====================================================

    public void CurrentSelectedCharacter(int characterIndex)
    {
        UIManager.Instance.SelectedCharacter(characterIndex);
    }
}