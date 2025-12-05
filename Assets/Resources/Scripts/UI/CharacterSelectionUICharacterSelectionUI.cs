using UnityEngine;
using System.Collections.Generic;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("References")]
    public SelectionArrow selectionArrow;
    public List<Transform> characterTransforms;

    private void Start()
    {
        // Chọn nhân vật mặc định (index 0) khi vào scene
        SelectCharacter(0);
    }

    // Gán hàm này vào Buttons chọn nhân vật: 0, 1, 2...
    public void SelectCharacter(int index)
    {
        // 1. Cập nhật Data
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectCharacterByIndex(index);
        }

        // 2. Di chuyển mũi tên
        if (selectionArrow != null && index >= 0 && index < characterTransforms.Count)
        {
            selectionArrow.MoveTo(characterTransforms[index]);
        }
    }
}