using UnityEngine;

public enum ItemType
{
    LearnItem,   // Sách, Laptop, Dụng cụ học tập...
    DoubleXPItem // Bình thuốc X2, Icon X2...
}

public class Collectible : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType type;
    public int scoreValue = 10; // Điểm cộng thêm (nếu là LearnItem)
    public GameObject collectEffect; // Hiệu ứng nổ khi ăn (nếu có)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Xử lý logic cộng điểm
            switch (type)
            {
                case ItemType.LearnItem:
                    GameStatsManager.Instance.CollectLearnItem(scoreValue);
                    break;

                case ItemType.DoubleXPItem:
                    GameStatsManager.Instance.CollectDoubleXPItem();
                    break;
            }

            // Hiệu ứng âm thanh/hình ảnh (Optional)
            if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);

            // Biến mất
            Destroy(gameObject);
        }
    }
}