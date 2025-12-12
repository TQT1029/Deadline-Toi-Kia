using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void PlayGame()
    {
        // Vào màn chọn tướng
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameConstants.SCENE_SELECTION);
    }

    public void EnterMap()
    {
        // Vào Playing từ màn chọn map
        if (ReferenceManager.Instance.CurrentSelectedMap == null)
        {
            Debug.LogError("[SceneLoader] Chưa chọn Map nào!");
            return;
        }

        Time.timeScale = 1f;
        AudioManager.Instance.PlayMusic($"BGM_Map{ReferenceManager.Instance.CurrentSelectedMap.mapIndex}");
        string mapScene = ReferenceManager.Instance.CurrentSelectedMap.mapName;
        SceneManager.LoadScene(mapScene);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameConstants.SCENE_MAIN_MENU);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}