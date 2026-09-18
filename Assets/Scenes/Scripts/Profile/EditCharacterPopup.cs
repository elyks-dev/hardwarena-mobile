using System.Collections.Generic;

using Firebase.Auth;
using Firebase.Firestore;

using UnityEngine;
using UnityEngine.UI;

public class EditCharacterPopup : MonoBehaviour
{
    // ==========================================
    // POPUP
    // ==========================================

    [Header("Popup")]
    public GameObject popup;

    // ==========================================
    // UI IMAGES
    // ==========================================

    [Header("Profile Picture (Circle Image)")]
    public Image profileImage;

    [Header("Popup Preview (Full Body Mascot)")]
    public Image previewImage;

    // ==========================================
    // PROFILE PORTRAITS
    // ==========================================

    [Header("Profile Portrait Sprites")]
    public Sprite profileOttoSprite;
    public Sprite profileKiraSprite;

    // ==========================================
    // FULL BODY PREVIEW
    // ==========================================

    [Header("Popup Preview Sprites")]
    public Sprite previewOttoSprite;
    public Sprite previewKiraSprite;

    // ==========================================
    // CHARACTER BUTTONS
    // ==========================================

    [Header("Character Buttons")]
    public Button ottoButton;
    public Button kiraButton;

    [Header("Selection Colors")]
    public Color selectedColor =
        new Color(0.35f, 0.74f, 1f, 1f);

    public Color normalColor =
        Color.white;

    // ==========================================
    // VARIABLES
    // ==========================================

    private string selectedCharacter = "otto";
    private FirebaseFirestore db;

    // ==========================================
    // START
    // ==========================================

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Default character is Otto.
        if (profileImage != null)
            profileImage.sprite = profileOttoSprite;

        if (previewImage != null)
            previewImage.sprite = previewOttoSprite;

        if (popup != null)
            popup.SetActive(false);

        LoadCharacter();
    }

    // ==========================================
    // OPEN POPUP
    // ==========================================

    public void OpenPopup()
    {
        if (popup != null)
            popup.SetActive(true);

        if (previewImage != null)
        {
            if (selectedCharacter == "kira")
                previewImage.sprite = previewKiraSprite;
            else
                previewImage.sprite = previewOttoSprite;
        }

        UpdateSelectionUI();
    }

    // ==========================================
    // CLOSE POPUP (X BUTTON)
    // ==========================================

    public void ClosePopup()
    {
        if (popup != null)
            popup.SetActive(false);
    }

    // ==========================================
    // SELECT OTTO
    // ==========================================

    public void SelectOtto()
    {
        selectedCharacter = "otto";

        if (previewImage != null)
            previewImage.sprite = previewOttoSprite;

        UpdateSelectionUI();
    }

    // ==========================================
    // SELECT KIRA
    // ==========================================

    public void SelectKira()
    {
        selectedCharacter = "kira";

        if (previewImage != null)
            previewImage.sprite = previewKiraSprite;

        UpdateSelectionUI();
    }

    // ==========================================
    // SAVE CHARACTER
    // ==========================================

    public async void SaveCharacter()
    {
        // ------------------------------------------
        // Update profile portrait
        // ------------------------------------------

        if (profileImage != null)
        {
            if (selectedCharacter == "kira")
                profileImage.sprite = profileKiraSprite;
            else
                profileImage.sprite = profileOttoSprite;
        }

        // Close popup.
        if (popup != null)
            popup.SetActive(false);

        // ------------------------------------------
        // Backup locally
        // ------------------------------------------

        PlayerPrefs.SetString(
            "Character",
            selectedCharacter
        );

        PlayerPrefs.Save();

        // ------------------------------------------
        // Firebase
        // ------------------------------------------

        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.Log(
                "No Firebase user. Saved locally only."
            );

            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users")
                  .Document(user.UserId);

            Dictionary<string, object> updates =
                new Dictionary<string, object>
                {
                    {
                        "profileImage",
                        selectedCharacter
                    }
                };

            // Save character first.
            await userRef.UpdateAsync(updates);

            Debug.Log(
                "Character saved to Firestore: " +
                selectedCharacter
            );

            // ==========================================
            // BADGE: NEW ME
            // ==========================================
            // Only trigger after Firestore save succeeds.

            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.CharacterCustomized();

                Debug.Log(
                    "New Me badge check triggered."
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Character save failed: " +
                e.Message
            );
        }
    }

    // ==========================================
    // LOAD CHARACTER
    // ==========================================

    private async void LoadCharacter()
    {
        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        // Offline fallback.
        if (user == null)
        {
            ApplyCharacter(
                PlayerPrefs.GetString(
                    "Character",
                    "otto"
                )
            );

            return;
        }

        try
        {
            DocumentSnapshot snapshot =
                await db.Collection("users")
                        .Document(user.UserId)
                        .GetSnapshotAsync();

            if (snapshot.Exists &&
                snapshot.TryGetValue(
                    "profileImage",
                    out string character))
            {
                ApplyCharacter(character);
            }
            else
            {
                ApplyCharacter("otto");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Character load failed: " +
                e.Message
            );

            ApplyCharacter(
                PlayerPrefs.GetString(
                    "Character",
                    "otto"
                )
            );
        }
    }

    // ==========================================
    // APPLY CHARACTER TO UI
    // ==========================================

    private void ApplyCharacter(
        string character)
    {
        selectedCharacter = character;

        if (character == "kira")
        {
            if (profileImage != null)
                profileImage.sprite =
                    profileKiraSprite;

            if (previewImage != null)
                previewImage.sprite =
                    previewKiraSprite;
        }
        else
        {
            selectedCharacter = "otto";

            if (profileImage != null)
                profileImage.sprite =
                    profileOttoSprite;

            if (previewImage != null)
                previewImage.sprite =
                    previewOttoSprite;
        }

        UpdateSelectionUI();
    }

    // ==========================================
    // BUTTON HIGHLIGHT
    // ==========================================

    private void UpdateSelectionUI()
    {
        if (ottoButton != null)
        {
            ottoButton.image.color =
                selectedCharacter == "otto"
                ? selectedColor
                : normalColor;
        }

        if (kiraButton != null)
        {
            kiraButton.image.color =
                selectedCharacter == "kira"
                ? selectedColor
                : normalColor;
        }
    }
}