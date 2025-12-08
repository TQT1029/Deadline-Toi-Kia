using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào Canvas hoặc UI Controller trong Scene.
/// Dùng để gán sự kiện cho các Button (Settings, Tutorial, Switch Page).
/// </summary>
public class PanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;

    [Header("Selection Pages")]
    [SerializeField] private GameObject charactersPage;
    [SerializeField] private GameObject mapsPage;

    private void Start()
    {
        // Tự động tìm reference từ UIManager nếu chưa kéo thả
        if (UIManager.Instance == null) return;

        if (settingPanel == null) settingPanel = UIManager.Instance.SettingPanel;

        if (charactersPage == null) charactersPage = UIManager.Instance.CharactersPage;
        if (mapsPage == null) mapsPage = UIManager.Instance.MapsPage;
    }

    // --- Panel Logic ---
    public void OpenSettings() => TogglePanel(settingPanel, true);
    public void CloseSettings() => TogglePanel(settingPanel, false);

    private void TogglePanel(GameObject panel, bool isOpen)
    {
        if (panel == null) return;
        panel.SetActive(isOpen);

        // Pause time khi mở panel popup (trừ khi đang ở main menu)
        if (GameManager.Instance && GameManager.Instance.CurrentState != GameState.MainMenu)
        {
            if (isOpen) GameManager.Instance.ChangeState(GameState.Paused);
            else GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    // --- Selection Page Switch ---
    public void ShowCharactersPage()
    {
        if (mapsPage) mapsPage.SetActive(false);
        if (charactersPage) charactersPage.SetActive(true);
    }

    public void ShowMapsPage()
    {
        if (charactersPage) charactersPage.SetActive(false);
        if (mapsPage) mapsPage.SetActive(true);
    }
}