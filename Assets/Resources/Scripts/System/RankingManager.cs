using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RankingManager : Singleton<RankingManager>
{
    private List<Transform> _botTransforms = new List<Transform>();
    private Transform _playerTransform;

    public int CurrentRank { get; private set; } = 1;
    public int TotalRacers { get; private set; } = 1;

    private bool _isInGameplayScene = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sName = scene.name;
        if (sName == GameConstants.SCENE_MAIN_MENU || sName == GameConstants.SCENE_SELECTION)
        {
            _isInGameplayScene = false;
            _playerTransform = null;
            _botTransforms.Clear();
            CurrentRank = 1;
            TotalRacers = 1;
        }
        else
        {
            _isInGameplayScene = true;
            InitializeRacers();
        }
    }

    public void InitializeRacers()
    {
        _botTransforms.Clear();
        _playerTransform = null;

        // 1. Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }

        // 2. Tìm tất cả Bots
        GameObject[] bots = GameObject.FindGameObjectsWithTag(GameConstants.TAG_BOT);
        if (bots != null)
        {
            for (int i = 0; i < bots.Length; i++)
            {
                if (bots[i] != null) _botTransforms.Add(bots[i].transform);
            }
        }

        TotalRacers = (_playerTransform != null ? 1 : 0) + _botTransforms.Count;
        CurrentRank = 1;
    }

    private void Update()
    {
        if (!_isInGameplayScene) return;

        if (_playerTransform == null)
        {
            // Thử tìm lại 1 lần nếu chưa có
            GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
            if (playerObj != null) _playerTransform = playerObj.transform;
            if (_playerTransform == null) return;
        }

        CalculateRanking();
    }

    private int _lastRank = -1;
    private int _lastTotalRacers = -1;

    private void CalculateRanking()
    {
        if (_playerTransform == null) return;

        float playerX = _playerTransform.position.x;
        int rank = 1;
        int activeRacers = 1;

        for (int i = _botTransforms.Count - 1; i >= 0; i--)
        {
            Transform bot = _botTransforms[i];
            if (bot == null)
            {
                _botTransforms.RemoveAt(i);
                continue;
            }

            activeRacers++;
            if (bot.position.x > playerX)
            {
                rank++;
            }
        }

        CurrentRank = rank;
        TotalRacers = activeRacers;

        if (CurrentRank != _lastRank || TotalRacers != _lastTotalRacers)
        {
            _lastRank = CurrentRank;
            _lastTotalRacers = TotalRacers;
            GameEvents.TriggerRankingUpdated(CurrentRank, TotalRacers);
        }
    }
}