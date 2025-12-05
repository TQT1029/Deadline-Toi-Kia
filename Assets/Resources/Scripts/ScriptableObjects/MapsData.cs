using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "newMapsData", menuName = "Data/NewMapsData")]
public class MapsData : ScriptableObject
{
    public string mapName;
    public string mapScene;
}
