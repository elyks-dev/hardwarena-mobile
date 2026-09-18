using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class BadgeManager : MonoBehaviour
{
    // =========================================================
    // INSTANCE
    // =========================================================

    public static BadgeManager Instance { get; private set; }

    // =========================================================
    // BADGE DATA
    // =========================================================

    [Serializable]
    public class BadgeData
    {
        public string key;
        public string badgeName;

        [TextArea(2, 4)]
        public string description;

        public Sprite badgeImage;
        public Button button;
    }

    // =========================================================
    // BADGES
    // =========================================================

    [Header("Badges")]
    [SerializeField]
    private List<BadgeData> badges =
        new List<BadgeData>();

    // =========================================================
    // POPUP
    // =========================================================

    [Header("Popup")]
    [SerializeField]
    private GameObject badgePopup;

    [SerializeField]
    private RectTransform badgePanel;

    [SerializeField]
    private Button blurBackgroundButton;

    [SerializeField]
    private TMP_Text badgeName;

    [SerializeField]
    private TMP_Text badgeStatus;

    [SerializeField]
    private TMP_Text badgeDescription;

    [SerializeField]
    private Image badgeImage;

    // =========================================================
    // LOCKED APPEARANCE
    // =========================================================

    [Header("Locked Appearance")]
    [SerializeField]
    [Range(0f, 1f)]
    private float lockedAlpha = 0.35f;

    // =========================================================
    // PROGRESSION SETTINGS
    // =========================================================

    [Header("Progression Settings")]
    [SerializeField]
    private int totalLevels = 8;

    [SerializeField]
    private int totalQuizzes = 8;

    [Header("XP Requirements")]
    [SerializeField]
    private int risingPlayerXP = 3000;

    [SerializeField]
    private int finalAscentXP = 7000;

    // =========================================================
    // FIREBASE
    // =========================================================

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    // =========================================================
    // BADGE IDS
    // =========================================================

    public const string UltimateCollector =
        "ultimate_collector";

    public const string TopContender =
        "top_contender";

    public const string FinalAscent =
        "the_final_ascent";

    public const string RisingPlayer =
        "rising_player";

    public const string Rank1Dominator =
        "rank_1_dominator";

    public const string NextPlease =
        "next_please";

    public const string NewMe =
        "new_me";

    public const string MindOverMatter =
        "mind_over_matter";

    public const string MasterOfAll =
        "master_of_all";

    public const string FirstSteps =
        "first_steps";

    public const string BrainInAction =
        "brain_in_action";

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentUser = auth.CurrentUser;

        if (badgePopup != null)
            badgePopup.SetActive(false);

        if (blurBackgroundButton != null)
        {
            blurBackgroundButton.onClick.RemoveAllListeners();

            blurBackgroundButton.onClick.AddListener(
                ClosePopup
            );
        }

        SetupBadgeButtons();

        RefreshBadges();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // SETUP BADGE BUTTONS
    // =========================================================

    private void SetupBadgeButtons()
    {
        foreach (BadgeData badge in badges)
        {
            if (badge == null)
                continue;

            if (badge.button == null)
                continue;

            badge.button.interactable = true;

            BadgeData selectedBadge = badge;

            badge.button.onClick.RemoveAllListeners();

            badge.button.onClick.AddListener(
                () => OpenBadgePopup(selectedBadge)
            );

            SetButtonAlpha(
                badge.button,
                lockedAlpha
            );
        }
    }

    // =========================================================
    // UNLOCK BADGE
    // =========================================================

    public async void UnlockBadge(string badgeId)
    {
        await UnlockBadgeAsync(badgeId);
    }

    private async Task<bool> UnlockBadgeAsync(
        string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId))
            return false;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "BadgeManager: No Firebase user logged in."
            );

            return false;
        }

        try
        {
            DocumentReference badgeRef =
                db.Collection("users")
                  .Document(user.UserId)
                  .Collection("badges")
                  .Document(badgeId);

            DocumentSnapshot snapshot =
                await badgeRef.GetSnapshotAsync();

            // Already unlocked.
            if (snapshot.Exists &&
                snapshot.TryGetValue(
                    "unlocked",
                    out bool unlocked) &&
                unlocked)
            {
                return false;
            }

            await badgeRef.SetAsync(
                new Dictionary<string, object>()
                {
                    {
                        "unlocked",
                        true
                    },
                    {
                        "unlockedAt",
                        Timestamp.GetCurrentTimestamp()
                    }
                },
                SetOptions.MergeAll
            );

            Debug.Log(
                "Badge unlocked: " + badgeId
            );

            RefreshBadgeVisual(badgeId);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Badge unlock failed (" +
                badgeId +
                "): " +
                e.Message
            );

            return false;
        }
    }

    // =========================================================
    // LEVEL 1
    // =========================================================

    public void Level1Completed()
    {
        UnlockBadge(NextPlease);
    }

    // =========================================================
    // ANY LEVEL COMPLETED
    // =========================================================

    public async void LevelCompleted(
        string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return;

        // Level 1 = Next Please!
        if (levelId.Equals(
                "level1",
                StringComparison.OrdinalIgnoreCase))
        {
            await UnlockBadgeAsync(
                NextPlease
            );
        }

        await CheckAllLevelsCompleted();
    }

    // =========================================================
    // TUTORIAL
    // =========================================================

    public void TutorialCompleted()
    {
        UnlockBadge(FirstSteps);
    }

    // =========================================================
    // CHARACTER
    // =========================================================

    public void CharacterCustomized()
    {
        UnlockBadge(NewMe);
    }

    // =========================================================
    // QUIZ
    // =========================================================

    public async void QuizCompleted(
        string quizId)
    {
        if (string.IsNullOrWhiteSpace(quizId))
            return;

        // Quiz 1 = Brain in Action!
        if (quizId.Equals(
                "quiz1",
                StringComparison.OrdinalIgnoreCase))
        {
            await UnlockBadgeAsync(
                BrainInAction
            );
        }

        await CheckAllQuizzesCompleted();
    }

    // =========================================================
    // XP BADGE CHECK
    // =========================================================
    // Top Contender:
    // XP > 0
    //
    // Rising Player:
    // XP >= 3000
    //
    // The Final Ascent:
    // XP >= 7000
    // =========================================================

    public void CheckXPBadges(int currentXP)
    {
        if (currentXP > 0)
        {
            UnlockBadge(TopContender);
        }

        if (currentXP >= risingPlayerXP)
        {
            UnlockBadge(RisingPlayer);
        }

        if (currentXP >= finalAscentXP)
        {
            UnlockBadge(FinalAscent);
        }
    }

    // =========================================================
    // RANK 1 BADGE CHECK
    // =========================================================

    public void CheckRank1Badge()
    {
        _ = CheckRank1();
    }

    // =========================================================
    // REFRESH ALL BADGES
    // =========================================================

    public async void RefreshBadges()
    {
        currentUser = auth.CurrentUser;

        // Reset all badge visuals.
        foreach (BadgeData badge in badges)
        {
            if (badge != null)
            {
                SetButtonAlpha(
                    badge.button,
                    lockedAlpha
                );
            }
        }

        if (currentUser == null)
            return;

        // Check all badge conditions.
        await CheckAllProgressionBadges();

        // Reload visual states from Firebase.
        foreach (BadgeData badge in badges)
        {
            if (badge != null)
                CheckBadgeUnlocked(badge);
        }
    }

    // =========================================================
    // CHECK ALL PROGRESSION BADGES
    // =========================================================

    private async Task CheckAllProgressionBadges()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
        return;

    try
    {
        DocumentReference userRef =
            db.Collection("users")
              .Document(user.UserId);

        DocumentSnapshot userSnapshot =
            await userRef.GetSnapshotAsync();

        // =================================================
        // XP
        // =================================================

        int currentXP = 0;

        if (userSnapshot.Exists &&
            userSnapshot.TryGetValue(
                "xp",
                out int xp))
        {
            currentXP = xp;
        }

        if (currentXP > 0)
        {
            await UnlockBadgeAsync(
                TopContender
            );
        }

        if (currentXP >= risingPlayerXP)
        {
            await UnlockBadgeAsync(
                RisingPlayer
            );
        }

        if (currentXP >= finalAscentXP)
        {
            await UnlockBadgeAsync(
                FinalAscent
            );
        }

        // =================================================
        // LEVEL 1
        // =================================================

        await CheckLevel1Completed();

        // =================================================
        // ALL LEVELS
        // =================================================

        await CheckAllLevelsCompleted();

        // =================================================
        // QUIZ 1
        // =================================================

        await CheckQuiz1Completed();

        // =================================================
        // ALL QUIZZES
        // =================================================

        await CheckAllQuizzesCompleted();

        // =================================================
        // CHARACTER
        // =================================================

        await CheckCharacterCustomized();

        // =================================================
        // RANK
        // =================================================

        await CheckRank1();

        // =================================================
        // ULTIMATE COLLECTOR
        // =================================================

        await CheckUltimateCollector();
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Badge progression check failed: " +
            e.Message
        );
    }
}

    // =========================================================
    // CHECK ALL LEVELS
    // =========================================================

    private async Task CheckAllLevelsCompleted()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        try
        {
            CollectionReference progressRef =
                db.Collection("users")
                  .Document(user.UserId)
                  .Collection("progress");

            QuerySnapshot snapshot =
                await progressRef.GetSnapshotAsync();

            int completedLevels = 0;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                if (doc.TryGetValue(
                        "completed",
                        out bool completed) &&
                    completed)
                {
                    completedLevels++;
                }
            }

            if (completedLevels >= totalLevels)
            {
                await UnlockBadgeAsync(
                    MasterOfAll
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Checking all levels failed: " +
                e.Message
            );
        }
    }

    // =========================================================
    // CHECK ALL QUIZZES
    // =========================================================

    private async Task CheckAllQuizzesCompleted()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
        return;

    try
    {
        CollectionReference quizRef =
            db.Collection("users")
              .Document(user.UserId)
              .Collection("quizScores");

        QuerySnapshot snapshot =
            await quizRef.GetSnapshotAsync();

        int completedQuizzes = 0;

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (!doc.Exists)
                continue;

            // Each document in quizScores represents
            // a completed quiz.
            completedQuizzes++;
        }

        Debug.Log(
            "Mind Over Matter check: " +
            completedQuizzes +
            " / " +
            totalQuizzes +
            " quizzes found."
        );

        if (completedQuizzes >= totalQuizzes)
        {
            await UnlockBadgeAsync(
                MindOverMatter
            );
        }
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Checking all quizzes failed: " +
            e.Message
        );
    }
}

