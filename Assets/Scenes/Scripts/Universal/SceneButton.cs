using UnityEngine;

public class SceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        if (SceneButtonManager.Instance != null)
        {
            SceneButtonManager.Instance.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("SceneButtonManager not found!");
        }
    }

    public void ReloadScene()
    {
        SceneButtonManager.Instance?.ReloadScene();
    }

    public void NextScene()
    {
        SceneButtonManager.Instance?.NextScene();
    }

    public void PreviousScene()
    {
        SceneButtonManager.Instance?.PreviousScene();
    }

    public void QuitGame()
    {
        SceneButtonManager.Instance?.QuitGame();
    }
}