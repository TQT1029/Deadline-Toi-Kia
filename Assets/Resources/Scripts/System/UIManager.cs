using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Characters
{
    HQ,
    LA,
    LN,
    MK,
    NE,
    TT,
}


[DefaultExecutionOrder(-10)]
public class UIManager : Singleton<UIManager>
{
    // Character Mặc định
    public Characters CurrentCharacter = Characters.HQ;

    [Header("Panels")]
    // Các biến này sẽ tự động được gán khi vào scene MainMenu
    public GameObject MainMenuPanel;
    public GameObject SettingPanel;
    public GameObject TutorialPanel;

    [Header("Fields")]
    private Image characterPreview;

    // Các biến này dành cho Scene Gameplay (có thể mở comment khi cần)
    public GameObject HUDPanel;
    public GameObject PausePanel;
    public GameObject GameOverPanel;

    // --- Đăng ký sự kiện ---
    private void OnEnable()
    {
        // Sửa lại cú pháp đúng của Unity: sceneLoaded (chữ thường)
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Lắng nghe sự kiện đổi State từ GameManager
        if (GameManager.Instance != null)
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (GameManager.Instance != null)
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }
    }

    // --- Xử lý khi Scene Load xong ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Logic: Khi vào đúng Scene MainMenu, tự đi tìm các GameObject UI
        if (scene.name == "MainMenu")
        {
            Debug.Log("[UIManager] Đang tìm kiếm UI references cho MainMenu...");

            // LƯU Ý: Tên GameObject trong Hierarchy phải trùng khớp với chuỗi string ở đây
            // Tìm MainMenuPanel (cần đảm bảo object này đang Active hoặc dùng transform.Find nếu nó là con của Canvas)
            MainMenuPanel = GameObject.Find("MainMenuPanel");
            SettingPanel = GameObject.Find("SettingPanel");
            TutorialPanel = GameObject.Find("TutorialPanel");

            // Tự động chuyển State về MainMenu để kích hoạt UI
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
        else if (scene.name == "CharacterSelection")
        {
            SettingPanel = GameObject.Find("SettingPanel");
            characterPreview = GameObject.Find("CharacterPreview").GetComponent<Image>();
        }
        // Mở rộng: Nếu vào Scene Gameplay thì tìm HUD

        else if (scene.name == "") // Hoặc tên scene game của bạn
        {
            HUDPanel = GameObject.Find("HUDPanel");
            PausePanel = GameObject.Find("PausePanel");
            GameOverPanel = GameObject.Find("GameOverPanel");

            GameManager.Instance.ChangeState(GameState.Gameplay);
        }

        HideAllPanels();

    }

    // --- Xử lý bật tắt Panel theo State ---
    private void HandleStateChanged(GameState newState)
    {
        // Tắt tất cả trước khi bật cái cần thiết
        HideAllPanels();

        switch (newState)
        {
            case GameState.MainMenu:
                if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
                break;

            case GameState.Gameplay:
                if (HUDPanel != null) HUDPanel.SetActive(true);
                break;

            case GameState.Paused:
                // Khi Pause, thường vẫn giữ HUD và bật thêm Pause Panel
                if (HUDPanel != null) HUDPanel.SetActive(true);
                if (PausePanel != null) PausePanel.SetActive(true);
                break;

            case GameState.GameOver:
                if (GameOverPanel != null) GameOverPanel.SetActive(true);
                break;
        }
    }

    // Hàm tiện ích để tắt hết (kèm kiểm tra null an toàn)
    private void HideAllPanels()
    {
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        if (SettingPanel != null) SettingPanel.SetActive(false);
        if (TutorialPanel != null) TutorialPanel.SetActive(false);
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (PausePanel != null) PausePanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Hàm để đổi Character hiện tại
    /// </summary>
    /// <param name="characterIndex">Lưu trữ nhân vật được chọn</param>

    public void SelectedCharacter(int characterIndex)
    {
        CurrentCharacter = (Characters)characterIndex;
        Debug.Log($"[UIManager] Character hiện tại được đặt thành: {CurrentCharacter}");
    }
}