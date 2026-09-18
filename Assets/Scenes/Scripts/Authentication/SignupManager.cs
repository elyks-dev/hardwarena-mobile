using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class SignupManager : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField fullNameInput;
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Buttons")]
    public Button signupButton;
    public Button passwordEyeButton;
    public Button confirmPasswordEyeButton;

    [Header("Password Icons")]
    public Image passwordEyeImage;
    public Image confirmPasswordEyeImage;

    public Sprite showPasswordIcon;
    public Sprite hidePasswordIcon;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private bool passwordVisible = false;
    private bool confirmPasswordVisible = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Hide passwords by default.
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();
        confirmPasswordInput.ForceLabelUpdate();

        // Default eye icon.
        passwordEyeImage.sprite = showPasswordIcon;
        confirmPasswordEyeImage.sprite = showPasswordIcon;

        signupButton.onClick.AddListener(SignUp);
        passwordEyeButton.onClick.AddListener(TogglePassword);
        confirmPasswordEyeButton.onClick.AddListener(ToggleConfirmPassword);

        // Press Enter to Sign Up.
        confirmPasswordInput.onSubmit.AddListener(delegate { SignUp(); });
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

        // Change eye icon.
        passwordEyeImage.sprite = passwordVisible ? hidePasswordIcon : showPasswordIcon;
    }

    void ToggleConfirmPassword()
    {
        confirmPasswordVisible = !confirmPasswordVisible;

        confirmPasswordInput.contentType = confirmPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        confirmPasswordInput.ForceLabelUpdate();

        // Change eye icon.
        confirmPasswordEyeImage.sprite = confirmPasswordVisible ? hidePasswordIcon : showPasswordIcon;
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
    // PASSWORD VALIDATION
    // =========================================================

    bool IsStrongPassword(string password)
    {
        // Minimum 8 characters.
        if (password.Length < 8)
            return false;

        // At least one uppercase letter.
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;

        // At least one lowercase letter.
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;

        // At least one number.
        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;

        // At least one special character.
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9\s]"))
            return false;

        return true;
    }

    // =========================================================
    // SIGN UP
    // =========================================================

    void SignUp()
{
    string fullName = fullNameInput.text.Trim();
    string username = usernameInput.text.Trim();
    string email = emailInput.text.Trim().ToLower();
    string password = passwordInput.text;
    string confirmPassword = confirmPasswordInput.text;

    if (string.IsNullOrEmpty(fullName) ||
        string.IsNullOrEmpty(username) ||
        string.IsNullOrEmpty(email) ||
        string.IsNullOrEmpty(password) ||
        string.IsNullOrEmpty(confirmPassword))
    {
        ShowError(
            "INCOMPLETE FORM",
            "Please complete all required fields."
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

    if (password != confirmPassword)
    {
        ShowError(
            "PASSWORD MISMATCH",
            "The passwords you entered do not match."
        );
        return;
    }

    if (!IsStrongPassword(password))
    {
        ShowError(
            "WEAK PASSWORD",
            "Password must contain at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character."
        );
        return;
    }

    signupButton.interactable = false;

    // TEMPORARY: Skip username verification and create the account immediately.
    CreateFirebaseAccount(fullName, username, email, password);
}

    // =========================================================
    // CREATE FIREBASE ACCOUNT
    // =========================================================

    void CreateFirebaseAccount(string fullName, string username, string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                signupButton.interactable = true;

                if (task.IsCanceled)
                {
                    ShowError(
                        "SIGN UP CANCELLED",
                        "The signup attempt was cancelled."
                    );
                    return;
                }

                if (task.IsFaulted)
                {
                    HandleSignupError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;

                Dictionary<string, object> userData = new Dictionary<string, object>()
                {
                    { "fullName", fullName },
                    { "username", username },
                    { "email", email },
                    { "role", "student" },
                    { "xp", 0 },
                    { "level", 1 },
                    { "profileImage", "Otto" },   // Default avatar
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                };

                db.Collection("users")
                    .Document(user.UserId)
                    .SetAsync(userData)
                    .ContinueWithOnMainThread(saveTask =>
                    {
                        if (saveTask.IsCanceled || saveTask.IsFaulted)
                        {
                            ShowError(
                                "PROFILE ERROR",
                                "Your account was created, but your profile could not be saved."
                            );
                            return;
                        }

                        user.SendEmailVerificationAsync()
                            .ContinueWithOnMainThread(emailTask =>
                            {
                                auth.SignOut();

                                if (emailTask.IsCompleted && !emailTask.IsFaulted)
                                {
                                    AuthPopupManager.Instance.ShowPopup(
                                        "VERIFY YOUR EMAIL",
                                        "We've sent a verification email to your email address. Please verify your email before logging in.",
                                        "CONTINUE",
                                        () =>
                                        {
                                            SceneManager.LoadScene("LoginScene");
                                        });
                                }
                                else
                                {
                                    AuthPopupManager.Instance.ShowPopup(
                                        "VERIFICATION EMAIL FAILED",
                                        "Your account was created, but we couldn't send the verification email. Please try logging in later to resend it.",
                                        "CONTINUE",
                                        () =>
                                        {
                                            SceneManager.LoadScene("LoginScene");
                                        });
                                }
                            });
                    });
            });
    }

    // =========================================================
    // SIGNUP ERROR HANDLER
    // =========================================================

    void HandleSignupError(System.AggregateException exception)
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
            case AuthError.EmailAlreadyInUse:
                ShowError(
                    "ACCOUNT EXISTS",
                    "An account with this email already exists."
                );
                return;

            case AuthError.InvalidEmail:
                ShowError(
                    "INVALID EMAIL",
                    "Please enter a valid email address."
                );
                return;

            case AuthError.WeakPassword:
                ShowError(
                    "WEAK PASSWORD",
                    "Password must contain at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character."
                );
                return;

            case AuthError.NetworkRequestFailed:
                ShowError(
                    "NO INTERNET",
                    "Check your internet connection and try again."
                );
                return;

            case AuthError.TooManyRequests:
                ShowError(
                    "TOO MANY ATTEMPTS",
                    "Too many signup attempts. Please try again later."
                );
                return;

            default:
                ShowError(
                    "SIGN UP FAILED",
                    "Unable to create your account. Please try again."
                );
                return;
        }
    }

    // =========================================================
    // POPUP + RESET PASSWORDS
    // =========================================================

    void ShowError(string title, string message)
    {
        // Clear password fields after an error.
        passwordInput.text = "";
        confirmPasswordInput.text = "";

        // Reset to hidden state.
        passwordVisible = false;
        confirmPasswordVisible = false;

        passwordInput.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();
        confirmPasswordInput.ForceLabelUpdate();

        passwordEyeImage.sprite = showPasswordIcon;
        confirmPasswordEyeImage.sprite = showPasswordIcon;

        passwordInput.ActivateInputField();

        AuthPopupManager.Instance.ShowPopup(
            title,
            message,
            "OK"
        );
    }
}