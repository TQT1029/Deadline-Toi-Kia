using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RankingManager : Singleton<RankingManager>
{
    // Danh sách chứa Transform của tất cả vận động viên (Player + Bots)
    private List<Transform> _racers = new List<Transform>();
    private Transform _playerTransform;

    // Cache giá trị để HUD đọc
    public int CurrentRank { get; private set; } = 1;
    public int TotalRacers { get; private set; } = 1;

    private void Start()
    {
        InitializeRacers();
    }

    private void InitializeRacers()
    {
        _racers.Clear();

        // 1. Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _racers.Add(_playerTransform);
        }

        // 2. Tìm tất cả Bots
        GameObject[] bots = GameObject.FindGameObjectsWithTag(GameConstants.TAG_BOT);
        foreach (var bot in bots)
        {
            _racers.Add(bot.transform);
        }

        TotalRacers = _racers.Count;
    }

    private void Update()
    {
        if (_playerTransform == null || _racers.Count == 0) return;

        CalculateRanking();
    }

    private void CalculateRanking()
    {
        // Loại bỏ các object đã bị hủy (nếu Bot bị rơi xuống vực chết)
        _racers.RemoveAll(item => item == null);
        TotalRacers = _racers.Count;

        // Sắp xếp danh sách: Ai có X lớn hơn thì đứng trước (Giảm dần)
        _racers.Sort((a, b) => b.position.x.CompareTo(a.position.x));

        // Tìm vị trí của Player trong danh sách đã sắp xếp
        // Index bắt đầu từ 0 nên Rank phải +1
        int playerIndex = _racers.IndexOf(_playerTransform);
        CurrentRank = playerIndex + 1;
    }
}