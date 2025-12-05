using UnityEngine;
using DG.Tweening; // Bắt buộc phải có namespace này

public class SelectionArrow : MonoBehaviour
{
    [Header("Cài đặt chung")]
    [Tooltip("Khoảng cách từ tâm nhân vật xuống vị trí mũi tên")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    [Header("Animation Settings")]
    [Tooltip("Thời gian mũi tên bay đến mục tiêu")]
    public float moveDuration = 0.4f;

    [Tooltip("Kiểu chuyển động khi bay (OutBack tạo cảm giác quán tính/nảy)")]
    public Ease moveEase = Ease.OutBack;

    [Header("Idle Bobbing (Nhún nhảy)")]
    [Tooltip("Khoảng cách nhún lên xuống")]
    public float bobDistance = 0.2f;

    [Tooltip("Thời gian của 1 nhịp nhún")]
    public float bobDuration = 0.5f;

    private Tween bobTween; // Lưu trữ tween nhún để quản lý

    // Hàm này sẽ được gọi từ script chọn nhân vật của bạn
    public void MoveTo(Transform targetCharacter)
    {
        // 1. Dừng mọi animation đang chạy trên mũi tên này (để tránh xung đột)
        transform.DOKill();

        // 2. Tính toán vị trí đích (Vị trí nhân vật + khoảng cách offset)
        Vector3 targetPos = targetCharacter.position + offset;

        // 3. Thực hiện di chuyển
        transform.DOMove(targetPos, moveDuration)
            .SetEase(moveEase) // Hiệu ứng nảy khi đến nơi
            .OnComplete(() =>
            {
                // 4. Khi bay đến nơi xong thì bắt đầu nhún nhảy
                StartBobbing(targetPos);
            });
    }

    private void StartBobbing(Vector3 basePosition)
    {
        // Đảm bảo vị trí chuẩn trước khi nhún
        transform.position = basePosition;

        // Tạo chuyển động đi xuống (hoặc lên) rồi lặp lại
        // SetLoops(-1, LoopType.Yoyo): Lặp vô hạn theo kiểu Yoyo (đi đi về về)
        bobTween = transform.DOMoveY(basePosition.y - bobDistance, bobDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine); // InOutSine giúp chuyển động mềm mại như sóng
    }

    // Nếu object bị tắt, nhớ kill tween để tránh lỗi
    private void OnDisable()
    {
        transform.DOKill();
    }
}