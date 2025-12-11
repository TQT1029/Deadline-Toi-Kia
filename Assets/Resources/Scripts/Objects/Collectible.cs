using UnityEngine;
using DG.Tweening; // Bắt buộc

public enum ItemType
{
    CoinItem,
    DoubleXPItem,
    // Có thể thêm các loại vật phẩm khác ở đây
}

public class Collectible : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType type;
    public int scoreValue = 10; // Giá trị mặc định
    public GameObject collectEffect;

    // --- HÀM MỚI: Dùng để nạp dữ liệu từ MapController ---
    public void Init(int value)
    {
        this.scoreValue = value;
    }

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;
            GetComponent<Collider2D>().enabled = false;

            // Xử lý cộng điểm
            switch (type)
            {
                case ItemType.CoinItem: // Hoặc Coin
                    GameStatsController.Instance.CollectCoinItem(scoreValue);
                    break;
                case ItemType.DoubleXPItem:
                    GameStatsController.Instance.CollectDoubleXPItem();
                    break;
            }

            if (collectEffect != null)
                Instantiate(collectEffect, transform.position, Quaternion.identity);

            // Hiệu ứng biến mất
            transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
        }
    }
}