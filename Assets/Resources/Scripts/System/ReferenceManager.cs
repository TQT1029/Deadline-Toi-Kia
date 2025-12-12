using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý các tham chiếu chung trong game.
/// </summary>

[DefaultExecutionOrder(-9)]
public class ReferenceManager : Singleton<ReferenceManager>
{
    [Header("Runtime References")]
    public Camera MainCamera;
    public Transform PlayerTransform;
    public Rigidbody2D PlayerRigidbody;

    public Transform SpawnTrans;
    public Transform RespawnTrans;

    [Header("Data Library")]
    [Tooltip("Kéo tất cả CharacterProfile vào đây")]
    public CharacterProfile[] AllCharacters;
    [Tooltip("Kéo tất cả MapProfile vào đây")]
    public MapProfile[] AllMaps;

    [Header("Current Session")]
    public CharacterProfile CurrentSelectedProfile;
    public MapProfile CurrentSelectedMap;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshRuntimeReferences();

        // Tìm lại Spawn và Respawn
        SpawnTrans = GameObject.FindGameObjectWithTag(GameConstants.TAG_SPAWNPOINT)?.transform;
        RespawnTrans = GameObject.FindGameObjectWithTag(GameConstants.TAG_RESPAWN)?.transform;

    }

    /// <summary>
    /// Tìm lại Camera và Player mỗi khi sang màn chơi mới
    /// </summary>
    public void RefreshRuntimeReferences()
    {
        MainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
        PlayerTransform = (playerObj != null) ? playerObj.transform : null;
        PlayerRigidbody = (PlayerTransform != null) ? PlayerTransform.GetComponent<Rigidbody2D>() : null;

        Debug.Log($"[ReferenceManager] Refreshed. Player found: {PlayerTransform != null}");
    }

}