using UnityEngine;
using UnityEngine.SceneManagement;



[DefaultExecutionOrder(-9)]
public class ReferenceManager : Singleton<ReferenceManager>
{
    [Header("Global References")]
    public Camera MainCamera;
    public Transform PlayerTransform;
    public CharactersData CharacterData;

    [Header("Character Data Library")]
    // Kéo 6 file CharacterProfile bạn vừa tạo vào list này theo đúng thứ tự Enum
    public CharactersData[] allCharacters;

    // Biến lưu nhân vật người chơi ĐÃ CHỌN để mang vào game
    public CharactersData currentSelectedProfile;

    [Header("Map Library")]
    public MapsData[] allMaps;
    // Biến lưu map người chơi ĐÃ CHỌN để mang vào game
    public MapsData  currentSelectedMap;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferences();

        LoadCharacterData();
    }

    /// <summary>
    /// Gọi hàm này khi Player spawn hoặc Scene mới load
    /// </summary>
    public void RefreshReferences()
    {
        MainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
        }
        else
        {
            // Reset nếu không tìm thấy (ví dụ ở Main Menu)
            PlayerTransform = null;
        }

        Debug.Log($"[ReferenceManager] Refs Refreshed. Player found: {PlayerTransform != null}");
    }

    private void LoadCharacterData()
    {
        Characters currentCharacter = UIManager.Instance.CurrentCharacter;

        string path = $"Data/Characters/{currentCharacter.ToString()}";

        CharacterData = Resources.Load<CharactersData>(path);
        if (CharacterData != null && PlayerTransform != null)
        {
            Debug.Log($"[ReferenceManager] Tải thành công dữ liệu cho {currentCharacter}");
        }
        else if (CharacterData == null)
        {
            Debug.LogError($"[ReferenceManager] Không có dữ liệu tại đường dẫn: {path}");
        }
    }
}