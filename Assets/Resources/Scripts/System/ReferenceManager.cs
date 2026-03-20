using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý các tham chiếu chung trong game.
/// </summary>

[DefaultExecutionOrder(-9)]
public class ReferenceManager : Singleton<ReferenceManager>
{
    [Header("Runtime References")]
    public Camera MainCamera { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public Rigidbody2D PlayerRigidbody { get; private set; }

    public Transform SpawnTrans { get; private set; }

    public BaseRunner[] Racers { get; private set; }

    [Header("Data Library")]
    [Tooltip("Kéo tất cả CharacterProfile vào đây")]
    [field: SerializeField] public CharacterProfile[] AllCharacters { get; private set; }
    [Tooltip("Kéo tất cả MapProfile vào đây")]
    [field: SerializeField] public MapProfile[] AllMaps { get; private set; }

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

    }

    /// <summary>
    /// Tìm lại các tham chiếu runtime sau khi load scene mới, đảm bảo luôn có tham chiếu chính xác đến Player và MainCamera.
    /// </summary>
    public void RefreshRuntimeReferences()
    {
        MainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
        PlayerTransform = (playerObj != null) ? playerObj.transform : null;
        PlayerRigidbody = (PlayerTransform != null) ? PlayerTransform.GetComponent<Rigidbody2D>() : null;
        Racers = Object.FindObjectsByType<BaseRunner>(FindObjectsSortMode.None);
        

        Debug.Log($"[ReferenceManager] Refreshed. Player found: {PlayerTransform != null}");
        Debug.Log($"[ReferenceManager] Refreshed. Runner found: {Racers.Length}");
    }

}