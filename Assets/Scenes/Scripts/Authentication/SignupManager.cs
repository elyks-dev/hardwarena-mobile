
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

    [Header("Grade Level Dropdown")]
    public TMP_Text gradeLevelText;
    public Button dropdownButton;
    public GameObject gradeLevelDropdownObject;
    public Button grade7Button;
    public Button grade8Button;
    public Button grade9Button;
    public Button grade10Button;

    [Header("Dropdown Arrow Sprites")]
    public Image dropdownArrowImage;
    public Sprite dropdownArrowDownSprite;
    public Sprite dropdownArrowUpSprite;

    private string selectedGradeLevel = "";

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private bool passwordVisible = false;
    private bool confirmPasswordVisible = false;
    private bool isSigningUp = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Hide passwords by default.
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();
        confirmPasswordInput.ForceLabelUpdate();

        // Set default password eye icons.
        passwordEyeImage.sprite = showPasswordIcon;
        confirmPasswordEyeImage.sprite = showPasswordIcon;

        // Connect signup and password buttons.
        signupButton.onClick.AddListener(SignUp);
        passwordEyeButton.onClick.AddListener(TogglePassword);
        confirmPasswordEyeButton.onClick.AddListener(ToggleConfirmPassword);

        // Press Enter to sign up.
        confirmPasswordInput.onSubmit.AddListener(delegate { SignUp(); });

        // Initialize the grade dropdown.
        SetupGradeLevelDropdown();
    }

    // =========================================================
    // GRADE LEVEL DROPDOWN
    // =========================================================

    void SetupGradeLevelDropdown()
    {
        if (gradeLevelDropdownObject != null)
            gradeLevelDropdownObject.SetActive(false);

        if (gradeLevelText != null)
            gradeLevelText.text = "Grade Level";

        // Default arrow points down.
        if (dropdownArrowImage != null)
            dropdownArrowImage.sprite = dropdownArrowDownSprite;

        // Connect dropdown toggle button.
        if (dropdownButton != null)
            dropdownButton.onClick.AddListener(ToggleGradeDropdown);

        // Store lowercase values for Firestore.
        // Display normal grade names in the UI.
        if (grade7Button != null)
            grade7Button.onClick.AddListener(
                delegate { SelectGradeLevel("grade7", "Grade 7"); }
            );

        if (grade8Button != null)
            grade8Button.onClick.AddListener(
                delegate { SelectGradeLevel("grade8", "Grade 8"); }
            );

        if (grade9Button != null)
            grade9Button.onClick.AddListener(
                delegate { SelectGradeLevel("grade9", "Grade 9"); }
            );

        if (grade10Button != null)
            grade10Button.onClick.AddListener(
                delegate { SelectGradeLevel("grade10", "Grade 10"); }
            );
    }

    void ToggleGradeDropdown()
    {
        if (gradeLevelDropdownObject == null)
            return;

        bool isOpening = !gradeLevelDropdownObject.activeSelf;
        gradeLevelDropdownObject.SetActive(isOpening);

        // Change arrow based on dropdown state.
        if (dropdownArrowImage != null)
        {
            dropdownArrowImage.sprite = isOpening
                ? dropdownArrowUpSprite
                : dropdownArrowDownSprite;
        }
    }

    void SelectGradeLevel(string databaseValue, string displayText)
    {
        selectedGradeLevel = databaseValue;

        // Update the displayed TMP text.
        if (gradeLevelText != null)
            gradeLevelText.text = displayText;

        // Close the dropdown after selection.
        if (gradeLevelDropdownObject != null)
            gradeLevelDropdownObject.SetActive(false);

        // Reset arrow to down.
        if (dropdownArrowImage != null)
            dropdownArrowImage.sprite = dropdownArrowDownSprite;
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

    void ToggleConfirmPassword()
    {
        confirmPasswordVisible = !confirmPasswordVisible;

        confirmPasswordInput.contentType = confirmPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        confirmPasswordInput.ForceLabelUpdate();

        confirmPasswordEyeImage.sprite = confirmPasswordVisible
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
        if (isSigningUp)
            return;

        string fullName = fullNameInput.text.Trim();
        string username = usernameInput.text.Trim();
        string email = emailInput.text.Trim().ToLowerInvariant();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        // Validate required fields.
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

        // Require a grade selection.
        // This popup does not clear the password fields.
        if (string.IsNullOrEmpty(selectedGradeLevel))
        {
            AuthPopupManager.Instance.ShowPopup(
                "GRADE LEVEL REQUIRED",
                "Please select your grade level.",
                "OK"
            );
            return;
        }

        // Validate email format.
        if (!IsValidEmail(email))
        {
            ShowError(
                "INVALID EMAIL",
                "Please enter a valid email address."
            );
            return;
        }

        // Validate matching passwords.
        if (password != confirmPassword)
        {
            ShowError(
                "PASSWORD MISMATCH",
                "The passwords you entered do not match."
            );
            return;
        }

        // Validate password strength.
        if (!IsStrongPassword(password))
        {
            ShowError(
                "WEAK PASSWORD",
                "Password must contain at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character."
            );
            return;
        }

        // Prevent repeated signup attempts.
        isSigningUp = true;

        if (signupButton != null)
            signupButton.interactable = false;

        CreateFirebaseAccount(
            fullName,
            username,
            email,
            password,
            selectedGradeLevel
        );
    }

    // =========================================================
    // CREATE FIREBASE ACCOUNT
    // =========================================================

    void CreateFirebaseAccount(
        string fullName,
        string username,
        string email,
        string password,
        string gradeLevel)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    ResetSignupButton();

                    ShowError(
                        "SIGN UP CANCELLED",
                        "The signup attempt was cancelled."
                    );
                    return;
                }

                if (task.IsFaulted)
                {
                    ResetSignupButton();
                    HandleSignupError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;

                if (user == null)
                {
                    ResetSignupButton();

                    ShowError(
                        "SIGN UP FAILED",
                        "Unable to create your account. Please try again."
                    );
                    return;
                }

                // Prepare the user's Firestore profile.
                Dictionary<string, object> userData =
                    new Dictionary<string, object>()
                    {
                        { "fullName", fullName },
                        { "username", username },
                        { "email", email },
                        { "role", "student" },
                        { "gradeLevel", gradeLevel },
                        { "xp", 0 },
                        { "level", 1 },
                        { "profileImage", "Otto" },
                        { "createdAt", Timestamp.GetCurrentTimestamp() }
                    };

                Debug.Log(
                    "Attempting to save Firestore profile for UID: " +
                    user.UserId +
                    ", gradeLevel: " + gradeLevel
                );

                // Save profile to Firestore.
                db.Collection("users")
                    .Document(user.UserId)
                    .SetAsync(userData)
                    .ContinueWithOnMainThread(saveTask =>
                    {
                        if (saveTask.IsCanceled || saveTask.IsFaulted)
                        {
                            Debug.LogError(
                                "Firestore profile save failed: " +
                                (saveTask.Exception != null
                                    ? saveTask.Exception.ToString()
                                    : "Task was canceled without an exception.")
                            );

                            auth.SignOut();
                            ResetSignupButton();

                            ShowError(
                                "PROFILE ERROR",
                                "Your account was created, but your profile could not be saved. Check the Unity Console for details."
                            );
                            return;
                        }

                        Debug.Log(
                            "Firestore profile saved successfully for UID: " +
                            user.UserId
                        );

                        // Send email verification.
                        user.SendEmailVerificationAsync()
                            .ContinueWithOnMainThread(emailTask =>
                            {
                                // Sign out after attempting verification.
                                auth.SignOut();
                                ResetSignupButton();

                                if (emailTask.IsCompleted &&
                                    !emailTask.IsCanceled &&
                                    !emailTask.IsFaulted)
                                {
                                    AuthPopupManager.Instance.ShowPopup(
                                        "VERIFY YOUR EMAIL",
                                        "We've sent a verification email to your email address. Please verify your email before logging in.",
                                        "CONTINUE",
                                        () =>
                                        {
                                            SceneManager.LoadScene("LoginScene");
                                        }
                                    );
                                }
                                else
                                {
                                    Debug.LogError(
                                        "Verification email failed: " +
                                        (emailTask.Exception != null
                                            ? emailTask.Exception.ToString()
                                            : "No exception details available.")
                                    );

                                    AuthPopupManager.Instance.ShowPopup(
                                        "VERIFICATION EMAIL FAILED",
                                        "Your account was created, but we couldn't send the verification email. Please try logging in later to resend it.",
                                        "CONTINUE",
                                        () =>
                                        {
                                            SceneManager.LoadScene("LoginScene");
                                        }
                                    );
                                }
                            });
                    });
            });
    }

    void ResetSignupButton()
    {
        isSigningUp = false;

        if (signupButton != null)
            signupButton.interactable = true;
    }

    // =========================================================
    // SIGNUP ERROR HANDLER
    // =========================================================

    void HandleSignupError(System.AggregateException exception)
    {
        AuthError? errorCode = null;

        if (exception != null)
        {
            foreach (var ex in exception.Flatten().InnerExceptions)
            {
                if (ex is FirebaseException firebaseEx)
                {
                    errorCode = (AuthError)firebaseEx.ErrorCode;
                    break;
                }
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
        // Clear password fields after a validation or signup error.
        passwordInput.text = "";
        confirmPasswordInput.text = "";

        // Reset password visibility.
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
