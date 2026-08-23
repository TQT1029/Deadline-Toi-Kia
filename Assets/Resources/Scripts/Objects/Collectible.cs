using UnityEngine;
using DG.Tweening;

public enum ItemType
{
    CoinItem,
    DoubleXPItem
}

public class Collectible : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType type;
    public int scoreValue = 10;

    private bool isCollected = false;

    public void Init(int value)
    {
        this.scoreValue = value;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag(GameConstants.TAG_PLAYER))
        {
            isCollected = true;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("CollectItem");
            }

            if (type == ItemType.CoinItem && GameStatsController.Instance != null)
            {
                GameStatsController.Instance.CollectCoinItem(scoreValue);
            }

            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
        }
        else if (collision.CompareTag(GameConstants.TAG_BOT) || collision.CompareTag("Obstacle") || collision.CompareTag("MiniPlatform"))
        {
            isCollected = true;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
        }
    }

    private void OnDisable()
    {
        transform.DOKill();
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}