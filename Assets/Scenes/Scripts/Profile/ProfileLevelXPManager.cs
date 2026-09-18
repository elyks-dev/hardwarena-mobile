using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileLevelXPManager : MonoBehaviour
{
    // =====================================================
    // UI
    // =====================================================

    [Header("Profile UI")]
    public TMP_Text levelTMP;
    public TMP_Text xpTMP;
    public Slider xpSlider;

    // =====================================================
    // XP SETTINGS
    // =====================================================

    [Header("XP Progression")]
    public int maxXP = 7000;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // LEVEL THRESHOLDS
    // =====================================================

    private readonly int[] levelThresholds =
    {
        0,      // Level 1
        500,    // Level 2
        1000,   // Level 3
        2000,   // Level 4
        3000,   // Level 5
        4000,   // Level 6
        5000,   // Level 7
        5750,   // Level 8
        6500,   // Level 9
        7000    // Level 10
    };

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Show default values immediately.
        UpdateUI(0);

        await LoadXP();
    }

    // =====================================================
    // LOAD XP
    // =====================================================

    private async Task LoadXP()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("ProfileLevelXPManager: No Firebase user logged in.");
            UpdateUI(0);
            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users")
                  .Document(user.UserId);

            DocumentSnapshot snapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (snapshot.Exists &&
                snapshot.TryGetValue("xp", out int savedXP))
            {
                currentXP = savedXP;
            }

            // Allow XP above the level cap.
            currentXP = Mathf.Max(0, currentXP);

            UpdateUI(currentXP);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Profile XP load failed: " +
                e.Message
            );

            UpdateUI(0);
        }
    }

    // =====================================================
    // UPDATE UI
    // =====================================================

    private void UpdateUI(int currentXP)
    {
        int currentLevel = GetLevel(currentXP);

        if (levelTMP != null)
        {
            levelTMP.text = currentLevel.ToString();
        }

        // =================================================
        // MAX LEVEL (LEVEL 10)
        // =================================================

        if (currentLevel >= 10)
        {
            if (xpTMP != null)
            {
                // Show total XP only once player reaches Level 10.
                xpTMP.text = currentXP.ToString("N0") + " XP";
            }

            if (xpSlider != null)
            {
                xpSlider.minValue = 0f;
                xpSlider.maxValue = 1f;
                xpSlider.value = 1f;
            }

            return;
        }

        // =================================================
        // CURRENT LEVEL RANGE
        // =================================================

        int currentLevelIndex = currentLevel - 1;

        int currentLevelXP =
            levelThresholds[currentLevelIndex];

        int nextLevelXP =
            levelThresholds[currentLevelIndex + 1];

        int progressXP =
            currentXP - currentLevelXP;

        int requiredXP =
            nextLevelXP - currentLevelXP;

        // Prevent invalid values.
        progressXP =
            Mathf.Clamp(progressXP, 0, requiredXP);

        // =================================================
        // XP TEXT
        // =================================================

        if (xpTMP != null)
        {
            xpTMP.text =
                currentXP.ToString("N0") + " / " +
                nextLevelXP.ToString("N0") + " XP";
        }

        // =================================================
        // XP SLIDER
        // =================================================

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;

            if (requiredXP <= 0)
            {
                xpSlider.value = 1f;
            }
            else
            {
                xpSlider.value =
                    (float)progressXP / requiredXP;
            }
        }
    }

    // =====================================================
    // CALCULATE LEVEL
    // =====================================================

    private int GetLevel(int xp)
    {
        for (int i = levelThresholds.Length - 1; i >= 0; i--)
        {
            if (xp >= levelThresholds[i])
            {
                return i + 1;
            }
        }

        return 1;
    }

    // =====================================================
    // REFRESH
    // =====================================================

    public async void RefreshXP()
    {
        await LoadXP();
    }
}