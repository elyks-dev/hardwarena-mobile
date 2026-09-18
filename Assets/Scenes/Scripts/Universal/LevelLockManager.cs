using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class LevelLockManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelUI
    {
        [Header("Firestore Quiz Key")]

        [Tooltip("Quiz document inside quizScores. Example: quiz1")]
        public string requiredQuizKey;

        [Header("UI")]

        [Tooltip("Lock image overlay for this level.")]
        public GameObject lockLayer;
    }

    [Header("Levels")]
    public List<LevelUI> levels = new List<LevelUI>();

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        await LoadLevelLocks();
    }

    // =====================================================
    // LOAD LEVEL LOCKS
    // =====================================================

    async Task LoadLevelLocks()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user found.");
            return;
        }

        DocumentReference userRef =
            db.Collection("users").Document(user.UserId);

        List<Task<DocumentSnapshot>> quizReadTasks =
            new List<Task<DocumentSnapshot>>();

        foreach (LevelUI level in levels)
        {
            if (level == null ||
                string.IsNullOrWhiteSpace(level.requiredQuizKey))
            {
                quizReadTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );

                continue;
            }

            quizReadTasks.Add(
                userRef
                    .Collection("quizScores")
                    .Document(level.requiredQuizKey)
                    .GetSnapshotAsync()
            );
        }

        DocumentSnapshot[] quizDocuments =
            await Task.WhenAll(quizReadTasks);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelUI level = levels[i];

            if (level == null ||
                level.lockLayer == null)
            {
                continue;
            }

            bool levelUnlocked =
                quizDocuments[i] != null &&
                quizDocuments[i].Exists;

            level.lockLayer.SetActive(!levelUnlocked);
        }
    }
}