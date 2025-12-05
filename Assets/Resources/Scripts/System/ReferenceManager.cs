using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9)]
public class ReferenceManager : Singleton<ReferenceManager>
{
    [Header("Runtime References")]
    public Camera MainCamera;
    public Transform PlayerTransform;

    [Header("Data Library")]
    [Tooltip("Kéo tất cả CharacterProfile vào đây")]
    public CharacterProfile[] allCharacters;
    [Tooltip("Kéo tất cả MapProfile vào đây")]
    public MapProfile[] allMaps;

    [Header("Current Session")]
    public CharacterProfile currentSelectedProfile;
    public MapProfile currentSelectedMap;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshRuntimeReferences();
    }

    /// <summary>
    /// Tìm lại Camera và Player mỗi khi sang màn chơi mới
    /// </summary>
    public void RefreshRuntimeReferences()
    {
        MainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
        PlayerTransform = (playerObj != null) ? playerObj.transform : null;

        Debug.Log($"[ReferenceManager] Refreshed. Player found: {PlayerTransform != null}");
    }
}