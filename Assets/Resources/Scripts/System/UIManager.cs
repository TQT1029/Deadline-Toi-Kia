using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class UIManager : Singleton<UIManager>
{
    [Header("References (Auto Found)")]
    // Main Menu & Settings
    public GameObject MainMenuPanel;
    public GameObject SettingPanel;
    public GameObject TutorialPanel;

    // Selection Scene
    public GameObject CharactersPage;
    public GameObject MapsPage;

    public Animator characterPreview;
    public Image characterChecklist;

    //Gameplay Scenes 
    public TMP_Text DistanceText;
    public TMP_Text LearnScoreText;
    public TMP_Text XPScoreText;

    public GameObject WinPanel;

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
            if (prevObj) characterPreview = prevObj.GetComponent<Animator>();

            var checkObj = GameObject.Find("Checklist");
            if (checkObj) characterChecklist = checkObj.GetComponent<Image>();

            // Mặc định chọn nhân vật đầu tiên nếu chưa có
            if (ReferenceManager.Instance.currentSelectedProfile == null)
                SelectCharacterByIndex(0);
        }
        else
        {
            // Gameplay Scenes
            DistanceText = FindObj("DistanceText")?.GetComponent<TMP_Text>();
            LearnScoreText = FindObj("LearnScoreText")?.GetComponent<TMP_Text>();
            XPScoreText = FindObj("XPScoreText")?.GetComponent<TMP_Text>();

            WinPanel = FindObj("WinPanel");
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
                break;
            case GameState.Paused:
                if (SettingPanel) SettingPanel.SetActive(true);
                break;
            case GameState.Victory:
                if (WinPanel) WinPanel.SetActive(true);
                break;
        }
    }

    private void HideAllPanels()
    {
        // Ẩn tất cả an toàn (Null check)
        if (MainMenuPanel) MainMenuPanel.SetActive(true);
        if (SettingPanel) SettingPanel.SetActive(false);
        if (TutorialPanel) TutorialPanel.SetActive(false);

        if (WinPanel) WinPanel.SetActive(false);

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
        // 1. Xử lý ANIMATOR cho nhân vật chính
        if (characterPreview != null && data.previewAction != null)
        {
            // Gán Animator Controller mới
            characterPreview.runtimeAnimatorController = data.previewAction;

            // Reset lại Animator về trạng thái đầu (tránh bị kẹt ở animation cũ)
            characterPreview.Rebind();
            characterPreview.Update(0f);

            // [Tùy chọn] Set Native Size cho Image chứa Animator
            // Vì Animator component không có hàm SetNativeSize, ta phải lấy Image cùng cấp
            Image previewImage = characterPreview.GetComponent<Image>();
            if (previewImage != null)
            {
                // Lưu ý: Nếu Animation thay đổi kích thước liên tục, việc này có thể gây giật hình
                // Nên cân nhắc chỉ dùng nếu các nhân vật có kích thước khác hẳn nhau
                previewImage.SetNativeSize();
            }
        }

        // 2. Xử lý check list (icon nhỏ)
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