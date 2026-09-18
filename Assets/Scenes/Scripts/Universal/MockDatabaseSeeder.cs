using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class MockDatabaseSeeder : MonoBehaviour
{
    private FirebaseFirestore db;

    private async void Start()
    {
        Debug.Log("=================================");
        Debug.Log("MOCK DATABASE SEEDER STARTED");
        Debug.Log("=================================");

        try
        {
            db = FirebaseFirestore.DefaultInstance;

            await CreateMockUser();

            Debug.Log("=================================");
            Debug.Log("MOCK DATABASE CREATED SUCCESSFULLY");
            Debug.Log("=================================");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "MOCK DATABASE SEEDER ERROR: " + e
            );
        }
    }

    // =========================================================
    // MAIN USER
    // =========================================================

    private async Task CreateMockUser()
    {
        string userId = "MOCK_USER_001";

        DocumentReference userRef =
            db.Collection("users").Document(userId);

        // =====================================================
        // USER PROFILE
        // =====================================================

        Dictionary<string, object> userData =
            new Dictionary<string, object>
            {
                { "username", "MockPlayer" },
                { "email", "mockplayer@example.com" },
                { "role", "student" },

                { "profileImage", "" },

                {
                    "bio",
                    "This is a mock HardWarena student account."
                },

                { "level", 4 },
                { "xp", 1250 },

                {
                    "createdAt",
                    Timestamp.GetCurrentTimestamp()
                }
            };

        await userRef.SetAsync(userData);

        Debug.Log(
            "Created: users/" + userId
        );

        // =====================================================
        // PROGRESS
        // =====================================================

        await CreateLevelProgress(
            userRef,
            "level1",
            100,
            true
        );

        await CreateLevelProgress(
            userRef,
            "level2",
            50,
            false
        );

        await CreateLevelProgress(
            userRef,
            "level3",
            0,
            false
        );

        // =====================================================
        // QUIZ SCORES
        // =====================================================

        await CreateQuizScore(
            userRef,
            "quiz1",
            8,
            10,
            2
        );

        await CreateQuizScore(
            userRef,
            "quiz2",
            6,
            10,
            1
        );

        // =====================================================
        // BADGES
        // =====================================================

        await CreateBadges(userRef);

        // =====================================================
        // SHOWCASED BADGES
        // =====================================================

        await CreateShowcase(userRef);

        // =====================================================
        // RECENT ACHIEVEMENTS
        // =====================================================

        await CreateAchievement(
            userRef,
            "achievement001",
            "lesson_completed",
            "Completed Lesson 1",
            "Completed the first lesson.",
            "level1"
        );

        await CreateAchievement(
            userRef,
            "achievement002",
            "quiz_completed",
            "Completed Quiz 1",
            "Completed the first quiz.",
            "quiz1"
        );

        await CreateAchievement(
            userRef,
            "achievement003",
            "badge_unlocked",
            "Unlocked First Steps",
            "Unlocked the First Steps badge.",
            "first_steps"
        );

        // =====================================================
        // SETTINGS
        // =====================================================

        await CreateSettings(userRef);

        // =====================================================
        // ADMIN DATA
        // =====================================================

        await CreateAdminData(userRef);

        // =====================================================
        // ADMIN ACTIVITY LOGS
        // =====================================================

        await CreateActivity(
            userRef,
            "activity001",
            "login",
            "User logged in."
        );

        await CreateActivity(
            userRef,
            "activity002",
            "profile_update",
            "User updated their profile bio."
        );

        await CreateActivity(
            userRef,
            "activity003",
            "level_completed",
            "User completed Level 1."
        );

        await CreateActivity(
            userRef,
            "activity004",
            "quiz_completed",
            "User completed Quiz 1."
        );

        Debug.Log(
            "Finished creating mock user data."
        );
    }

    // =========================================================
    // LEVEL PROGRESS
    // =========================================================

    private async Task CreateLevelProgress(
        DocumentReference userRef,
        string levelId,
        int progress,
        bool completed
    )
    {
        DocumentReference levelRef =
            userRef
                .Collection("progress")
                .Document(levelId);

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "progress", progress },
                { "completed", completed },

                {
                    "firstCompletedAt",
                    completed
                        ? Timestamp.GetCurrentTimestamp()
                        : null
                }
            };

        await levelRef.SetAsync(data);

        Debug.Log(
            "Created progress/" + levelId
        );
    }

    // =========================================================
    // QUIZ SCORE
    // =========================================================

    private async Task CreateQuizScore(
        DocumentReference userRef,
        string quizId,
        int score,
        int totalQuestions,
        int attempts
    )
    {
        int percentage =
            Mathf.RoundToInt(
                ((float)score / totalQuestions) * 100
            );

        DocumentReference quizRef =
            userRef
                .Collection("quizScores")
                .Document(quizId);

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "score", score },
                { "totalQuestions", totalQuestions },
                { "percentage", percentage },
                { "attempts", attempts },
                { "bestScore", score },

                {
                    "lastAttemptAt",
                    Timestamp.GetCurrentTimestamp()
                }
            };

        await quizRef.SetAsync(data);

        Debug.Log(
            "Created quizScores/" + quizId
        );
    }

    // =========================================================
    // BADGES
    // =========================================================

    private async Task CreateBadges(
        DocumentReference userRef
    )
    {
        string[] badgeKeys =
        {
            "first_steps",
            "new_me",
            "next_please",
            "brain_in_action",
            "rising_player",
            "top_contender",
            "final_ascent",
            "master_of_all",
            "mind_over_matter",
            "rank_1_dominator",
            "ultimate_collector"
        };

        // First 4 are unlocked for mock purposes
        int unlockedCount = 4;

        for (int i = 0; i < badgeKeys.Length; i++)
        {
            bool unlocked = i < unlockedCount;

            DocumentReference badgeRef =
                userRef
                    .Collection("badges")
                    .Document(badgeKeys[i]);

            Dictionary<string, object> data =
                new Dictionary<string, object>
                {
                    { "unlocked", unlocked },

                    {
                        "unlockedAt",
                        unlocked
                            ? Timestamp.GetCurrentTimestamp()
                            : null
                    }
                };

            await badgeRef.SetAsync(data);

            Debug.Log(
                "Created badges/" +
                badgeKeys[i]
            );
        }
    }

    // =========================================================
    // SHOWCASED BADGES
    // =========================================================

    private async Task CreateShowcase(
        DocumentReference userRef
    )
    {
        DocumentReference showcaseRef =
            userRef
                .Collection("showcase")
                .Document("selectedBadges");

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                {
                    "selectedBadges",
                    new List<string>
                    {
                        "first_steps",
                        "new_me",
                        "next_please"
                    }
                }
            };

        await showcaseRef.SetAsync(data);

        Debug.Log(
            "Created showcase/selectedBadges"
        );
    }

    // =========================================================
    // ACHIEVEMENT
    // =========================================================

    private async Task CreateAchievement(
        DocumentReference userRef,
        string achievementId,
        string type,
        string title,
        string description,
        string referenceId
    )
    {
        DocumentReference achievementRef =
            userRef
                .Collection("achievements")
                .Document(achievementId);

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "type", type },
                { "title", title },
                { "description", description },
                { "referenceId", referenceId },

                {
                    "timestamp",
                    Timestamp.GetCurrentTimestamp()
                }
            };

        await achievementRef.SetAsync(data);

        Debug.Log(
            "Created achievements/" +
            achievementId
        );
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private async Task CreateSettings(
        DocumentReference userRef
    )
    {
        DocumentReference settingsRef =
            userRef
                .Collection("settings")
                .Document("preferences");

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "musicEnabled", true },
                { "musicVolume", 0.75 },
                { "sfxEnabled", true },
                { "sfxVolume", 0.85 }
            };

        await settingsRef.SetAsync(data);

        Debug.Log(
            "Created settings/preferences"
        );
    }

    // =========================================================
    // ADMIN DATA
    // =========================================================

    private async Task CreateAdminData(
        DocumentReference userRef
    )
    {
        DocumentReference adminRef =
            userRef
                .Collection("adminData")
                .Document("account");

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "accountStatus", "active" },
                { "loginCount", 47 },

                {
                    "lastLogin",
                    Timestamp.GetCurrentTimestamp()
                },

                { "lastKnownDevice", "iPhone" },
                { "lastKnownPlatform", "iOS" }

            };

        await adminRef.SetAsync(data);

        Debug.Log(
            "Created adminData/account"
        );
    }

    // =========================================================
    // ADMIN ACTIVITY
    // =========================================================

    private async Task CreateActivity(
        DocumentReference userRef,
        string activityId,
        string type,
        string description
    )
    {
        DocumentReference activityRef =
            userRef
                .Collection("adminData")
                .Document("account")
                .Collection("activity")
                .Document(activityId);

        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "type", type },
                { "description", description },

                {
                    "timestamp",
                    Timestamp.GetCurrentTimestamp()
                }
            };

        await activityRef.SetAsync(data);

        Debug.Log(
            "Created activity/" +
            activityId
        );
    }
}