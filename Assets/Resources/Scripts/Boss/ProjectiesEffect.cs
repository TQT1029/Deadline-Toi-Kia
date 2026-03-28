using UnityEngine;

public class ProjectiesEffect : MonoBehaviour
{
    [SerializeField] private float force = 15f;
    [SerializeField, Tooltip("Direction Knockback")] private Vector2 direction = new Vector2(-1, 1);
    [SerializeField] private float stunDuration = 0.3f;
    [SerializeField] private int minAmountCoin = 5;
    [SerializeField] private int maxAmountCoin = 30;

    [SerializeField] private GameObject hitEffectPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Racer"))
        {
            Rigidbody2D rbCollision = collision.GetComponent<Rigidbody2D>();
            if (rbCollision == null) return;


            var runner = collision.GetComponent<BaseRunner>();

            if (runner != null)
            {
                runner.ApplyKnockback(direction.normalized, force, stunDuration);
            }

            if (collision.CompareTag("Player"))
            {
                GameStatsController.Instance.HitObstacleBoss(minAmountCoin, maxAmountCoin);
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, collision.transform.position, Quaternion.identity, collision.transform);
                }
            }

        }
    }
}
