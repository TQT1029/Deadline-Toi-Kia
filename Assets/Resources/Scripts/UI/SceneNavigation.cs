using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string selectedCharacterSceneName = "SelectionScene";
    [SerializeField] private string gameplaySceneName = "Map0";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void PlayBtn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(selectedCharacterSceneName);
    }

    public void GoGameplayBtn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ReturnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
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