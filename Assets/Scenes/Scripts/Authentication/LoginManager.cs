
using System.Collections;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class LoginManager : MonoBehaviour
{
    private const string FailedAttemptsKey = "HW_FailedAttempts";
    private const string LoginLockoutEndKey = "HW_LoginLockoutEnd";

    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Buttons")]
    public Button showPasswordButton;
    public Button loginButton;

    [Header("Password Eye Icons")]
    public Image passwordEyeImage;
    public Sprite showPasswordIcon;
    public Sprite hidePasswordIcon;

    [Header("Login Attempt Protection")]
    public int maxFailedAttempts = 5;
    public float lockoutDuration = 30f;
    public TMP_Text lockoutCountdownText;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private bool passwordVisible = false;
    private int failedAttempts = 0;
    private bool isLockedOut = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Password hidden by default.
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        passwordEyeImage.sprite = showPasswordIcon;
        lockoutCountdownText.gameObject.SetActive(false);

        loginButton.onClick.AddListener(Login);
        showPasswordButton.onClick.AddListener(TogglePassword);

        // Press Enter to Login.
        passwordInput.onSubmit.AddListener(delegate { Login(); });

        RestoreLoginLockout();
    }

    // =========================================================
    // SHOW / HIDE PASSWORD
    // =========================================================

    void TogglePassword()
    {
        passwordVisible = !passwordVisible;

        passwordInput.contentType = passwordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();

        passwordEyeImage.sprite = passwordVisible
            ? hidePasswordIcon
            : showPasswordIcon;
    }

    // =========================================================
    // EMAIL VALIDATION
    // =========================================================

    bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }

    // =========================================================
    // LOGIN
    // =========================================================

    void Login()
    {
        if (isLockedOut)
        {
            ShowError(
                "TOO MANY LOGIN ATTEMPTS",
                "You have reached the maximum number of failed login attempts.\n\nPlease wait 30 seconds before trying to log in again."
            );
            return;
        }

        string email = emailInput.text.Trim().ToLower();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError(
                "INCOMPLETE FORM",
                "Please enter your email and password."
            );
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError(
                "INVALID EMAIL",
                "Please enter a valid email address."
            );
            return;
        }

        loginButton.interactable = false;

        // STEP 1: Authenticate before reading the user's own profile.
        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                loginButton.interactable = true;

                if (task.IsCanceled || task.IsFaulted)
                {
                    if (task.IsFaulted)
                    {
                        bool lockoutStarted = RegisterFailedAttempt();

                        if (!lockoutStarted)
                        {
                            HandleLoginError(task.Exception);
                        }
                    }
                    else
                    {
                        ShowError(
                            "LOGIN CANCELLED",
                            "The login attempt was cancelled."
                        );
                    }

                    return;
                }

                failedAttempts = 0;

                FirebaseUser user = task.Result.User;

                // STEP 2: Read only the authenticated user's profile.
                db.Collection("users")
                    .Document(user.UserId)
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.IsCanceled || profileTask.IsFaulted)
                        {
                            auth.SignOut();

                            ShowError(
                                "SERVICE UNAVAILABLE",
                                "Unable to verify your account. Please try again later."
                            );
                            return;
                        }

                        DocumentSnapshot userDoc = profileTask.Result;

                        if (!userDoc.Exists)
                        {
                            auth.SignOut();

                            ShowError(
                                "ACCOUNT NOT FOUND",
                                "No account exists with this email address."
                            );
                            return;
                        }

                        string role = userDoc.GetValue<string>("role").ToLower();

                        // Block admin/instructor accounts.
                        if (role == "admin" || role == "instructor")
                        {
                            auth.SignOut();

                            ShowError(
                                "ACCESS DENIED",
                                "Only students can log in."
                            );
                            return;
                        }

                        Debug.Log("Student login successful: " + user.Email);

                        LoadUserAndContinue(user.UserId);
                    });
            });
    }

    // =========================================================
    // FAILED LOGIN ATTEMPTS
    // =========================================================

    bool RegisterFailedAttempt()
    {
        failedAttempts++;
        PlayerPrefs.SetInt(FailedAttemptsKey, failedAttempts);
        PlayerPrefs.Save();

        if (failedAttempts >= maxFailedAttempts && !isLockedOut)
        {
            DateTime lockoutEnd = DateTime.UtcNow.AddSeconds(lockoutDuration);
            PlayerPrefs.SetString(LoginLockoutEndKey, lockoutEnd.Ticks.ToString());
            PlayerPrefs.Save();

            StartCoroutine(LoginCooldown(lockoutDuration, true));
            return true;
        }

        return false;
    }

    IEnumerator LoginCooldown(float duration, bool saveLockout)
    {
        isLockedOut = true;
        loginButton.interactable = false;
        lockoutCountdownText.gameObject.SetActive(true);

        if (saveLockout)
        {
            AuthPopupManager.Instance.ShowPopup(
                "TOO MANY LOGIN ATTEMPTS",
                "You have reached the maximum number of failed login attempts. Please wait 30 seconds before trying to log in again.",
                "OK"
            );
        }

        float remainingTime = duration;

        while (remainingTime > 0f)
        {
            lockoutCountdownText.text =
                "Try again in " + Mathf.CeilToInt(remainingTime) + "s.";

            remainingTime -= Time.deltaTime;
            yield return null;
        }

        failedAttempts = 0;
        isLockedOut = false;
        loginButton.interactable = true;
        lockoutCountdownText.gameObject.SetActive(false);

        PlayerPrefs.DeleteKey(FailedAttemptsKey);
        PlayerPrefs.DeleteKey(LoginLockoutEndKey);
        PlayerPrefs.Save();
    }

    void RestoreLoginLockout()
    {
        failedAttempts = PlayerPrefs.GetInt(FailedAttemptsKey, 0);

        if (!PlayerPrefs.HasKey(LoginLockoutEndKey))
        {
            return;
        }

        if (!long.TryParse(
                PlayerPrefs.GetString(LoginLockoutEndKey),
                out long lockoutEndTicks))
        {
            ClearLoginLockoutState();
            return;
        }

        TimeSpan remaining =
            new DateTime(lockoutEndTicks, DateTimeKind.Utc) - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            ClearLoginLockoutState();
            return;
        }

        StartCoroutine(
            LoginCooldown((float)remaining.TotalSeconds, false)
        );
    }

    void ClearLoginLockoutState()
    {
        failedAttempts = 0;
        isLockedOut = false;
        loginButton.interactable = true;
        lockoutCountdownText.gameObject.SetActive(false);

        PlayerPrefs.DeleteKey(FailedAttemptsKey);
        PlayerPrefs.DeleteKey(LoginLockoutEndKey);
        PlayerPrefs.Save();
    }

    // =========================================================
    // LOGIN ERROR HANDLER
    // =========================================================

    void HandleLoginError(System.AggregateException exception)
    {
        AuthError? errorCode = null;

        foreach (var ex in exception.Flatten().InnerExceptions)
        {
            if (ex is FirebaseException firebaseEx)
            {
                errorCode = (AuthError)firebaseEx.ErrorCode;
                break;
            }
        }

        switch (errorCode)
        {
            case AuthError.WrongPassword:
            case AuthError.InvalidCredential:
                ShowError(
                    "LOGIN FAILED",
                    "Incorrect email or password."
                );
                return;

            case AuthError.InvalidEmail:
                ShowError(
                    "INVALID EMAIL",
                    "Please enter a valid email address."
                );
                return;

            case AuthError.TooManyRequests:
                ShowError(
                    "TOO MANY ATTEMPTS",
                    "Too many login attempts. Please try again later."
                );
                return;

            case AuthError.NetworkRequestFailed:
                ShowError(
                    "NO INTERNET",
                    "Check your internet connection and try again."
                );
                return;

            case AuthError.UserDisabled:
                ShowError(
                    "ACCOUNT DISABLED",
                    "This account has been disabled. Please contact your administrator."
                );
                return;

            case AuthError.UserNotFound:
                ShowError(
                    "ACCOUNT NOT FOUND",
                    "No account exists with this email address."
                );
                return;

            default:
                ShowError(
                    "LOGIN FAILED",
                    "Incorrect email or password."
                );
                return;
        }
    }

    // =========================================================
    // LOAD USER PROFILE
    // =========================================================

    void LoadUserAndContinue(string userId)
    {
        DocumentReference userRef = db
            .Collection("users")
            .Document(userId);

        userRef.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    auth.SignOut();

                    ShowError(
                        "PROFILE ERROR",
                        "Logged in successfully, but your profile could not be loaded."
                    );
                    return;
                }

                if (!task.Result.Exists)
                {
                    auth.SignOut();

                    ShowError(
                        "PROFILE ERROR",
                        "Your account profile could not be found."
                    );
                    return;
                }

                FirebaseUser user = auth.CurrentUser;

                // Students must verify email.
                if (!user.IsEmailVerified)
                {
                    user.SendEmailVerificationAsync();

                    auth.SignOut();

                    ShowError(
                        "EMAIL NOT VERIFIED",
                        "Please verify your email before logging in. We've sent you another verification email."
                    );
                    return;
                }

                Debug.Log("User profile loaded successfully.");

                // Reset failed attempts after successful login.
                ClearLoginLockoutState();

                SceneManager.LoadScene("HomeScene");
            });
    }

    // =========================================================
    // POPUP + RESET PASSWORD
    // =========================================================

    void ShowError(string title, string message)
    {
        // Clear password field.
        passwordInput.text = "";

        // Reset password visibility.
        passwordVisible = false;
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        // Reset eye icon.
        passwordEyeImage.sprite = showPasswordIcon;

        passwordInput.ActivateInputField();

        AuthPopupManager.Instance.ShowPopup(
            title,
            message,
            "OK"
        );
    }
}