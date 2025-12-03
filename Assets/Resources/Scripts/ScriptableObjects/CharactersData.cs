using UnityEngine;

[CreateAssetMenu(fileName = "NewDataCharacters", menuName = "Data/NewCharacters")]
public class CharactersData : ScriptableObject
{
    public string characterNames = "";
    public string characterDescriptions = "";

    public float characterHealth = 100f;
    public float characterEnergy = 10f;
    public float characterSpeed = 5f;
    public float characterStrength = 15f;
    public float characterMagnet = 8f;
}
