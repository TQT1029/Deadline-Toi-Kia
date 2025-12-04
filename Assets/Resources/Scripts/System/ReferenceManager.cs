using UnityEngine;
using UnityEngine.SceneManagement;



[DefaultExecutionOrder(-9)]
public class ReferenceManager : Singleton<ReferenceManager>
{
    [Header("Global References")]
    public Camera MainCamera;
    public Transform PlayerTransform;

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

        CharactersData characterData = Resources.Load<CharactersData>(path);
        if (characterData != null && PlayerTransform != null)
        {
            Debug.Log($"[ReferenceManager] Tải thành công dữ liệu cho {currentCharacter}");
        }
        else
        {
            Debug.LogError($"[ReferenceManager] Không có dữ liệu tại đường dẫn: {path}");
        }
    }
}