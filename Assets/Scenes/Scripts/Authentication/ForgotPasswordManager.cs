using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Extensions;

public class ForgotPasswordManager : MonoBehaviour
{
    private const string ResetCooldownEndKey = "HW_ResetCooldownEnd";
    private const string ResetEmailSentKey = "HW_ResetEmailSent";

    [Header("Input")]
    [SerializeField] private TMP_InputField emailInput;

    [Header("Buttons")]
    [SerializeField] private Button sendButton;

    [Header("Countdown UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Settings")]
    [SerializeField] private float resendCooldown = 30f;

    private FirebaseAuth auth;
    private bool isCooldownActive = false;
    private Coroutine cooldownCoroutine;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        countdownText.gameObject.SetActive(false);
        sendButton.interactable = true;
        sendButton.onClick.AddListener(SendResetEmail);

        RestoreForgotPasswordState();
    }

    void SendResetEmail()
    {
        if (isCooldownActive)
            return;

        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            AuthPopupManager.Instance.ShowPopup(
                "EMAIL REQUIRED",
                "Please enter your email address."
            );
            return;
        }

        if (!IsValidEmail(email))
        {
            AuthPopupManager.Instance.ShowPopup(
                "INVALID EMAIL",
                "Please enter a valid email address."
            );
            return;
        }

        sendButton.interactable = false;

        auth.SendPasswordResetEmailAsync(email)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    sendButton.interactable = true;

                    AuthPopupManager.Instance.ShowPopup(
                        "RESET CANCELLED",
                        "The password reset request was cancelled."
                    );
                    return;
                }

                if (task.IsFaulted)
                {
                    sendButton.interactable = true;

                    Debug.LogError(
                        "Password reset error: " + task.Exception
                    );

                    AuthPopupManager.Instance.ShowPopup(
                        "RESET FAILED",
                        "We couldn't send the password reset email. Please try again."
                    );
                    return;
                }

                sendButton.interactable = false;

                bool emailWasAlreadySent =
                    PlayerPrefs.HasKey(ResetEmailSentKey);

                AuthPopupManager.Instance.ShowPopup(
                    emailWasAlreadySent ? "EMAIL RESENT" : "EMAIL SENT",
                    "A password reset link has been sent to your email."
                );

                PlayerPrefs.SetInt(ResetEmailSentKey, 1);
                PlayerPrefs.Save();

                StartCooldown(resendCooldown, true);
            });
    }

    void StartCooldown(float duration, bool saveCooldown)
    {
        if (isCooldownActive)
            return;

        cooldownCoroutine = StartCoroutine(
            Cooldown(duration, saveCooldown)
        );
    }

    IEnumerator Cooldown(float duration, bool saveCooldown)
    {
        isCooldownActive = true;
        sendButton.interactable = false;
        countdownText.gameObject.SetActive(true);

        if (saveCooldown)
        {
            DateTime cooldownEnd = DateTime.UtcNow.AddSeconds(duration);
            PlayerPrefs.SetString(
                ResetCooldownEndKey,
                cooldownEnd.Ticks.ToString()
            );
            PlayerPrefs.Save();
        }

        float remainingTime = duration;

        while (remainingTime > 0f)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            countdownText.text =
                "Resend in " + seconds + "s.";

            remainingTime -= Time.deltaTime;
            yield return null;
        }

        isCooldownActive = false;
        cooldownCoroutine = null;
        sendButton.interactable = true;
        countdownText.gameObject.SetActive(false);

        PlayerPrefs.DeleteKey(ResetCooldownEndKey);
        PlayerPrefs.Save();
    }

    void RestoreForgotPasswordState()
    {
        if (!PlayerPrefs.HasKey(ResetEmailSentKey))
        {
            sendButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        if (!PlayerPrefs.HasKey(ResetCooldownEndKey))
        {
            sendButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        if (!long.TryParse(
                PlayerPrefs.GetString(ResetCooldownEndKey),
                out long cooldownEndTicks))
        {
            ClearCooldownPreference();
            sendButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        TimeSpan remaining =
            new DateTime(cooldownEndTicks, DateTimeKind.Utc) - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            ClearCooldownPreference();
            sendButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        sendButton.interactable = false;
        countdownText.gameObject.SetActive(true);
        StartCooldown((float)remaining.TotalSeconds, false);
    }

    void ClearCooldownPreference()
    {
        PlayerPrefs.DeleteKey(ResetCooldownEndKey);
        PlayerPrefs.Save();
    }

    public void ResetForgotPasswordState()
    {
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        PlayerPrefs.DeleteKey(ResetCooldownEndKey);
        PlayerPrefs.DeleteKey(ResetEmailSentKey);
        PlayerPrefs.Save();

        isCooldownActive = false;
        sendButton.interactable = true;
        countdownText.gameObject.SetActive(false);
    }

    bool IsValidEmail(string email)
    {
        return email.Contains("@") &&
               email.Contains(".");
    }
}
