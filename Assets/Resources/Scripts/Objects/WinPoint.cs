using UnityEngine;

public class WinPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("WIN!");
            // Gọi GameManager chuyển trạng thái thắng
            GameManager.Instance.ChangeState(GameState.Victory);

            // Dừng thời gian lại
            Time.timeScale = 0f;
        }
    }
}