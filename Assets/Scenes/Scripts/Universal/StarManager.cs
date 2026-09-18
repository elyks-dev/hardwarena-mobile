using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;

public class StarManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizStars
    {
        [Header("Firestore")]
        [Tooltip("Quiz document inside quizScores. Example: quiz1")]
        public string quizId;

        [Header("UI Stars")]
        public Image star1;
        public Image star2;
        public Image star3;
    }

    [Header("Quiz Star Displays")]
    public List<QuizStars> quizzes = new List<QuizStars>();

    [Header("Sprites")]
    public Sprite filledStar;
    public Sprite emptyStar;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private bool isDestroyed;

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        if (this == null)
            return;

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        await LoadAllStars();
    }

    // =====================================================
    // DESTROY
    // =====================================================

    private void OnDestroy()
    {
        isDestroyed = true;
    }

    // =====================================================
    // LOAD ALL QUIZ STARS
    // =====================================================

    private async Task LoadAllStars()
    {
        if (isDestroyed || this == null)
            return;

        if (auth == null)
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user found.");
            return;
        }

        if (db == null)
            return;

        DocumentReference userRef =
            db.Collection("users").Document(user.UserId);

        foreach (QuizStars quiz in quizzes)
        {
            // The object may have been destroyed while looping.
            if (isDestroyed || this == null)
                return;

            if (quiz == null)
                continue;

            // Default = Empty stars
            SetStars(quiz, 0);

            try
            {
                DocumentSnapshot quizDoc = await userRef
                    .Collection("quizScores")
                    .Document(quiz.quizId)
                    .GetSnapshotAsync();

                // IMPORTANT:
                // The scene may have been destroyed while Firestore
                // was loading.
                if (isDestroyed || this == null)
                    return;

                if (quizDoc == null || !quizDoc.Exists)
                    continue;

                int stars = 0;

                if (quizDoc.TryGetValue("stars", out int savedStars))
                {
                    stars = savedStars;
                }

                SetStars(quiz, stars);

                Debug.Log(
                    $"{quiz.quizId} loaded with {stars} star(s)."
                );
            }
            catch (System.Exception e)
            {
                // Don't report an error if the object was destroyed
                // during the async Firestore operation.
                if (isDestroyed || this == null)
                    return;

                Debug.LogError(
                    $"Error loading {quiz.quizId}: {e.Message}"
                );
            }
        }
    }

    // =====================================================
    // SET STAR SPRITES
    // =====================================================

    private void SetStars(QuizStars quiz, int stars)
    {
        if (isDestroyed || this == null)
            return;

        if (quiz == null)
            return;

        if (quiz.star1 != null)
        {
            quiz.star1.sprite =
                stars >= 1 ? filledStar : emptyStar;
        }

        if (quiz.star2 != null)
        {
            quiz.star2.sprite =
                stars >= 2 ? filledStar : emptyStar;
        }

        if (quiz.star3 != null)
        {
            quiz.star3.sprite =
                stars >= 3 ? filledStar : emptyStar;
        }
    }
}