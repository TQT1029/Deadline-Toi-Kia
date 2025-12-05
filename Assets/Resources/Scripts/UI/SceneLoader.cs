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
        // Vào Gameplay từ màn chọn map
        if (ReferenceManager.Instance.currentSelectedMap == null)
        {
            Debug.LogError("[SceneLoader] Chưa chọn Map nào!");
            return;
        }

        Time.timeScale = 1f;
        string mapScene = ReferenceManager.Instance.currentSelectedMap.targetSceneName;
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