using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;

public class ProfileBadgeManager : MonoBehaviour
{
    [System.Serializable]
    public class ProfileBadgeData
    {
        public string badgeKey;
        public Sprite badgeImage;
        public Button selectionButton;
    }

    [Header("Profile Badge Display")]
    [SerializeField] private Image badgeDisplay1;
    [SerializeField] private Image badgeDisplay2;
    [SerializeField] private Image badgeDisplay3;

    [Header("Edit Popup")]
    [SerializeField] private GameObject badgeSelectionPopup;
    [SerializeField] private Button editBadgeButton;
    [SerializeField] private Button saveBadgeButton;
    [SerializeField] private Button closeBadgeButton;

    [Header("Badge Selection")]
    [SerializeField] private List<ProfileBadgeData> badges =
        new List<ProfileBadgeData>();

    [Header("Locked Appearance")]
    [SerializeField] private float lockedAlpha = 0.35f;

    [Header("Selected Appearance")]
    [SerializeField] private Color selectedColor = Color.green;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private readonly Dictionary<string, bool> unlockedBadges =
        new Dictionary<string, bool>();
    private readonly Dictionary<ProfileBadgeData, Color> originalButtonColors =
        new Dictionary<ProfileBadgeData, Color>();
    private readonly List<string> selectedBadgeKeys =
        new List<string>();

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (badgeSelectionPopup != null)
            badgeSelectionPopup.SetActive(false);

        SetupButtonListeners();
        CaptureOriginalButtonColors();

