using System;
using UnityEngine;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Victory,
}

/// <summary>
/// Singleton quản lý trạng thái game chung.
/// Được dùng để gọi các hàm toàn cục liên quan đến trạng thái game.
/// </summary>

[DefaultExecutionOrder(-10)]
public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; }
    public static event Action<GameState> OnStateChanged;

    protected override void OnAwake()
    {
        // Mặc định ban đầu
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // Tự động xử lý TimeScale
        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
            case GameState.Playing:
                GameStatsController.Instance.StartMap();
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Victory:
                GameStatsController.Instance.FinishLevel();
                Time.timeScale = 0f;
                break;

            default:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log($"[GameManager] State: {newState}");
        OnStateChanged?.Invoke(newState);
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}