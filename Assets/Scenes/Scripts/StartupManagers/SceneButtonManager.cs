using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonManager : MonoBehaviour
{
    public static SceneButtonManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;

        if (current < SceneManager.sceneCountInBuildSettings - 1)
            SceneManager.LoadScene(current + 1);
    }

    public void PreviousScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;

        if (current > 0)
            SceneManager.LoadScene(current - 1);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}