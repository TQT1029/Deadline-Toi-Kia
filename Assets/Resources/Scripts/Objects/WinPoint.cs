using UnityEngine;


public class WinPoint : MonoBehaviour
{

    [SerializeField] private float timeOpenWin = 120f; // Thời gian mở Win Point (Giây)
    [SerializeField] private float spendTime = 0f;

    [SerializeField] private bool isOpenWin = false;
    private void Update()
    {
        if (ReferenceManager.Instance.PlayerRigidbody.linearVelocityX >= 4.5f)
        {
            spendTime += Time.deltaTime;
            if (spendTime >= timeOpenWin)
            {
                isOpenWin = true;
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isOpenWin)
                // Gọi hàm xử lý khi người chơi chạm vào WinPoint
                GameManager.Instance.ChangeState(GameState.Victory);
            else
            {
                ReferenceManager.Instance.SpawnTrans.position = new Vector3(other.transform.position.x, ReferenceManager.Instance.SpawnTrans.position.y, 0);
                FindFirstObjectByType<MapController>().GenerateLevel();
            }
        }
    }
}
