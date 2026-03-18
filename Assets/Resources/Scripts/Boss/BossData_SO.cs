using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Animations;

[CreateAssetMenu(fileName = "NewBossData", menuName = "Boss System/Boss Data")]
public class BossDataSO : ScriptableObject
{
    [Header("Visuals")]
    public string bossName;
    public Sprite bossSprite; // Hình ảnh Boss (để hiện lên BossVisual)
    public AnimatorController bossAnimation;
    public bool flipXAnimator = false;
    // public RuntimeAnimatorController bossAnimator; // Nếu bạn dùng Animation thay vì Sprite tĩnh

    [Header("Combat Config")]
    // Mỗi Boss sẽ có danh sách các chiêu thức riêng mà nó được phép dùng
    public List<ObstacleBossController.AttackPattern> availablePatterns;

    [Header("Projecties")]
    public List<MoveObstacleBoss> projectiesObstacle;


    [Tooltip("Thời gian nghỉ tối thiểu giữa các đòn đánh")]
    public float minAttackInterval = 1.5f;
    [Tooltip("Thời gian nghỉ tối đa")]
    public float maxAttackInterval = 3.0f;
}