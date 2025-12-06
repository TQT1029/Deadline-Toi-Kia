using UnityEngine;

[CreateAssetMenu(fileName = "NewMapProfile", menuName = "Data/Map Profile")]
public class MapProfile  : ScriptableObject
{
    public string mapName;
    [Tooltip("Tên Scene chính xác trong Build Settings")]
    public string targetSceneName;
    [Tooltip("Index dùng để xác định Skin nhân vật")]
    public int mapIndex;
}