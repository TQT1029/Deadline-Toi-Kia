using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneSelection()
    {
        // Vào màn chọn tướng
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Menu);
        SceneManager.LoadScene(GameConstants.SCENE_SELECTION);
    }

    public void EnterMap()
    {
        // Vào Playing từ màn chọn map
        if (ReferenceManager.Instance == null || ReferenceManager.Instance.CurrentSelectedMap == null)
        {
            Debug.LogWarning("[SceneLoader] Chưa chọn Map nào, mặc định chọn Map 0!");
            if (ReferenceManager.Instance != null && ReferenceManager.Instance.AllMaps != null && ReferenceManager.Instance.AllMaps.Length > 0)
            {
                ReferenceManager.Instance.SelectMap(0);
            }
            else
            {
                SceneManager.LoadScene("Map0");
                return;
            }
        }

        Time.timeScale = 1f;
        if (AudioManager.Instance != null && ReferenceManager.Instance.CurrentSelectedMap != null)
        {
            AudioManager.Instance.PlayMusic($"BGM_Map{ReferenceManager.Instance.CurrentSelectedMap.mapIndex}");
        }

        string mapScene = ReferenceManager.Instance.CurrentSelectedMap.mapName;
        SceneManager.LoadScene(mapScene);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Menu);
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