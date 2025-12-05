using UnityEngine;
using System.Collections.Generic; // Để dùng List
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("References")]
    public SelectionArrow selectionArrow; // Kéo script mũi tên vào đây
    public List<Transform> characterTransforms; // Kéo tất cả Transform của các thẻ nhân vật vào đây theo đúng thứ tự (0, 1, 2...)
    

    private void Start()
    {
        // Mặc định chọn nhân vật đầu tiên khi vào game (nếu muốn)
        SelectCharacter(0);
    }

    // Hàm này được gọi từ các nút chọn nhân vật
    public void SelectCharacter(int index)
    {
        // Logic gửi data đi (giữ nguyên logic cũ của bạn)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectedCharacter(index);
        }

        // --- PHẦN MỚI: Di chuyển mũi tên ---
        if (index >= 0 && index < characterTransforms.Count)
        {
            // Gọi mũi tên bay đến vị trí nhân vật được chọn
            selectionArrow.MoveTo(characterTransforms[index]);
        }
    }


}