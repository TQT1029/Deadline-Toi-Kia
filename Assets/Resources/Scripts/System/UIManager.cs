using UnityEngine;

[DefaultExecutionOrder(-10)]
public class UIManager : Singleton<UIManager>
{
    [Header("Panels")]
    // Kéo thả các GameObject cha của UI vào đây trong Prefab SystemManagers
    public GameObject MainMenuPanel;
    public GameObject HUDPanel;
    public GameObject PausePanel;
    public GameObject GameOverPanel;

    private void OnEnable()
    {
        // Tự động lắng nghe sự kiện đổi State từ GameManager
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        // Tắt tất cả trước khi bật cái cần thiết
        HideAllPanels();

        switch (newState)
        {
            case GameState.MainMenu:
                if (MainMenuPanel) MainMenuPanel.SetActive(true);
                break;

            case GameState.Gameplay:
                if (HUDPanel) HUDPanel.SetActive(true);
                break;

            case GameState.Paused:
                // Giữ HUD hiển thị nếu muốn, hoặc tắt đi
                if (PausePanel) PausePanel.SetActive(true);
                break;

            case GameState.GameOver:
                if (GameOverPanel) GameOverPanel.SetActive(true);
                break;
        }
    }

    private void HideAllPanels()
    {
        if (MainMenuPanel) MainMenuPanel.SetActive(false);
        if (HUDPanel) HUDPanel.SetActive(false);
        if (PausePanel) PausePanel.SetActive(false);
        if (GameOverPanel) GameOverPanel.SetActive(false);
    }
}