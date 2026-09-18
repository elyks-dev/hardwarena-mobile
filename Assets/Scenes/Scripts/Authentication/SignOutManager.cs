using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class SignOutManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject signOutPopup;

    [Header("Buttons")]
    [SerializeField] private Button signOutButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button confirmSignOutButton;

    [Header("Scene")]
    [SerializeField] private string loginScene = "LoginScene";

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Hide popup when HomeScene starts.
        if (signOutPopup != null)
            signOutPopup.SetActive(false);

        // Connect buttons.
        if (signOutButton != null)
            signOutButton.onClick.AddListener(OpenPopup);

        if (returnButton != null)
            returnButton.onClick.AddListener(ClosePopup);

        if (confirmSignOutButton != null)
            confirmSignOutButton.onClick.AddListener(SignOut);
    }

    // =========================================================
    // OPEN POPUP
    // =========================================================

    void OpenPopup()
    {
        if (signOutPopup != null)
            signOutPopup.SetActive(true);
    }

    // =========================================================
    // RETURN TO GAME
    // =========================================================

    void ClosePopup()
    {
        if (signOutPopup != null)
            signOutPopup.SetActive(false);
    }

    // =========================================================
    // SIGN OUT
    // =========================================================

    void SignOut()
    {
        auth.SignOut();

        if (signOutPopup != null)
            signOutPopup.SetActive(false);

        SceneManager.LoadScene(loginScene);
    }

    // =========================================================
    // CLEAN UP BUTTON LISTENERS
    // =========================================================

    void OnDestroy()
    {
        if (signOutButton != null)
            signOutButton.onClick.RemoveListener(OpenPopup);

        if (returnButton != null)
            returnButton.onClick.RemoveListener(ClosePopup);

        if (confirmSignOutButton != null)
            confirmSignOutButton.onClick.RemoveListener(SignOut);
    }
}