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
    [Header("Main Menu UI References")]
    public GameObject MainMenuPanel;
    public GameObject SettingPanel;

    // Selection Scene
    [Header("Selection UI References")]
    public GameObject CharactersPage;
    public GameObject MapsPage;

    public Animator characterPreview;
    public Image characterChecklist;

    //Playing Scenes 
    [Header("In-Game UI References")]
    public Image MainInfo;
    [Space(10)]
    public TMP_Text DistanceText;
    public TMP_Text CoinText;
    public TMP_Text XPScoreText;

    public TMP_Text RankTitleText;
    public TMP_Text RankDetailText;
    [Space(10)]

    public GameObject ResultPanel;

    public GameObject[] Stars = new GameObject[5];
    public Animator AnimatorObj1;
    public Animator AnimatorObj2;
    public TMP_Text ResultDistanceText;
    public TMP_Text ResultXPScoreText;
    public TMP_Text ResultRankText;

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

        // 1. Reset references tùy theo Scene
        if (sName == GameConstants.SCENE_MAIN_MENU)
        {
            MainMenuPanel = FindObj("MainMenuPanel");
            SettingPanel = FindObj("SettingPanel");

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Menu);
            else
                HandleStateChanged(GameState.Menu);
        }
        else if (sName == GameConstants.SCENE_SELECTION)
        {
            SettingPanel = FindObj("SettingPanel");
            CharactersPage = FindObj("CharactersPage");
            MapsPage = FindObj("MapsPage");

            GameObject prevObj = FindObj("CharacterPreview");
            if (prevObj) characterPreview = prevObj.GetComponent<Animator>();

            var checkObj = FindObj("Checklist");
            if (checkObj) characterChecklist = checkObj.GetComponent<Image>();

            // Mặc định chọn nhân vật đầu tiên nếu chưa có
            if (ReferenceManager.Instance != null && ReferenceManager.Instance.CurrentSelectedProfile == null)
                SelectCharacterByIndex(0);

            HideAllPanels();
        }
        else
        {
            // Playing Scenes
            SettingPanel = FindObj("SettingPanel");

            var mainInfoObj = FindObj("MainInfo");
            if (mainInfoObj != null) MainInfo = mainInfoObj.GetComponent<Image>();

            var distObj = FindObj("DistanceText");
            if (distObj != null) DistanceText = distObj.GetComponent<TMP_Text>();

            var coinObj = FindObj("CoinText");
            if (coinObj != null) CoinText = coinObj.GetComponent<TMP_Text>();

            var xpObj = FindObj("XPScoreText");
            if (xpObj != null) XPScoreText = xpObj.GetComponent<TMP_Text>();

            var rankTitleObj = FindObj("RankingTitleText");
            if (rankTitleObj != null) RankTitleText = rankTitleObj.GetComponent<TMP_Text>();

            var rankDetailObj = FindObj("RankingDetailText");
            if (rankDetailObj != null) RankDetailText = rankDetailObj.GetComponent<TMP_Text>();

            ResultPanel = FindObj("ResultPanel");

            if (ResultPanel != null)
            {
                Stars[0] = ResultPanel.transform.Find("ResultZone/Stars/1")?.gameObject;
                Stars[1] = ResultPanel.transform.Find("ResultZone/Stars/2")?.gameObject;
                Stars[2] = ResultPanel.transform.Find("ResultZone/Stars/3")?.gameObject;
                Stars[3] = ResultPanel.transform.Find("ResultZone/Stars/4")?.gameObject;
                Stars[4] = ResultPanel.transform.Find("ResultZone/Stars/5")?.gameObject;

                ResultDistanceText = ResultPanel.transform.Find("ResultZone/ResultDistance")?.GetComponent<TMP_Text>();
                ResultXPScoreText = ResultPanel.transform.Find("ResultZone/ResultXPScore")?.GetComponent<TMP_Text>();
                ResultRankText = ResultPanel.transform.Find("ResultZone/ResultRank")?.GetComponent<TMP_Text>();
            }

            var bg1 = FindObj("BGObj1");
            if (bg1 != null) AnimatorObj1 = bg1.GetComponent<Animator>();

            var bg2 = FindObj("BGObj2");
            if (bg2 != null) AnimatorObj2 = bg2.GetComponent<Animator>();

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);
            else
                HandleStateChanged(GameState.Playing);
        }
    }

    private GameObject FindObj(string name)
    {
        // 1. Thử tìm nhanh object active
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;

        // 2. Tìm sâu trong tất cả Canvas (kể cả khi object đang bị inactive/tắt)
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            var allTransforms = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i] != null && allTransforms[i].name == name)
                {
                    return allTransforms[i].gameObject;
                }
            }
        }

        return null;
    }

    private void HandleStateChanged(GameState newState)
    {
        HideAllPanels();

        switch (newState)
        {
            case GameState.Menu:
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
        if (MainMenuPanel) MainMenuPanel.SetActive(false);
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
        if (ReferenceManager.Instance == null) return;
        var lib = ReferenceManager.Instance.AllCharacters;

        if (lib == null || index < 0 || index >= lib.Length) return;

        var profile = lib[index];
        ReferenceManager.Instance.CurrentSelectedProfile = profile;
        UpdatePreviewUI(profile);
        GameEvents.TriggerCharacterSelected(profile);

        Debug.Log($"[UIManager] Update Preview: {profile.name}");
    }

    private void UpdatePreviewUI(CharacterProfile data)
    {
        if (data == null) return;

        // 1. Tự tìm lại nếu chưa có reference
        if (characterPreview == null)
        {
            var prevObj = FindObj("CharacterPreview");
            if (prevObj != null) characterPreview = prevObj.GetComponent<Animator>();
        }

        if (characterChecklist == null)
        {
            var checkObj = FindObj("Checklist");
            if (checkObj != null) characterChecklist = checkObj.GetComponent<Image>();
        }

        // 2. Xử lý ANIMATOR cho nhân vật chính
        if (characterPreview != null && data.previewAction != null)
        {
            characterPreview.runtimeAnimatorController = data.previewAction;
            characterPreview.Rebind();
            characterPreview.Update(0f);
        }

        // 3. Xử lý check list (icon nhỏ)
        if (characterChecklist != null && data.checklistImage != null)
        {
            characterChecklist.sprite = data.checklistImage;
            characterChecklist.SetNativeSize();
        }
    }

    public void SelectMapByIndex(int index)
    {
        if (ReferenceManager.Instance == null) return;
        var lib = ReferenceManager.Instance.AllMaps;
        if (lib == null || index < 0 || index >= lib.Length) return;

        ReferenceManager.Instance.CurrentSelectedMap = lib[index];
        GameEvents.TriggerMapSelected(index);
        Debug.Log($"[UIManager] Map Selected: {lib[index].mapName}");
    }
}