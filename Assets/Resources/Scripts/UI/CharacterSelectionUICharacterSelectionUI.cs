using UnityEngine;

public class CharacterSelectionUI : MonoBehaviour
{
    public void SelectCharacter(int characterIndex)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectedCharacter(characterIndex); // Giả sử bên UIManager có hàm SetRole
            // Hoặc gọi: UIManager.Instance.SelectedRole = (RoleType)characterIndex;
        }
    }
}