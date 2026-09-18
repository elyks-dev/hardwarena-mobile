using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class QuizLockManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizUI
    {
        [Header("Progress Document")]
        [Tooltip("Document inside users/{uid}/progress. Example: level1")]
        public string levelCompletedKey;

        [Header("Quiz Score Document")]
        [Tooltip("Document inside users/{uid}/quizScores. Example: quiz1")]
        public string quizCompletedKey;

        [Header("UI Layers")]
        public GameObject lockLayer;
        public GameObject doneLayer;
    }

    [Header("Quiz List")]
    public List<QuizUI> quizzes = new List<QuizUI>();

    FirebaseFirestore db;
    FirebaseAuth auth;
    private bool isDestroyed = false;

    async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        await LoadQuizStates();
    }

    async Task LoadQuizStates()
    {
        if (isDestroyed || this == null)
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user found.");
            return;
        }

        DocumentReference userRef =
            db.Collection("users").Document(user.UserId);

        List<Task<DocumentSnapshot>> progressTasks =
            new List<Task<DocumentSnapshot>>();

        List<Task<DocumentSnapshot>> quizTasks =
            new List<Task<DocumentSnapshot>>();

        foreach (QuizUI quiz in quizzes)
        {
            if (quiz == null)
            {
                progressTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );

                quizTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(quiz.levelCompletedKey))
            {
                progressTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );
            }
            else
            {
                Debug.Log($"Checking {quiz.levelCompletedKey}...");

                progressTasks.Add(
                    userRef
                        .Collection("progress")
                        .Document(quiz.levelCompletedKey)
                        .GetSnapshotAsync()
                );
            }

            if (string.IsNullOrWhiteSpace(quiz.quizCompletedKey))
            {
                quizTasks.Add(
                    Task.FromResult<DocumentSnapshot>(null)
                );
            }
            else
            {
                quizTasks.Add(
                    userRef
                        .Collection("quizScores")
                        .Document(quiz.quizCompletedKey)
                        .GetSnapshotAsync()
                );
            }
        }

        try
        {
            List<Task<DocumentSnapshot>> allReadTasks =
                new List<Task<DocumentSnapshot>>();

            allReadTasks.AddRange(progressTasks);
            allReadTasks.AddRange(quizTasks);

            DocumentSnapshot[] allDocuments =
                await Task.WhenAll(allReadTasks);

            if (isDestroyed || this == null)
                return;

            for (int i = 0; i < quizzes.Count; i++)
            {
                if (isDestroyed || this == null)
                    return;

                QuizUI quiz = quizzes[i];

                if (quiz == null)
                    continue;

                bool levelCompleted = false;
                DocumentSnapshot progressDoc =
                    allDocuments[i];

                if (progressDoc != null &&
                    progressDoc.Exists &&
                    progressDoc.TryGetValue(
                        "completed",
                        out bool completed))
                {
                    levelCompleted = completed;
                }

                if (levelCompleted)
                {
                    Debug.Log($"{quiz.levelCompletedKey} unlocked.");
                }
                else
                {
                    Debug.Log($"{quiz.levelCompletedKey} still locked.");
                }

                DocumentSnapshot quizDoc =
                    allDocuments[progressTasks.Count + i];

                bool quizCompleted =
                    quizDoc != null &&
                    quizDoc.Exists;

                if (quizCompleted)
                {
                    Debug.Log($"{quiz.quizCompletedKey} completed.");
                }

                if (quiz.lockLayer != null)
                {
                    quiz.lockLayer.SetActive(
                        !levelCompleted && !quizCompleted
                    );
                }

                if (quiz.doneLayer != null)
                {
                    quiz.doneLayer.SetActive(quizCompleted);
                }
            }
        }
        catch (System.Exception e)
        {
            if (isDestroyed || this == null)
                return;

            Debug.LogError(
                "QuizLockManager: Failed to load quiz states. " +
                e.Message
            );
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
    }
}
