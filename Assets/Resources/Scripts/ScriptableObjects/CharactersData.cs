using UnityEngine;

[CreateAssetMenu(fileName = "NewDataCharacters", menuName = "Data/NewCharacters")]
public class CharactersData : ScriptableObject
{
    [Header("Character Info")]
    [Tooltip("Character name")] public string characterName = "";
    [Tooltip("Character Description")] public string characterDescriptions = "";
    [Tooltip("Character Sprite")] public Sprite[] characterSprite;
    [Tooltip("Character Preview")] public Sprite characterPreview;
    [Tooltip("Character Checklist")] public Sprite characterChecklist;

    [Header("Character Stats")]
    public float characterHealth = 100f;
    public float characterEnergy = 10f;
    public float characterSpeed = 5f;
    public float characterStrength = 15f;
    public float characterMagnet = 8f;
}
