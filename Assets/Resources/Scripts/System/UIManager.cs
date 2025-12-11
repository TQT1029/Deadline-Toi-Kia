using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý giao diện người dùng (UI) chung.
/// Được dùng để điều khiển các panel UI tùy theo trạng thái game.
/// </summary>

[DefaultExecutionOrder(-10)]
public class UIManager : Singleton<UIManager>
{
    [Header("References (Auto Found)")]
    // Main Menu & Settings
    public GameObject MainMenuPanel;
    public GameObject SettingPanel;

    // Selection Scene
    public GameObject CharactersPage;
    public GameObject MapsPage;

    public Animator characterPreview;
    public Image characterChecklist;

    //Playing Scenes 
    public Image MainInfo;

    public TMP_Text DistanceText;
    public TMP_Text CoinText;
    public TMP_Text XPScoreText;

    public GameObject ResultPanel;

    public GameObject[] Stars = new GameObject[5];
    public Animator AnimatorObj1;
    public Animator AnimatorObj2;
    public TMP_Text ResultDistanceText;
    public TMP_Text ResultCoinText;
    public TMP_Text ResultXPScoreText;

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
            // Playing Scenes
            SettingPanel = FindObj("SettingPanel");

            MainInfo = FindObj("MainInfo")?.GetComponent<Image>();

            DistanceText = FindObj("DistanceText")?.GetComponent<TMP_Text>();
            CoinText = FindObj("CoinText")?.GetComponent<TMP_Text>();
            XPScoreText = FindObj("XPScoreText")?.GetComponent<TMP_Text>();

            ResultPanel = FindObj("ResultPanel");

            Stars[0] = ResultPanel.transform.Find("ResultZone/Stars/1")?.gameObject;
            Stars[1] = ResultPanel.transform.Find("ResultZone/Stars/2")?.gameObject;
            Stars[2] = ResultPanel.transform.Find("ResultZone/Stars/3")?.gameObject;
            Stars[3] = ResultPanel.transform.Find("ResultZone/Stars/4")?.gameObject;
            Stars[4] = ResultPanel.transform.Find("ResultZone/Stars/5")?.gameObject;

            AnimatorObj1 = FindObj("BGObj1")?.GetComponent<Animator>();
            AnimatorObj2 = FindObj("BGObj2")?.GetComponent<Animator>();

            ResultDistanceText = ResultPanel.transform.Find("ResultZone/ResultDistance")?.GetComponent<TMP_Text>();
            ResultCoinText = ResultPanel.transform.Find("ResultZone/ResultDocuments")?.GetComponent<TMP_Text>();
            ResultXPScoreText = ResultPanel.transform.Find("ResultZone/ResultXP")?.GetComponent<TMP_Text>();

            GameManager.Instance.ChangeState(GameState.Playing);
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
            case GameState.Playing:
                break;
            case GameState.Paused:
                if (SettingPanel) SettingPanel.SetActive(true);
                break;
            case GameState.Victory:
                if (ResultPanel) ResultPanel.SetActive(true);
                break;
        }
    }

    private void HideAllPanels()
    {
        // Ẩn tất cả an toàn (Null check)
        if (MainMenuPanel) MainMenuPanel.SetActive(true);
        if (SettingPanel) SettingPanel.SetActive(false);

        if (ResultPanel) ResultPanel.SetActive(false);

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