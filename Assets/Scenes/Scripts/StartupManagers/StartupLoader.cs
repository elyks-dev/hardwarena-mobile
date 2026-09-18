using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class StartupLoader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private string loginScene = "LoginScene";
    [SerializeField] private string homeScene = "HomeScene";
    [SerializeField] private float dotSpeed = 0.25f;

    private Coroutine dotsCoroutine;

    IEnumerator Start()
    {
        // ---------------------------------------------
        // INITIALIZING
        // ---------------------------------------------

        yield return RunStep(
            "Initializing...",
            0.5f
        );

        // ---------------------------------------------
        // CHECK FIREBASE
        // ---------------------------------------------

        yield return RunStep(
            "Checking Firebase"
        );

        while (!FirebaseInitializer.Instance.IsInitialized)
        {
            yield return null;
        }

        StopDots();

        // ---------------------------------------------
        // CONNECTING SERVICES
        // ---------------------------------------------

        yield return RunStep(
            "Connecting Services",
            0.6f
        );

        // ---------------------------------------------
        // CHECK USER SESSION
        // ---------------------------------------------

        yield return RunStep(
            "Checking User Session",
            0.6f
        );

        FirebaseUser currentUser =
            FirebaseAuth.DefaultInstance.CurrentUser;

        // ---------------------------------------------
        // PREPARING USER SESSION
        // ---------------------------------------------

        yield return RunStep(
            "Preparing User Session",
            0.6f
        );

        // ---------------------------------------------
        // LOADING ASSETS
        // ---------------------------------------------

        yield return RunStep(
            "Loading Assets",
            0.6f
        );

        // ---------------------------------------------
        // DECIDE WHERE TO GO
        // ---------------------------------------------

        if (currentUser != null)
        {
            // User is already signed in.

            yield return RunStep(
                "Loading Home",
                0.6f
            );

            StopDots();

            SceneManager.LoadScene(homeScene);
        }
        else
        {
            // No signed-in user.

            yield return RunStep(
                "Loading Login",
                0.6f
            );

            StopDots();

            SceneManager.LoadScene(loginScene);
        }
    }

    // =========================================================
    // RUN LOADING STEP
    // =========================================================

    IEnumerator RunStep(
        string message,
        float duration = -1f
    )
    {
        StartDots(message);

        if (duration >= 0f)
        {
            yield return new WaitForSeconds(duration);
        }
    }

    // =========================================================
    // START DOT ANIMATION
    // =========================================================

    void StartDots(string message)
    {
        StopDots();

        dotsCoroutine =
            StartCoroutine(
                AnimateDots(message)
            );
    }

    // =========================================================
    // STOP DOT ANIMATION
    // =========================================================

    void StopDots()
    {
        if (dotsCoroutine != null)
        {
            StopCoroutine(dotsCoroutine);
            dotsCoroutine = null;
        }
    }

    // =========================================================
    // ANIMATE LOADING DOTS
    // =========================================================

    IEnumerator AnimateDots(string message)
    {
        int dots = 0;

        while (true)
        {
            statusText.text =
                message + new string('.', dots);

            dots =
                (dots + 1) % 4;

            yield return new WaitForSeconds(dotSpeed);
        }
    }
}