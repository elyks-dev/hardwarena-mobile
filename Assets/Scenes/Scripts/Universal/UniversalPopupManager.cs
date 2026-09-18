using UnityEngine;
using UnityEngine.UI;

public class UniversalPopupManager : MonoBehaviour
{
    [Header("Popup Screen")]
    [SerializeField] private GameObject popupScreen;

    [Header("Buttons")]
    [SerializeField] private Button enableScreenButton;
    [SerializeField] private Button closeScreenButton;

    private void Start()
{
    // Instructions screen starts ON
    if (popupScreen != null)
    {
        popupScreen.SetActive(true);
    }

    if (enableScreenButton != null)
    {
        enableScreenButton.onClick.RemoveAllListeners();
        enableScreenButton.onClick.AddListener(ShowPopup);
    }

    if (closeScreenButton != null)
    {
        closeScreenButton.onClick.RemoveAllListeners();
        closeScreenButton.onClick.AddListener(ClosePopup);
    }
}

    // =====================================================
    // SHOW POPUP
    // =====================================================

    public void ShowPopup()
    {
        if (popupScreen != null)
        {
            popupScreen.SetActive(true);
        }
    }

    // =====================================================
    // CLOSE POPUP
    // =====================================================

    public void ClosePopup()
    {
        if (popupScreen != null)
        {
            popupScreen.SetActive(false);
        }
    }

    // =====================================================
    // TOGGLE POPUP
    // =====================================================

    public void TogglePopup()
    {
        if (popupScreen == null)
            return;

        popupScreen.SetActive(!popupScreen.activeSelf);
    }
}