using UnityEngine;
using DG.Tweening; // Bắt buộc

public enum ItemType
{
    DocumentItem,
    DoubleXPItem,
    // Có thể thêm các loại vật phẩm khác ở đây
}

public class Collectible : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType type;
    public int scoreValue = 10;
    public GameObject collectEffect;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.InBack; // Hiệu ứng thu nhỏ giật lại

    private bool isCollected = false; // Cờ để tránh bị ăn 2 lần

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return; // Nếu đã ăn rồi thì bỏ qua

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            // 1. Tắt Collider ngay lập tức để không ai chạm vào được nữa
            GetComponent<Collider2D>().enabled = false;

            // 2. Xử lý logic cộng điểm
            switch (type)
            {
                case ItemType.DocumentItem:
                    GameStatsController.Instance.CollectLearnItem(scoreValue);
                    break;
                case ItemType.DoubleXPItem:
                    GameStatsController.Instance.CollectDoubleXPItem();
                    break;
            }

            // 3. Hiệu ứng nổ (Particle System)
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // 4. Hiệu ứng biến mất bằng DOTween
            // Thu nhỏ về 0, sau đó mới Destroy object
            transform.DOScale(Vector3.zero, animDuration)
                .SetEase(animEase)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                });
        }
    }
}