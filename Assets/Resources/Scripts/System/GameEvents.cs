using System;
using UnityEngine;

public static class GameEvents
{
    // --- Gameplay & Stats Events ---
    public static event Action<float, int> OnStatsUpdated; // (distance, coins)
    public static event Action<int, int> OnRankingUpdated; // (currentRank, totalRacers)
    public static event Action<int, float, int, int> OnLevelFinished; // (stars, distance, totalScore, finalRank)
    public static event Action OnLevelStarted;

    // --- State & Navigation Events ---
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<CharacterProfile> OnCharacterSelected;
    public static event Action<int> OnMapSelected;

    // --- Invoker Helpers ---
    public static void TriggerStatsUpdated(float distance, int coins)
    {
        OnStatsUpdated?.Invoke(distance, coins);
    }

    public static void TriggerRankingUpdated(int currentRank, int totalRacers)
    {
        OnRankingUpdated?.Invoke(currentRank, totalRacers);
    }

    public static void TriggerLevelFinished(int stars, float distance, int totalScore, int finalRank)
    {
        OnLevelFinished?.Invoke(stars, distance, totalScore, finalRank);
    }

    public static void TriggerLevelStarted()
    {
        OnLevelStarted?.Invoke();
    }

    public static void TriggerGameStateChanged(GameState newState)
    {
        OnGameStateChanged?.Invoke(newState);
    }

    public static void TriggerCharacterSelected(CharacterProfile profile)
    {
        OnCharacterSelected?.Invoke(profile);
    }

    public static void TriggerMapSelected(int mapIndex)
    {
        OnMapSelected?.Invoke(mapIndex);
    }
}