        LoadProfileState();
    }

    private void SetupButtonListeners()
    {
        if (editBadgeButton != null)
        {
            editBadgeButton.onClick.RemoveAllListeners();
            editBadgeButton.onClick.AddListener(OpenBadgeSelection);
        }

        if (saveBadgeButton != null)
        {
            saveBadgeButton.onClick.RemoveAllListeners();
            saveBadgeButton.onClick.AddListener(SaveDisplayedBadges);
        }

        if (closeBadgeButton != null)
        {
            closeBadgeButton.onClick.RemoveAllListeners();
            closeBadgeButton.onClick.AddListener(CloseBadgeSelection);
        }

        foreach (ProfileBadgeData badge in badges)
        {
            if (badge == null ||
                badge.selectionButton == null)
            {
                continue;
            }

            ProfileBadgeData selectedBadge = badge;

            selectedBadge.selectionButton.onClick.RemoveAllListeners();
            selectedBadge.selectionButton.onClick.AddListener(
                () => ToggleBadgeSelection(selectedBadge)
            );
        }
    }

    private void CaptureOriginalButtonColors()
    {
        foreach (ProfileBadgeData badge in badges)
        {
            if (badge == null ||
                badge.selectionButton == null)
            {
                continue;
            }

            Image buttonImage =
                badge.selectionButton.GetComponent<Image>();

            if (buttonImage != null)
                originalButtonColors[badge] = buttonImage.color;
        }
    }

    private async void LoadProfileState()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning(
                "ProfileBadgeManager: No Firebase user logged in."
            );

            ApplyBadgeDisplays();
            ApplyBadgeButtonStates();
            return;
        }

        // Ensure earned badges exist even if Badge scene was never opened.
        await CheckAndUnlockBadges();

        await LoadBadgeUnlockStates();
        await LoadDisplayedBadges();

        ApplyBadgeButtonStates();
        ApplyBadgeDisplays();
    }

    public async void OpenBadgeSelection()
    {
        if (badgeSelectionPopup != null)
            badgeSelectionPopup.SetActive(true);

        // Ensure earned badges exist even if Badge scene was never opened.
        await CheckAndUnlockBadges();

        await LoadBadgeUnlockStates();
        await LoadDisplayedBadges();

        ApplyBadgeButtonStates();
        ApplyBadgeDisplays();
    }

    private async Task LoadBadgeUnlockStates()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogWarning(
                "ProfileBadgeManager: No Firebase user logged in."
            );

            SetAllBadgesLocked();
            return;
        }

        List<Task<DocumentSnapshot>> readTasks =
            new List<Task<DocumentSnapshot>>();

        foreach (ProfileBadgeData badge in badges)
        {
            if (badge == null ||
                string.IsNullOrWhiteSpace(badge.badgeKey))
            {
                readTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );

                continue;
            }

            readTasks.Add(
                db.Collection("users")
                  .Document(user.UserId)
                  .Collection("badges")
                  .Document(badge.badgeKey)
                  .GetSnapshotAsync()
            );
        }

        try
        {
            DocumentSnapshot[] snapshots =
                await Task.WhenAll(readTasks);

            unlockedBadges.Clear();

            for (int i = 0; i < badges.Count; i++)
            {
                ProfileBadgeData badge = badges[i];

                if (badge == null ||
                    string.IsNullOrWhiteSpace(badge.badgeKey))
                {
                    continue;
                }

                DocumentSnapshot snapshot = snapshots[i];
                bool unlocked = false;

                if (snapshot != null &&
                    snapshot.Exists &&
                    snapshot.TryGetValue(
                        "unlocked",
                        out bool unlockedValue))
                {
                    unlocked = unlockedValue;
                }

                unlockedBadges[badge.badgeKey] = unlocked;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "ProfileBadgeManager: Failed to load badge states. " +
                e.Message
            );

            SetAllBadgesLocked();
        }
    }

    private async Task LoadDisplayedBadges()
    {
        selectedBadgeKeys.Clear();

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        try
        {
            DocumentSnapshot userSnapshot =
                await db.Collection("users")
                        .Document(user.UserId)
                        .GetSnapshotAsync();

            if (!userSnapshot.Exists ||
                !userSnapshot.ContainsField("displayedBadges"))
            {
                return;
            }

            if (userSnapshot.TryGetValue(
                "displayedBadges",
                out List<string> savedBadgeKeys))
            {
                AddSavedBadgeKeys(savedBadgeKeys);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "ProfileBadgeManager: Failed to load displayed badges. " +
                e.Message
            );
        }
    }

    private void AddSavedBadgeKeys(List<string> savedBadgeKeys)
    {
        if (savedBadgeKeys == null)
            return;

        foreach (string badgeKey in savedBadgeKeys)
        {
            if (selectedBadgeKeys.Count >= 3)
                break;

            if (string.IsNullOrWhiteSpace(badgeKey) ||
                selectedBadgeKeys.Contains(badgeKey) ||
                !IsConfiguredBadge(badgeKey) ||
                !IsBadgeUnlocked(badgeKey))
            {
                continue;
            }

            selectedBadgeKeys.Add(badgeKey);
        }
    }

    private bool IsConfiguredBadge(string badgeKey)
    {
        foreach (ProfileBadgeData badge in badges)
        {
            if (badge != null &&
                badge.badgeKey == badgeKey)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBadgeUnlocked(string badgeKey)
    {
        return unlockedBadges.TryGetValue(
            badgeKey,
            out bool unlocked) && unlocked;
    }

    private void SetAllBadgesLocked()
    {
        unlockedBadges.Clear();

        foreach (ProfileBadgeData badge in badges)
        {
            if (badge != null &&
                !string.IsNullOrWhiteSpace(badge.badgeKey))
            {
                unlockedBadges[badge.badgeKey] = false;
            }
        }
    }

    private void ApplyBadgeButtonStates()
    {
        foreach (ProfileBadgeData badge in badges)
        {
            if (badge == null ||
                badge.selectionButton == null)
            {
                continue;
            }

            bool unlocked =
                IsBadgeUnlocked(badge.badgeKey);

            bool selected =
                selectedBadgeKeys.Contains(badge.badgeKey);

            badge.selectionButton.interactable = unlocked;

            Image buttonImage =
                badge.selectionButton.GetComponent<Image>();

            if (buttonImage == null)
                continue;

            if (selected)
            {
                buttonImage.color = selectedColor;
            }
            else if (unlocked)
            {
                RestoreOriginalButtonColor(
                    badge,
                    buttonImage
                );
            }
            else
            {
                Color lockedColor =
                    GetOriginalButtonColor(
                        badge,
                        buttonImage
                    );

                lockedColor.a *= lockedAlpha;
                buttonImage.color = lockedColor;
            }
        }
    }

    private void ToggleBadgeSelection(ProfileBadgeData badge)
    {
        if (badge == null ||
            !IsBadgeUnlocked(badge.badgeKey))
        {
            return;
        }

        if (selectedBadgeKeys.Contains(badge.badgeKey))
        {
            selectedBadgeKeys.Remove(badge.badgeKey);
        }
        else
        {
            if (selectedBadgeKeys.Count >= 3)
            {
                Debug.Log(
                    "Maximum of 3 displayed badges reached."
                );

                return;
            }

            selectedBadgeKeys.Add(badge.badgeKey);
        }

        ApplyBadgeButtonStates();
        ApplyBadgeDisplays();
    }

    private Color GetOriginalButtonColor(
        ProfileBadgeData badge,
        Image buttonImage)
    {
        if (originalButtonColors.TryGetValue(
            badge,
            out Color originalColor))
        {
            return originalColor;
        }

        originalButtonColors[badge] = buttonImage.color;
        return buttonImage.color;
    }

    private void RestoreOriginalButtonColor(
        ProfileBadgeData badge,
        Image buttonImage)
    {
        buttonImage.color =
            GetOriginalButtonColor(
                badge,
                buttonImage
            );
    }

    private void ApplyBadgeDisplays()
    {
        ApplyBadgeDisplay(
            badgeDisplay1,
            selectedBadgeKeys.Count > 0 ?
            selectedBadgeKeys[0] :
            null
        );

        ApplyBadgeDisplay(
            badgeDisplay2,
            selectedBadgeKeys.Count > 1 ?
            selectedBadgeKeys[1] :
            null
        );

        ApplyBadgeDisplay(
            badgeDisplay3,
            selectedBadgeKeys.Count > 2 ?
            selectedBadgeKeys[2] :
            null
        );
    }

    private void ApplyBadgeDisplay(
        Image display,
        string badgeKey)
    {
        if (display == null)
            return;

        ProfileBadgeData badge =
            FindBadge(badgeKey);

        if (badge == null ||
            badge.badgeImage == null)
        {
            display.sprite = null;
            display.gameObject.SetActive(false);
            return;
        }

        display.sprite = badge.badgeImage;
        display.gameObject.SetActive(true);
    }

    private ProfileBadgeData FindBadge(string badgeKey)
    {
        if (string.IsNullOrWhiteSpace(badgeKey))
            return null;

        foreach (ProfileBadgeData badge in badges)
        {
            if (badge != null &&
                badge.badgeKey == badgeKey)
            {
                return badge;
            }
        }

        return null;
    }

    public async void SaveDisplayedBadges()
    {
        NormalizeSelectedBadgeKeys();

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "ProfileBadgeManager: No Firebase user logged in."
            );

            return;
        }

        try
        {
            await db.Collection("users")
                    .Document(user.UserId)
                    .SetAsync(
                        new Dictionary<string, object>()
                        {
                            {
                                "displayedBadges",
                                new List<string>(selectedBadgeKeys)
                            }
                        },
                        SetOptions.MergeAll
                    );

            ApplyBadgeDisplays();
            CloseBadgeSelection();
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "ProfileBadgeManager: Failed to save displayed badges. " +
                e.Message
            );
        }
    }

    private void NormalizeSelectedBadgeKeys()
    {
        List<string> normalizedKeys =
            new List<string>();

        foreach (string badgeKey in selectedBadgeKeys)
        {
            if (normalizedKeys.Count >= 3)
                break;

            if (string.IsNullOrWhiteSpace(badgeKey) ||
                normalizedKeys.Contains(badgeKey))
            {
                continue;
            }

            normalizedKeys.Add(badgeKey);
        }

        selectedBadgeKeys.Clear();
        selectedBadgeKeys.AddRange(normalizedKeys);
    }


    // =========================================================
    // CHECK AND UNLOCK BADGES (Independent of Badge Scene)
    // =========================================================

    private async Task CheckAndUnlockBadges()
    {
        FirebaseUser user = auth.CurrentUser;
        if (user == null) return;

        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        DocumentSnapshot userSnapshot = await userRef.GetSnapshotAsync();

        int xp = 0;
        if (userSnapshot.Exists && userSnapshot.TryGetValue("xp", out int currentXP))
            xp = currentXP;

        if (xp > 0) await UnlockBadge(BadgeManager.TopContender);
        if (xp >= 3000) await UnlockBadge(BadgeManager.RisingPlayer);
        if (xp >= 7000) await UnlockBadge(BadgeManager.FinalAscent);

        if (userSnapshot.TryGetValue("profileImage", out string profileImage) && !string.IsNullOrWhiteSpace(profileImage))
            await UnlockBadge(BadgeManager.NewMe);

        DocumentSnapshot level1 = await userRef.Collection("progress").Document("level1").GetSnapshotAsync();
        if (level1.Exists && level1.TryGetValue("completed", out bool level1Completed) && level1Completed)
            await UnlockBadge(BadgeManager.NextPlease);

        DocumentSnapshot quiz1 = await userRef.Collection("quizScores").Document("quiz1").GetSnapshotAsync();
        if (quiz1.Exists)
            await UnlockBadge(BadgeManager.BrainInAction);

        QuerySnapshot levels = await userRef.Collection("progress").GetSnapshotAsync();
        int completedLevels = 0;
        foreach (DocumentSnapshot doc in levels.Documents)
        {
            if (doc.TryGetValue("completed", out bool completed) && completed)
                completedLevels++;
        }
        if (completedLevels >= 8)
            await UnlockBadge(BadgeManager.MasterOfAll);

        QuerySnapshot quizzes = await userRef.Collection("quizScores").GetSnapshotAsync();
        if (quizzes.Count >= 8)
            await UnlockBadge(BadgeManager.MindOverMatter);
    }

    private async Task UnlockBadge(string badgeId)
    {
        FirebaseUser user = auth.CurrentUser;
        if (user == null) return;

        DocumentReference badgeRef = db.Collection("users")
            .Document(user.UserId)
            .Collection("badges")
            .Document(badgeId);

        DocumentSnapshot snapshot = await badgeRef.GetSnapshotAsync();

        if (snapshot.Exists && snapshot.TryGetValue("unlocked", out bool unlocked) && unlocked)
            return;

        await badgeRef.SetAsync(new Dictionary<string, object>
        {
            { "unlocked", true },
            { "unlockedAt", Timestamp.GetCurrentTimestamp() }
        }, SetOptions.MergeAll);
    }

    private void CloseBadgeSelection()
    {
        if (badgeSelectionPopup != null)
            badgeSelectionPopup.SetActive(false);
    }
}
