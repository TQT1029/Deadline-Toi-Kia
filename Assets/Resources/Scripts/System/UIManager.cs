using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class UIManager : Singleton<UIManager>
{
    [Header("References (Auto Found)")]
    public GameObject MainMenuPanel;
    public GameObject SettingPanel;
    public GameObject TutorialPanel;
    public GameObject CharactersPage;
    public GameObject MapsPage;

    public Image characterPreview;
    public Image characterChecklist;

    public GameObject HUDPanel;
    public GameObject PausePanel;
    public GameObject GameOverPanel;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sName = scene.name;

        // Reset references tùy theo Scene
        if (sName == GameConstants.SCENE_MAIN_MENU)
        {
            MainMenuPanel = FindObj("MainMenuPanel");
            SettingPanel = FindObj("SettingPanel");
            TutorialPanel = FindObj("TutorialPanel");
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
        else if (sName == GameConstants.SCENE_SELECTION)
        {
            SettingPanel = FindObj("SettingPanel");
            CharactersPage = FindObj("CharactersPage");
            MapsPage = FindObj("MapsPage");

            var prevObj = GameObject.Find("CharacterPreview");
            if (prevObj) characterPreview = prevObj.GetComponent<Image>();

            var checkObj = GameObject.Find("Checklist");
            if (checkObj) characterChecklist = checkObj.GetComponent<Image>();

            // Mặc định chọn nhân vật đầu tiên nếu chưa có
            if (ReferenceManager.Instance.currentSelectedProfile == null)
                SelectCharacterByIndex(0);
        }
        else
        {
            // Gameplay Scenes
            HUDPanel = FindObj("HUDPanel");
            PausePanel = FindObj("PausePanel");
            GameOverPanel = FindObj("GameOverPanel");
            GameManager.Instance.ChangeState(GameState.Gameplay);
        }

        HideAllPanels();
    }

    private GameObject FindObj(string name) => GameObject.Find(name);

    private void HandleStateChanged(GameState newState)
    {
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
                if (HUDPanel) HUDPanel.SetActive(true);
                if (PausePanel) PausePanel.SetActive(true);
                break;
            case GameState.GameOver:
                if (GameOverPanel) GameOverPanel.SetActive(true);
                break;
        }
    }

    private void HideAllPanels()
    {
        // Ẩn tất cả an toàn (Null check)
        if (MainMenuPanel) MainMenuPanel.SetActive(true);
        if (SettingPanel) SettingPanel.SetActive(false);
        if (TutorialPanel) TutorialPanel.SetActive(false);

        if (HUDPanel) HUDPanel.SetActive(false);
        if (PausePanel) PausePanel.SetActive(false);
        if (GameOverPanel) GameOverPanel.SetActive(false);

        // Page Selection xử lý riêng
        if (MapsPage) MapsPage.SetActive(false);
        if (CharactersPage && SceneManager.GetActiveScene().name == GameConstants.SCENE_SELECTION)
            CharactersPage.SetActive(true);
    }

    // --- SELECTION LOGIC ---

    public void SelectCharacterByIndex(int index)
    {
        var lib = ReferenceManager.Instance.allCharacters;
        if (lib == null || index < 0 || index >= lib.Length) return;

        var profile = lib[index];
        ReferenceManager.Instance.currentSelectedProfile = profile;
        UpdatePreviewUI(profile);
    }

    private void UpdatePreviewUI(CharacterProfile data)
    {
        if (characterPreview && data.previewImage)
        {
            characterPreview.sprite = data.previewImage;
            characterPreview.SetNativeSize();
        }
        if (characterChecklist && data.checklistImage)
        {
            characterChecklist.sprite = data.checklistImage;
            characterChecklist.SetNativeSize();
        }
    }

    public void SelectMapByIndex(int index)
    {
        var lib = ReferenceManager.Instance.allMaps;
        if (lib == null || index < 0 || index >= lib.Length) return;

        ReferenceManager.Instance.currentSelectedMap = lib[index];
        Debug.Log($"[UIManager] Map Selected: {lib[index].mapName}");
    }
}