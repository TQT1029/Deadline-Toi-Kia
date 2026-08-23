using UnityEngine;
using System.Collections.Generic;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("References")]
    public SelectionArrow selectionArrow;
    public List<Transform> characterTransforms;

    private int currentIndex = 0;

    private void Start()
    {
        // Chọn nhân vật mặc định (index 0) khi vào scene
        SelectCharacter(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentIndex > 0) SelectCharacter(currentIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (characterTransforms != null && currentIndex < characterTransforms.Count - 1)
                SelectCharacter(currentIndex + 1);
        }
    }

    public void SelectCharacter(int index)
    {
        if (characterTransforms != null && (index < 0 || index >= characterTransforms.Count)) return;
        currentIndex = index;

        // 1. Cập nhật Data qua ReferenceManager & GameEvents
        if (ReferenceManager.Instance != null)
        {
            ReferenceManager.Instance.SelectCharacter(index);
        }

        // 2. Fallback qua UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectCharacterByIndex(index);
        }

        // 3. Di chuyển mũi tên
        if (selectionArrow != null && characterTransforms != null && index >= 0 && index < characterTransforms.Count)
        {
            if (characterTransforms[index] != null)
            {
                selectionArrow.MoveTo(characterTransforms[index]);
            }
        }

        Debug.Log($"[CharacterSelectionUI] Character Selected: {index}");
    }
}