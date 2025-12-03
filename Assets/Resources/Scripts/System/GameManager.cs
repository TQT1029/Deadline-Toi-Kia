using System;
using UnityEngine;

// Các trạng thái cơ bản mà game nào cũng có
public enum GameState
{
    MainMenu,
    Gameplay,
    Paused,
    Victory,
    GameOver
}

[DefaultExecutionOrder(-10)]
public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; }

    // Sự kiện để các Manager khác (UI, Audio) lắng nghe thay vì check liên tục
    public static event Action<GameState> OnStateChanged;

    protected override void OnAwake()
    {
        // Mặc định ban đầu, có thể đổi tùy logic game
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
            case GameState.Gameplay:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 1f; // Thường để 1f để chạy animation thua
                break;
        }

        Debug.Log($"[GameManager] State: {newState}");
        OnStateChanged?.Invoke(newState);
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting Game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}