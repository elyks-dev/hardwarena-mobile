using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class RecentActivityManager : MonoBehaviour
{
    // =====================================================
    // UI
    // =====================================================

    [Header("Recent Activity TMPs")]
    public TMP_Text activityTMP1;
    public TMP_Text activityTMP2;

    // =====================================================
    // COLORS
    // =====================================================

    [Header("Text Colors")]
    public Color titleColor = new Color32(0x01, 0x6E, 0xDF, 0xFF);
    public Color descriptionColor = new Color32(0xFF, 0xD1, 0x3F, 0xFF);

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // ACTIVITY DATA
    // =====================================================

    private class ActivityRecord
    {
        public string title;
        public string description;
        public DateTime time;
    }

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        ShowNoActivity();

        await SyncActivitiesToFirestore();
        await LoadRecentActivities();
    }

    // =====================================================
    // SYNC ALL ACTIVITIES INTO recentActivity
    // =====================================================

    private async Task SyncActivitiesToFirestore()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "RecentActivityManager: No Firebase user logged in."
            );

            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users")
                  .Document(user.UserId);

            CollectionReference recentActivityRef =
                userRef.Collection("recentActivity");

            // =================================================
            // LEVEL PROGRESS
            // =================================================

            QuerySnapshot progressSnapshot =
                await userRef.Collection("progress")
                             .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in progressSnapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                bool completed;

                if (!doc.TryGetValue(
                    "completed",
                    out completed) ||
                    !completed)
                {
                    continue;
                }

                DateTime timestamp;

                if (!TryGetTimestamp(
                    doc,
                    out timestamp,
                    "completedAt",
                    "timestamp",
                    "updatedAt"))
                {
                    continue;
                }

                string levelName =
                    FormatDocumentName(doc.Id);

                // Fixed ID prevents duplicates
                string activityId =
                    "level_" + doc.Id.ToLower();

                await SaveActivityIfNotExists(
                    recentActivityRef,
                    activityId,
                    "level",
                    "Completed Level",
                    levelName,
                    timestamp
                );
            }

            // =================================================
            // QUIZ SCORES
            // =================================================

            QuerySnapshot quizSnapshot =
                await userRef.Collection("quizScores")
                             .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in quizSnapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                DateTime timestamp;

                if (!TryGetTimestamp(
                    doc,
                    out timestamp,
                    "completedAt",
                    "timestamp",
                    "updatedAt",
                    "createdAt"))
                {
                    continue;
                }

                string quizName =
                    FormatDocumentName(doc.Id);

                // Fixed ID prevents duplicates
                string activityId =
                    "quiz_" + doc.Id.ToLower();

                await SaveActivityIfNotExists(
                    recentActivityRef,
                    activityId,
                    "quiz",
                    "Completed Quiz",
                    quizName,
                    timestamp
                );
            }

            // =================================================
            // BADGES
            // =================================================

            QuerySnapshot badgeSnapshot =
                await userRef.Collection("badges")
                             .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in badgeSnapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                // Only create activity for actually unlocked badges
                if (doc.ContainsField("unlocked"))
                {
                    bool unlocked;

                    if (doc.TryGetValue(
                        "unlocked",
                        out unlocked) &&
                        !unlocked)
                    {
                        continue;
                    }
                }

                DateTime timestamp;

                if (!TryGetTimestamp(
                    doc,
                    out timestamp,
                    "unlockedAt",
                    "earnedAt",
                    "timestamp",
                    "createdAt",
                    "updatedAt"))
                {
                    continue;
                }

                string badgeName =
                    FormatDocumentName(doc.Id);

                // Fixed ID prevents duplicates
                string activityId =
                    "badge_" + doc.Id.ToLower();

                await SaveActivityIfNotExists(
                    recentActivityRef,
                    activityId,
                    "badge",
                    "Unlocked Badge",
                    badgeName,
                    timestamp
                );
            }

            Debug.Log(
                "RecentActivityManager: Activities synced successfully."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "RecentActivityManager: Activity sync failed: " +
                e.Message
            );
        }
    }

    // =====================================================
    // SAVE ACTIVITY ONLY IF IT DOES NOT ALREADY EXIST
    // =====================================================

    private async Task SaveActivityIfNotExists(
        CollectionReference recentActivityRef,
        string activityId,
        string type,
        string title,
        string description,
        DateTime timestamp)
    {
        DocumentReference activityRef =
            recentActivityRef.Document(activityId);

        DocumentSnapshot snapshot =
            await activityRef.GetSnapshotAsync();

        // Already exists = do nothing
        if (snapshot.Exists)
            return;

        Dictionary<string, object> activityData =
            new Dictionary<string, object>
            {
                { "type", type },
                { "title", title },
                { "description", description },
                { "timestamp", Timestamp.FromDateTime(
                    timestamp.ToUniversalTime()
                )}
            };

        await activityRef.SetAsync(activityData);
    }

    // =====================================================
// LOAD RECENT ACTIVITIES FROM recentActivity
// =====================================================

