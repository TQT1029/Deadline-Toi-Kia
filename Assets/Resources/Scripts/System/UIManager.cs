using System.Linq;
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

public enum Maps
{
    Maps0,
    Maps1,
    Maps2,
    Maps3,
    Maps4,

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

    [Header("Selection UI")] 
    public GameObject CharactersPage;
    public GameObject MapsPage;
    public Image characterPreview;
    public Image characterChecklist;


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
        else if (scene.name == "SelectionScene")
        {
            SettingPanel = GameObject.Find("SettingPanel");
            characterPreview = GameObject.Find("CharacterPreview").GetComponent<Image>();
            characterChecklist = GameObject.Find("Checklist").GetComponent<Image>();

            CharactersPage = GameObject.Find("CharactersPage");
            MapsPage = GameObject.Find("MapsPage");
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
        //Panels
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        if (SettingPanel != null) SettingPanel.SetActive(false);
        if (TutorialPanel != null) TutorialPanel.SetActive(false);
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (PausePanel != null) PausePanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(false);

        //Pages
        if (CharactersPage != null) CharactersPage.SetActive(true);
        if (MapsPage != null) MapsPage.SetActive(false);
    }

    // --- Chọn nhân vật từ UI Selection ---

    /// <summary>
    /// Hàm này gọi khi bấm vào nút/thẻ nhân vật
    /// </summary>
    /// <param name="characterIndex">0 đến 5 tương ứng với Enum</param>
    public void SelectedCharacter(int characterIndex)
    {
        // 1. Kiểm tra index hợp lệ
        if (ReferenceManager.Instance.allCharacters == null ||
            characterIndex < 0 ||
            characterIndex >= ReferenceManager.Instance.allCharacters.Length)
        {
            Debug.LogError("[UIManager] Index nhân vật không hợp lệ hoặc chưa gán Data trong ReferenceManager!");
            return;
        }

        // 2. Lấy dữ liệu từ thư viện trong ReferenceManager
        CharactersData selectedData = ReferenceManager.Instance.allCharacters[characterIndex];

        // 3. Lưu vào biến toàn cục để Scene sau dùng (Spawn nhân vật)
        ReferenceManager.Instance.currentSelectedProfile = selectedData;

        // 4. Cập nhật hình ảnh Preview
        CharacterPreviewUpdate(selectedData.characterPreview, selectedData.characterChecklist);

        Debug.Log($"[UIManager] Đã chọn: {selectedData.characterName} (Index: {characterIndex})");
    }

    private void CharacterPreviewUpdate(Sprite newSprite, Sprite newChecklist)
    {
        if (characterPreview != null && newSprite != null)
        {
            characterPreview.sprite = newSprite;

            // Tùy chọn: SetNativeSize giúp ảnh không bị méo nếu tỉ lệ gốc khác khung hình
            characterPreview.SetNativeSize();
        }

        if (characterChecklist!=null && newChecklist != null)
        {
            characterChecklist.sprite = newChecklist;

            characterChecklist.SetNativeSize();
        }
    }

    //------- Chọn map từ UI Selection -------
    public void SelectedMap(int mapIndex)
    {
        // 1. Kiểm tra index hợp lệ
        if (ReferenceManager.Instance.allMaps == null ||
            mapIndex < 0 ||
            mapIndex >= ReferenceManager.Instance.allMaps.Length)
        {
            Debug.LogError("[UIManager] Index bản đồ không hợp lệ hoặc chưa gán Data trong ReferenceManager!");
            return;
        }
        // 2. Lấy dữ liệu từ thư viện trong ReferenceManager
        MapsData selectedMap = ReferenceManager.Instance.allMaps[mapIndex];
        // 3. Lưu vào biến toàn cục để Scene sau dùng (Load map)
        ReferenceManager.Instance.currentSelectedMap = selectedMap;
        Debug.Log($"[UIManager] Đã chọn bản đồ: {selectedMap.mapName} (Index: {mapIndex})");
    }
}
