using UnityEngine;

[CreateAssetMenu(fileName = "NewMapProfile", menuName = "Data/Map Profile")]
public class MapProfile  : ScriptableObject
{
    public string mapName;
    [Tooltip("Âm thanh nền của map")]
    public string idBGM;
    [Tooltip("Index dùng để xác định map")]
    public int mapIndex;
}