private async Task LoadRecentActivities()
{
    FirebaseUser user = auth.CurrentUser;

    if (user == null)
    {
        Debug.LogError(
            "RecentActivityManager: No Firebase user logged in."
        );

        ShowNoActivity();
        return;
    }

    try
    {
        DocumentReference userRef =
            db.Collection("users")
              .Document(user.UserId);

        Query query =
            userRef.Collection("recentActivity")
                   .OrderByDescending("timestamp")
                   .Limit(2);

        QuerySnapshot snapshot =
            await query.GetSnapshotAsync();

        List<ActivityRecord> activities =
            new List<ActivityRecord>();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (!doc.Exists)
                continue;

            string title = "Activity";
            string description = "Unknown";

            if (doc.ContainsField("title"))
            {
                try
                {
                    doc.TryGetValue("title", out title);
                }
                catch
                {
                    title = "Activity";
                }
            }

            if (doc.ContainsField("description"))
            {
                try
                {
                    doc.TryGetValue("description", out description);
                }
                catch
                {
                    description = "Unknown";
                }
            }

            DateTime timestamp;

            if (!TryGetTimestamp(doc, out timestamp, "timestamp"))
            {
                continue;
            }

            activities.Add(new ActivityRecord
            {
                title = title,
                description = description,
                time = timestamp
            });
        }

        // =================================================
        // DISPLAY
        // =================================================

        if (activities.Count == 0)
        {
            ShowNoActivity();
            return;
        }

        // First activity always goes to TMP1.
        DisplayActivity(activityTMP1, activities[0]);

        // Second activity only appears if it exists.
        if (activities.Count > 1)
        {
            DisplayActivity(activityTMP2, activities[1]);
        }
        else
        {
            // Leave TMP2 blank when there is only one activity.
            SetTMPText(activityTMP2, "");
        }
    }
    catch (Exception e)
    {
        Debug.LogError(
            "Recent activity load failed: " +
            e.Message
        );

        ShowNoActivity();
    }
}

    // =====================================================
    // DISPLAY ACTIVITY
    // =====================================================

    private void DisplayActivity(
    TMP_Text target,
    ActivityRecord activity)
    {
        if (target == null)
            return;

        string title =
            $"<color=#{ColorUtility.ToHtmlStringRGB(titleColor)}>{activity.title}:</color>";

        string description =
            $"<color=#{ColorUtility.ToHtmlStringRGB(descriptionColor)}>{activity.description}</color>";

        target.text =
            title + " " + description;
    }

    // =====================================================
    // NO ACTIVITY
    // =====================================================

private void ShowNoActivity()
{
    // TMP1 shows the message.
    SetTMPText(
        activityTMP1,
        "No recent activity"
    );

    // TMP2 stays blank.
    SetTMPText(
        activityTMP2,
        ""
    );
}

    private void SetTMPText(
        TMP_Text target,
        string text)
    {
        if (target != null)
            target.text = text;
    }

    // =====================================================
    // TIMESTAMP
    // =====================================================

    private bool TryGetTimestamp(
        DocumentSnapshot document,
        out DateTime timestamp,
        params string[] fieldNames)
    {
        timestamp = DateTime.MinValue;

        foreach (string fieldName in fieldNames)
        {
            if (!document.ContainsField(fieldName))
                continue;

            // ---------------------------------------------
            // Firebase Timestamp
            // ---------------------------------------------

            try
            {
                Timestamp firebaseTimestamp;

                if (document.TryGetValue(
                    fieldName,
                    out firebaseTimestamp))
                {
                    timestamp =
                        firebaseTimestamp.ToDateTime();

                    return true;
                }
            }
            catch
            {
                // Try next format
            }

            // ---------------------------------------------
            // DateTime
            // ---------------------------------------------

            try
            {
                DateTime dateTime;

                if (document.TryGetValue(
                    fieldName,
                    out dateTime))
                {
                    timestamp = dateTime;
                    return true;
                }
            }
            catch
            {
                // Try next field
            }
        }

        return false;
    }

    // =====================================================
    // FORMAT DOCUMENT NAME
    // =====================================================

    private string FormatDocumentName(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            return "Unknown";

        string text =
            documentId
                .Trim()
                .Replace("_", " ")
                .Replace("-", " ");

        // Add space between letters and numbers
        System.Text.RegularExpressions.Regex regex =
            new System.Text.RegularExpressions.Regex(
                @"([a-zA-Z])([0-9])"
            );

        text = regex.Replace(
            text,
            "$1 $2"
        );

        // Add space between number and letter
        regex =
            new System.Text.RegularExpressions.Regex(
                @"([0-9])([a-zA-Z])"
            );

        text = regex.Replace(
            text,
            "$1 $2"
        );

        // Capitalize each word
        string[] words =
            text.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0)
                continue;

            words[i] =
                char.ToUpper(words[i][0]) +
                words[i].Substring(1).ToLower();
        }

        return string.Join(" ", words);
    }

    // =====================================================
    // REFRESH
    // =====================================================

    public async void RefreshActivities()
    {
        await SyncActivitiesToFirestore();
        await LoadRecentActivities();
    }
}