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
        // 1. Tự tìm trong Canvas cục bộ trước (Decoupled local resolution)
        if (settingPanel == null)
            settingPanel = transform.Find("SettingPanel")?.gameObject ?? transform.parent?.Find("SettingPanel")?.gameObject;

        if (charactersPage == null)
            charactersPage = transform.Find("CharactersPage")?.gameObject ?? transform.parent?.Find("CharactersPage")?.gameObject;

        if (mapsPage == null)
            mapsPage = transform.Find("MapsPage")?.gameObject ?? transform.parent?.Find("MapsPage")?.gameObject;

        // 2. Fallback sang UIManager nếu chưa tìm thấy
        if (UIManager.Instance != null)
        {
            if (settingPanel == null) settingPanel = UIManager.Instance.SettingPanel;
            if (charactersPage == null) charactersPage = UIManager.Instance.CharactersPage;
            if (mapsPage == null) mapsPage = UIManager.Instance.MapsPage;
        }
    }

    // --- Panel Logic ---
    public void OpenSettings() => TogglePanel(settingPanel, true);
    public void CloseSettings() => TogglePanel(settingPanel, false);

    private void TogglePanel(GameObject panel, bool isOpen)
    {
        if (panel == null) return;
        panel.SetActive(isOpen);

        // Pause time khi mở panel popup (trừ khi đang ở main menu)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Menu)
        {
            Time.timeScale = isOpen ? 0f : 1f;
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