private async Task CheckCharacterCustomized()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
        return;

    try
    {
        DocumentReference userRef =
            db.Collection("users")
              .Document(user.UserId);

        DocumentSnapshot snapshot =
            await userRef.GetSnapshotAsync();

        if (!snapshot.Exists)
            return;

        if (snapshot.TryGetValue(
                "profileImage",
                out string character))
        {
            if (!string.IsNullOrWhiteSpace(character))
            {
                await UnlockBadgeAsync(NewMe);
            }
        }
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Checking New Me failed: " +
            e.Message
        );
    }
}

    private async Task CheckLevel1Completed()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
        return;

    try
    {
        DocumentReference level1Ref =
            db.Collection("users")
              .Document(user.UserId)
              .Collection("progress")
              .Document("level1");

        DocumentSnapshot snapshot =
            await level1Ref.GetSnapshotAsync();

        if (!snapshot.Exists)
            return;

        if (snapshot.TryGetValue(
                "completed",
                out bool completed) &&
            completed)
        {
            await UnlockBadgeAsync(
                NextPlease
            );
        }
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Checking Next Please failed: " +
            e.Message
        );
    }
}

private async Task CheckQuiz1Completed()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
        return;

    try
    {
        DocumentReference quiz1Ref =
            db.Collection("users")
              .Document(user.UserId)
              .Collection("quizScores")
              .Document("quiz1");

        DocumentSnapshot snapshot =
            await quiz1Ref.GetSnapshotAsync();

        if (!snapshot.Exists)
            return;

        if (snapshot.TryGetValue(
                "completed",
                out bool completed) &&
            completed)
        {
            await UnlockBadgeAsync(
                BrainInAction
            );
        }
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Checking Brain in Action failed: " +
            e.Message
        );
    }
}   

    // =========================================================
    // CHECK RANK 1
    // =========================================================

    private async Task CheckRank1()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        try
        {
            QuerySnapshot snapshot =
                await db.Collection("users")
                       .GetSnapshotAsync();

            List<UserRankData> users =
                new List<UserRankData>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                // Only students should count toward ranking.
                if (!doc.TryGetValue(
                        "role",
                        out string role))
                {
                    continue;
                }

                if (role != "student")
                    continue;

                int xp = 0;

                if (doc.TryGetValue(
                        "xp",
                        out int savedXP))
                {
                    xp = savedXP;
                }

                users.Add(
                    new UserRankData
                    {
                        userId = doc.Id,
                        xp = xp
                    }
                );
            }

            users.Sort(
                (a, b) =>
                    b.xp.CompareTo(a.xp)
            );

            if (users.Count == 0)
                return;

            // Highest XP student = Rank #1.
            if (users[0].userId ==
                user.UserId)
            {
                await UnlockBadgeAsync(
                    Rank1Dominator
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Checking rank 1 failed: " +
                e.Message
            );
        }
    }

    // =========================================================
    // ULTIMATE COLLECTOR
    // =========================================================

    private async Task CheckUltimateCollector()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        string[] requiredBadges =
        {
            TopContender,
            FinalAscent,
            RisingPlayer,
            Rank1Dominator,
            NextPlease,
            NewMe,
            MindOverMatter,
            MasterOfAll,
            FirstSteps,
            BrainInAction
        };

        try
        {
            CollectionReference badgesRef =
                db.Collection("users")
                  .Document(user.UserId)
                  .Collection("badges");

            QuerySnapshot snapshot =
                await badgesRef.GetSnapshotAsync();

            HashSet<string> unlockedBadges =
                new HashSet<string>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                if (doc.TryGetValue(
                        "unlocked",
                        out bool unlocked) &&
                    unlocked)
                {
                    unlockedBadges.Add(
                        doc.Id
                    );
                }
            }

            // All 10 other badges required.
            foreach (string requiredBadge
                in requiredBadges)
            {
                if (!unlockedBadges.Contains(
                        requiredBadge))
                {
                    return;
                }
            }

            DocumentReference ultimateRef =
                badgesRef.Document(
                    UltimateCollector
                );

            DocumentSnapshot ultimateSnapshot =
                await ultimateRef.GetSnapshotAsync();

            if (ultimateSnapshot.Exists &&
                ultimateSnapshot.TryGetValue(
                    "unlocked",
                    out bool alreadyUnlocked) &&
                alreadyUnlocked)
            {
                return;
            }

            await ultimateRef.SetAsync(
                new Dictionary<string, object>()
                {
                    {
                        "unlocked",
                        true
                    },
                    {
                        "unlockedAt",
                        Timestamp.GetCurrentTimestamp()
                    }
                },
                SetOptions.MergeAll
            );

            Debug.Log(
                "Badge unlocked: " +
                UltimateCollector
            );

            RefreshBadgeVisual(
                UltimateCollector
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Ultimate Collector check failed: " +
                e.Message
            );
        }
    }

    // =========================================================
    // CHECK FIREBASE BADGE STATUS
    // =========================================================

    private void CheckBadgeUnlocked(
        BadgeData badge)
    {
        if (badge == null ||
            badge.button == null)
            return;

        if (currentUser == null)
        {
            SetButtonAlpha(
                badge.button,
                lockedAlpha
            );

            return;
        }

        DocumentReference badgeReference =
            db.Collection("users")
              .Document(currentUser.UserId)
              .Collection("badges")
              .Document(badge.key);

        badgeReference
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled ||
                    task.IsFaulted)
                {
                    SetButtonAlpha(
                        badge.button,
                        lockedAlpha
                    );

                    return;
                }

                DocumentSnapshot snapshot =
                    task.Result;

                bool unlocked = false;

                if (snapshot.Exists &&
                    snapshot.ContainsField(
                        "unlocked"))
                {
                    unlocked =
                        snapshot.GetValue<bool>(
                            "unlocked"
                        );
                }

                SetButtonAlpha(
                    badge.button,
                    unlocked
                        ? 1f
                        : lockedAlpha
                );
            });
    }

    // =========================================================
    // REFRESH ONE BADGE VISUAL
    // =========================================================

    private void RefreshBadgeVisual(
        string badgeId)
    {
        foreach (BadgeData badge in badges)
        {
            if (badge == null)
                continue;

            if (badge.key != badgeId)
                continue;

            SetButtonAlpha(
                badge.button,
                1f
            );

            return;
        }
    }

    // =========================================================
    // OPEN BADGE POPUP
    // =========================================================

    private void OpenBadgePopup(
        BadgeData badge)
    {
        if (currentUser == null)
        {
            Debug.LogWarning(
                "No user is currently signed in."
            );

            return;
        }

        if (badgeName != null)
            badgeName.text =
                badge.badgeName;

        if (badgeDescription != null)
            badgeDescription.text =
                badge.description;

        if (badgeImage != null)
            badgeImage.sprite =
                badge.badgeImage;

        if (badgeStatus != null)
            badgeStatus.text =
                "Badge Status";

        if (badgePopup != null)
            badgePopup.SetActive(true);

        CheckBadgeStatusForPopup(
            badge
        );
    }

    // =========================================================
    // CHECK POPUP STATUS
    // =========================================================

    private void CheckBadgeStatusForPopup(
        BadgeData badge)
    {
        if (currentUser == null)
            return;

        DocumentReference badgeReference =
            db.Collection("users")
              .Document(currentUser.UserId)
              .Collection("badges")
              .Document(badge.key);

        badgeReference
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled ||
                    task.IsFaulted)
                {
                    if (badgeStatus != null)
                        badgeStatus.text =
                            "Status Unknown";

                    return;
                }

                DocumentSnapshot snapshot =
                    task.Result;

                bool unlocked = false;

                if (snapshot.Exists &&
                    snapshot.ContainsField(
                        "unlocked"))
                {
                    unlocked =
                        snapshot.GetValue<bool>(
                            "unlocked"
                        );
                }

                if (badgeStatus != null)
                {
                    badgeStatus.text =
                        unlocked
                        ? "Badge Unlocked"
                        : "Badge Locked";
                }
            });
    }

    // =========================================================
    // CLOSE POPUP
    // =========================================================

    private void ClosePopup()
    {
        if (badgePopup != null)
            badgePopup.SetActive(false);
    }

    // =========================================================
    // SET BUTTON ALPHA
    // =========================================================

    private void SetButtonAlpha(
        Button button,
        float alpha)
    {
        if (button == null)
            return;

        Image image =
            button.GetComponent<Image>();

        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;

        image.color = color;
    }

    // =========================================================
    // DATA
    // =========================================================

    private class UserRankData
    {
        public string userId;
        public int xp;
    }
}