using UnityEngine;

public class WinPoint : MonoBehaviour
{
   
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Gọi hàm xử lý khi người chơi chạm vào WinPoint
            GameManager.Instance.ChangeState(GameState.Victory);
        }
    }
}
