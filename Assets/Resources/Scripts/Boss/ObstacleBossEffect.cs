using UnityEngine;

public class ObstacleBossEffect : MonoBehaviour
{
    [SerializeField] private float force = 15f;
    [SerializeField] private Vector2 direction = new Vector2(-1, 1);
    [SerializeField] private int minAmountCoin = 5;
    [SerializeField] private int maxAmountCoin = 30;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Racer"))
        {
            Rigidbody2D rbCollision = collision.GetComponent<Rigidbody2D>();
            if (rbCollision == null) return;

            //NOTE: đang bị vô hiệu hóa ngay khi vừa chay do script BaseRunner và các class con
            rbCollision.linearVelocity = Vector2.zero;
            rbCollision.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            if (collision.CompareTag("Player"))
            {
                GameStatsController.Instance.HitObstacleBoss(minAmountCoin, maxAmountCoin);
            }

        }
    }
}
