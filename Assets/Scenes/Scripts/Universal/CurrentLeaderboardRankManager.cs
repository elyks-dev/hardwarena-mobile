using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentLeaderboardRankManager : MonoBehaviour
{
    // =====================================================
    // UI
    // =====================================================

    [Header("Rank UI")]
    public TMP_Text rankTMP;
    public Image rankChangeImage;

    // =====================================================
    // ARROW SPRITES
    // =====================================================

    [Header("Rank Change Sprites")]
    public Sprite arrowUpSprite;
    public Sprite arrowDownSprite;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // PLAYER PREFS
    // =====================================================

    private string savedRankKey;

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (rankChangeImage != null)
            rankChangeImage.gameObject.SetActive(false);

        await LoadCurrentRank();
    }

    // =====================================================
    // LOAD CURRENT RANK
    // =====================================================

    private async Task LoadCurrentRank()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "CurrentLeaderboardRankManager: No Firebase user logged in."
            );

            return;
        }

        try
        {
            savedRankKey = "LeaderboardPreviousRank_" + user.UserId;

            // =================================================
            // GET ALL USERS
            // =================================================

            Query query = db.Collection("users");

            QuerySnapshot snapshot =
                await query.GetSnapshotAsync();

            List<UserRankData> users =
                new List<UserRankData>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.Exists)
                    continue;

                int xp = 0;

                if (document.TryGetValue(
                        "xp",
                        out int savedXP))
                {
                    xp = savedXP;
                }

                users.Add(
                    new UserRankData
                    {
                        userId = document.Id,
                        xp = xp
                    }
                );
            }

            // =================================================
            // SORT BY XP
            // =================================================

            users.Sort(
                (a, b) => b.xp.CompareTo(a.xp)
            );

            // =================================================
            // FIND CURRENT USER
            // =================================================

            int currentRank = -1;

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].userId == user.UserId)
                {
                    currentRank = i + 1;
                    break;
                }
            }

            if (currentRank == -1)
            {
                Debug.LogWarning(
                    "CurrentLeaderboardRankManager: " +
                    "User was not found in leaderboard."
                );

                return;
            }

            // =================================================
            // DISPLAY RANK
            // =================================================

            if (rankTMP != null)
                rankTMP.text = "#" + currentRank.ToString();

            // =================================================
            // PREVIOUS RANK
            // =================================================

            bool hasPreviousRank =
                PlayerPrefs.HasKey(savedRankKey);

            int previousRank = 0;

            if (hasPreviousRank)
            {
                previousRank =
                    PlayerPrefs.GetInt(savedRankKey);
            }

            // =================================================
            // SHOW RANK CHANGE
            // =================================================

            if (rankChangeImage != null)
            {
                if (!hasPreviousRank)
                {
                    // First time checking rank.
                    rankChangeImage.gameObject.SetActive(false);
                }
                else if (currentRank < previousRank)
                {
                    // Rank number decreased = player moved UP.
                    rankChangeImage.sprite = arrowUpSprite;
                    rankChangeImage.gameObject.SetActive(true);
                }
                else if (currentRank > previousRank)
                {
                    // Rank number increased = player moved DOWN.
                    rankChangeImage.sprite = arrowDownSprite;
                    rankChangeImage.gameObject.SetActive(true);
                }
                else
                {
                    // Rank stayed the same.
                    rankChangeImage.gameObject.SetActive(false);
                }
            }

            // =================================================
            // SAVE CURRENT RANK
            // =================================================

            PlayerPrefs.SetInt(
                savedRankKey,
                currentRank
            );

            PlayerPrefs.Save();

            Debug.Log(
                "Current leaderboard rank: " +
                currentRank
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Current leaderboard rank load failed: " +
                e.Message
            );
        }
    }

    // =====================================================
    // DATA CLASS
    // =====================================================

    private class UserRankData
    {
        public string userId;
        public int xp;
    }

    // =====================================================
    // REFRESH
    // =====================================================

    public async void RefreshRank()
    {
        await LoadCurrentRank();
    }
}