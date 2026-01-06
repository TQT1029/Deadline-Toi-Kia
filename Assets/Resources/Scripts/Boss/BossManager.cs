using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance;
    private void Awake() => Instance = this;


    [Header("References")]
    public ObstacleBossController obstacleController; // Kéo script Controller vào đây
    public Transform bossVisual; // Hình ảnh Boss (để làm intro entry)

    [Header("Game Flow")]
    public float attackInterval = 4.0f; // Thời gian nghỉ giữa các đợt tấn công

    private bool isFighting = false;

    // Hàm này được GameController gọi khi chạy đủ quãng đường
    public void StartBossFight()
    {
        if (isFighting) return;

        // 1. Di chuyển Boss Visual vào màn hình (Intro)
        EnterBossSequence();
    }

    private void EnterBossSequence()
    {
        // Ví dụ: Boss bay từ trên xuống điểm giữa (0.5, 0.8)
        Vector3 startPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1.5f, 10));
        Vector3 endPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.8f, 10));

        bossVisual.position = startPos;
        bossVisual.DOMove(endPos, 2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            isFighting = true;
            StartCoroutine(CombatLoop());
        });
    }

    // Vòng lặp chiến đấu chính
    private IEnumerator CombatLoop()
    {
        while (isFighting)
        {
            // 2. Random chọn đòn tấn công
            ObstacleBossController.AttackPattern randomPattern = GetRandomPattern();

            // 3. Ra lệnh cho Controller thực hiện
            obstacleController.ExecuteAttack(randomPattern);

            // 4. Chờ đợt sau
            yield return new WaitForSeconds(attackInterval);
        }
    }

    private ObstacleBossController.AttackPattern GetRandomPattern()
    {
        // Lấy ngẫu nhiên 1 giá trị từ Enum
        var values = System.Enum.GetValues(typeof(ObstacleBossController.AttackPattern));
        return (ObstacleBossController.AttackPattern)values.GetValue(Random.Range(0, values.Length));
    }

    // Gọi hàm này khi Boss chết hoặc Game Over
    public void StopFight()
    {
        isFighting = false;
        StopAllCoroutines();
    }
}