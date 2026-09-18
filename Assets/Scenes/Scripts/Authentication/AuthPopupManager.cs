using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthPopupManager : MonoBehaviour
{
    public static AuthPopupManager Instance;

    [Header("UI")]
    public CanvasGroup canvasGroup;

    public TMP_Text titleText;
    public TMP_Text messageText;

    public Button actionButton;
    public TMP_Text actionButtonText;

    private Action callbackAction;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HidePopup();

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(ClosePopup);
    }

    public void ShowPopup(
        string title,
        string message,
        string buttonText = "OK",
        Action callback = null)
    {
        titleText.text = title;
        messageText.text = message;
        actionButtonText.text = buttonText;

        callbackAction = callback;

        ShowPopupUI();
    }

    private void ShowPopupUI()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HidePopup()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ClosePopup()
    {
        HidePopup();

        callbackAction?.Invoke();
        callbackAction = null;
    }
}