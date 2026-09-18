using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;

public class LevelProgressSliderManager : MonoBehaviour
{
    // =====================================================
    // PROGRESS ELEMENT
    // =====================================================

    [Serializable]
    public class ProgressElement
    {
        [Header("Sliders")]
        public Slider[] sliders;

        [Header("Firebase IDs")]
        public string levelId;
        public string quizId;
    }

    // =====================================================
    // PROGRESS ELEMENTS
    // =====================================================

    [Header("Progress Elements")]
    [SerializeField] private ProgressElement[] elements;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // START
    // =====================================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        await LoadProgress();
    }

    // =====================================================
    // LOAD PROGRESS
    // =====================================================

    public async Task LoadProgress()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "LevelProgressSliderManager: No Firebase user logged in."
            );

            return;
        }

        if (elements == null || elements.Length == 0)
        {
            Debug.LogError(
                "LevelProgressSliderManager: No progress elements assigned."
            );

            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users")
                  .Document(user.UserId);

            foreach (ProgressElement element in elements)
            {
                if (element == null)
                    continue;

                if (element.sliders == null ||
                    element.sliders.Length == 0)
                {
                    Debug.LogWarning(
                        "No sliders assigned for " +
                        element.levelId
                    );

                    continue;
                }

                // =============================================
                // GET LEVEL
                // =============================================

                bool levelCompleted = false;

                DocumentSnapshot levelSnapshot =
                    await userRef
                        .Collection("progress")
                        .Document(element.levelId)
                        .GetSnapshotAsync();

                if (levelSnapshot.Exists &&
                    levelSnapshot.TryGetValue(
                        "completed",
                        out bool levelValue))
                {
                    levelCompleted = levelValue;
                }

                // =============================================
                // GET QUIZ
                // =============================================

                bool quizCompleted = false;

                DocumentSnapshot quizSnapshot =
                    await userRef
                        .Collection("quizScores")
                        .Document(element.quizId)
                        .GetSnapshotAsync();

                if (quizSnapshot.Exists)
                {
                    Debug.Log(
                        "Quiz document found: " +
                        element.quizId
                    );

                    if (quizSnapshot.TryGetValue(
                        "completed",
                        out bool quizValue))
                    {
                        quizCompleted = quizValue;

                        Debug.Log(
                            "Quiz completed = " +
                            quizCompleted
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "Quiz document exists but " +
                            "'completed' was not found."
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "Quiz document NOT found: " +
                        element.quizId
                    );
                }

                // =============================================
                // DETERMINE PROGRESS
                // =============================================

                float progress = 0f;

                if (quizCompleted)
                {
                    progress = 1f;
                }
                else if (levelCompleted)
                {
                    progress = 0.5f;
                }

                // =============================================
                // APPLY TO ALL SLIDERS
                // =============================================

                foreach (Slider slider in element.sliders)
                {
                    if (slider == null)
                        continue;

                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                    slider.value = progress;
                }

                Debug.Log(
                    "FINAL PROGRESS | " +
                    element.levelId +
                    " + " +
                    element.quizId +
                    " = " +
                    (progress * 100f) +
                    "%"
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "LevelProgressSliderManager Error: " +
                e.Message
            );
        }
    }

    // =====================================================
    // MANUAL REFRESH
    // =====================================================

    public async void RefreshProgress()
    {
        await LoadProgress();
    }
}