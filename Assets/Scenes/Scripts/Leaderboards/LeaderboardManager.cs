using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    // =====================================================
    // SCROLL VIEW
    // =====================================================

    [Header("Scroll View")]
    public Transform content;

    // =====================================================
    // LEADERBOARD PREFABS
    // =====================================================

    [Header("Leaderboard Prefabs")]
    public GameObject top1Prefab;
    public GameObject top2Prefab;
    public GameObject top3Prefab;
    public GameObject normalPrefab;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        await LoadLeaderboard();
    }

    // =====================================================
    // ON ENABLE
    // =====================================================

    private async void OnEnable()
    {
        if (db == null)
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        await LoadLeaderboard();
    }

    // =====================================================
    // LOAD LEADERBOARD
    // =====================================================

    public async Task LoadLeaderboard()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogError("Leaderboard: No Firebase user is logged in.");
            return;
        }

        Debug.Log("Leaderboard UID: " + currentUser.UserId);

        try
        {
            QuerySnapshot snapshot = await db
                .Collection("users")
                .GetSnapshotAsync();

            List<LeaderboardPlayer> players = new List<LeaderboardPlayer>();

            // =================================================
            // BUILD PLAYER LIST
            // =================================================

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (!doc.Exists)
                    continue;

                if (!doc.TryGetValue("role", out string role))
                    continue;

                if (role != "student")
                    continue;

                LeaderboardPlayer player = new LeaderboardPlayer();

                player.userId = doc.Id;

                player.username =
                    doc.TryGetValue("username", out string username)
                        ? username
                        : "Unknown Player";

                player.xp =
                    doc.TryGetValue("xp", out int xp)
                        ? xp
                        : 0;

                player.profileImage =
                    doc.TryGetValue("profileImage", out string profile)
                        ? profile
                        : "otto";

                players.Add(player);
            }

            // =================================================
            // SORT PLAYERS BY XP
            // =================================================

            players = players
                .OrderByDescending(p => p.xp)
                .ToList();

            // =================================================
            // ASSIGN RANKS
            // =================================================

            int currentPlayerRank = -1;

            for (int i = 0; i < players.Count; i++)
            {
                int rank = i + 1;
                players[i].rank = rank;

                // Save ONLY the logged-in student's rank.
                if (players[i].userId == currentUser.UserId)
                {
                    currentPlayerRank = rank;
                }
            }

            // =================================================
            // SAVE CURRENT USER RANK ONLY
            // =================================================

            if (currentPlayerRank != -1)
            {
                await SavePlayerRank(currentUser.UserId, currentPlayerRank);
            }

            Debug.Log("Total users returned: " + snapshot.Count);
            Debug.Log("Students loaded: " + players.Count);

            // =================================================
            // DISPLAY LEADERBOARD
            // =================================================

            PopulateLeaderboard(players);

            // =================================================
            // CHECK RANK #1 BADGE
            // =================================================

            if (BadgeManager.Instance != null &&
                players.Count > 0 &&
                players[0].userId == currentUser.UserId)
            {
                BadgeManager.Instance.CheckRank1Badge();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Leaderboard Error: " + e.Message);
        }
    }

    // =====================================================
    // SAVE CURRENT PLAYER RANK
    // =====================================================

    private async Task SavePlayerRank(string userId, int rank)
    {
        try
        {
            DocumentReference userRef =
                db.Collection("users")
                  .Document(userId);

            Dictionary<string, object> rankData =
                new Dictionary<string, object>
                {
                    { "rank", rank }
                };

            await userRef.UpdateAsync(rankData);

            Debug.Log("Saved current player rank: " + rank);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save current player rank: " + e.Message);
        }
    }

    // =====================================================
    // POPULATE LEADERBOARD UI
    // =====================================================

    private void PopulateLeaderboard(List<LeaderboardPlayer> players)
    {
        if (content == null)
        {
            Debug.LogError("Leaderboard: Content is not assigned.");
            return;
        }

        // Clear previous entries.
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Create leaderboard entries.
        for (int i = 0; i < players.Count; i++)
        {
            int rank = i + 1;

            GameObject itemObject;

            if (rank == 1)
            {
                itemObject = Instantiate(top1Prefab, content);

                TopLeaderboardItem item =
                    itemObject.GetComponent<TopLeaderboardItem>();

                if (item != null)
                    item.Setup(players[i]);
            }
            else if (rank == 2)
            {
                itemObject = Instantiate(top2Prefab, content);

                TopLeaderboardItem item =
                    itemObject.GetComponent<TopLeaderboardItem>();

                if (item != null)
                    item.Setup(players[i]);
            }
            else if (rank == 3)
            {
                itemObject = Instantiate(top3Prefab, content);

                TopLeaderboardItem item =
                    itemObject.GetComponent<TopLeaderboardItem>();

                if (item != null)
                    item.Setup(players[i]);
            }
            else
            {
                itemObject = Instantiate(normalPrefab, content);

                NormalLeaderboardItem item =
                    itemObject.GetComponent<NormalLeaderboardItem>();

                if (item != null)
                    item.Setup(players[i], rank);
            }
        }
    }

    // =====================================================
    // MANUAL REFRESH
    // =====================================================

    public async void RefreshLeaderboard()
    {
        await LoadLeaderboard();
    }
}