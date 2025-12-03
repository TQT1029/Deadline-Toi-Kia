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
    }

    /// <summary>
    /// Gọi hàm này khi Player respawn hoặc Scene mới load
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
}