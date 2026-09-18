using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitLevelPopupManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject exitPopup;

    [Header("Buttons")]
    [SerializeField] private Button openPopupButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button exitButton;

    [Header("Scene")]
    [SerializeField] private string exitScene = "LevelsScene";

    void Start()
    {
        // Hide popup when scene starts.
        if (exitPopup != null)
            exitPopup.SetActive(false);

        // Button listeners.
        if (openPopupButton != null)
            openPopupButton.onClick.AddListener(OpenPopup);

        if (backButton != null)
            backButton.onClick.AddListener(ClosePopup);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitLevel);
    }

    // =========================================================
    // OPEN POPUP
    // =========================================================

    public void OpenPopup()
    {
        if (exitPopup != null)
            exitPopup.SetActive(true);
    }

    // =========================================================
    // CLOSE POPUP
    // =========================================================

    public void ClosePopup()
    {
        if (exitPopup != null)
            exitPopup.SetActive(false);
    }

    // =========================================================
    // EXIT LEVEL
    // =========================================================

    public void ExitLevel()
    {
        SceneManager.LoadScene(exitScene);
    }

    // =========================================================
    // CLEAN UP LISTENERS
    // =========================================================

    void OnDestroy()
    {
        if (openPopupButton != null)
            openPopupButton.onClick.RemoveListener(OpenPopup);

        if (backButton != null)
            backButton.onClick.RemoveListener(ClosePopup);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitLevel);
    }
}