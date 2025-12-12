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

    // Cờ kiểm tra xem đã khởi tạo xong chưa
    private bool _isInitialized = false;

    private void Start()
    {
        InitializeRacers();
    }

    // Hàm này public để các script khác (như Spawner) có thể gọi thủ công nếu cần
    public void InitializeRacers()
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

        // Chỉ coi là đã khởi tạo thành công nếu tìm thấy Player
        if (_playerTransform != null)
        {
            _isInitialized = true;
            TotalRacers = _racers.Count;
            Debug.Log($"RankingManager: Đã tìm thấy {_racers.Count} racers (Có Player).");
        }
    }
    private void Update()
    {
        // CƠ CHẾ TỰ SỬA LỖI:
        // Nếu chưa khởi tạo thành công (chưa thấy Player), hãy thử tìm lại liên tục
        if (!_isInitialized || _playerTransform == null)
        {
            InitializeRacers();

            // Nếu vẫn chưa tìm thấy sau khi thử lại -> Dừng update frame này
            if (_playerTransform == null) return;
        }

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