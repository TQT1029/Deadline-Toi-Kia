using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Data/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Information")]
    public string characterName = "";
    [TextArea] public string characterDescription = "";

    [Header("Visuals")]
    [Tooltip("Danh sách Sprite theo Map Index (Map 0 dùng Element 0...)")]
    public Sprite[] skinVariants;
    [Tooltip("Ảnh hiển thị lớn ở màn chọn tướng")]
    public Sprite previewImage;
    [Tooltip("Ảnh hiển thị nhỏ (icon)")]
    public Sprite checklistImage;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float strength = 15f;
}