using UnityEngine;

[CreateAssetMenu(fileName = "NewDataCharacters", menuName = "Data/NewCharacters")]
public class CharactersData : ScriptableObject
{
    [Header("Character Info")]
    [Tooltip("Character name")] public string characterNames = "";
    [Tooltip("Character Description")] public string characterDescriptions = "";

    [Header("Character Stats")]
    public float characterHealth = 100f;
    public float characterEnergy = 10f;
    public float characterSpeed = 5f;
    public float characterStrength = 15f;
    public float characterMagnet = 8f;
}
