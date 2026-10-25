using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        [Header("Question Screen")]
        public GameObject screen;

        [Header("Answer Buttons")]
        public Button buttonA;
        public Button buttonB;
        public Button buttonC;
        public Button buttonD;

        [Header("Answer Assignment")]
        public Button correctButton;

        [Header("Timer UI")]
        public TMP_Text timerTMP;
        public Slider timerSlider;
    }

    [Header("Guide Screens")]
    public GameObject screen1;
    public GameObject screen2;
    public GameObject screen3;
    public GameObject screen4;

    [Header("Loading")]
    public Button guide2Button;

    [Header("Countdown")]
    public GameObject countdownScreen;
    public TMP_Text countdownTMP;

    [Header("Questions")]
    public QuestionData[] questions;

    [Header("Result Screen")]
    public GameObject resultScreen;
    public TMP_Text messageTMP;
    public TMP_Text scoreTMP;
    public TMP_Text xpTMP;
    public Button retryButton;

    [Header("Exit Button Position After Retry")]
    public Button exitButton;
    [SerializeField] private Vector2 exitAfterRetryPosition;

    [Header("Star Images")]
    public Image star1;
    public Image star2;
    public Image star3;
    public Sprite starSprite;
    public Sprite noStarSprite;

    [Header("Result Image")]
    public Image resultImage;
    public Sprite failSprite;
    public Sprite passSprite;
    public Sprite perfectSprite;

    [Header("Audio")]
    public AudioSource bgMusicSource;
    public AudioSource sfxSource;

    public AudioClip guideMusic;
    public AudioClip countdownAudio;
    public AudioClip quizMusic;
    public AudioClip correctAudio;
    public AudioClip wrongAudio;
    public AudioClip resultsAudio;

    [Header("Settings")]
    public float questionTime = 60f;
    public int xpPerCorrect = 20;

    [Header("Quiz Progress (Firestore)")]
    public string quizId = "quiz1";

    private const int MaxAttempts = 2;

    private int passingScore =>
        questions == null
            ? 0
            : Mathf.FloorToInt(questions.Length * 0.75f);

    private int currentQuestion;
    private int score;
    private int attemptNumber = 1;
    private int attemptsUsed;

    private bool answered;
    private bool isSavingResult;
    private bool quizStateLoaded;
    private bool secondAttemptAlreadyCompleted;
    private bool retryInProgress;

    private Coroutine timerRoutine;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private readonly List<Dictionary<string, object>> currentAnswers =
        new List<Dictionary<string, object>>();

    private readonly Color correctColor = new Color(0.35f, 0.85f, 0.35f);
    private readonly Color wrongColor = new Color(0.95f, 0.35f, 0.35f);
    private readonly Color defaultColor = Color.white;

    // =========================================================
    // START
    // =========================================================

    async void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseBackgroundMusic();
            AudioManager.Instance.OnAudioSettingsChanged += ApplyQuizAudioSettings;
        }

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        ApplyQuizAudioSettings();

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryQuiz);
            retryButton.onClick.AddListener(RetryQuiz);
            retryButton.gameObject.SetActive(false);
        }

        if (questions != null)
        {
            foreach (QuestionData q in questions)
            {
                if (q == null)
                    continue;

                if (q.screen == null)
                    Debug.LogError("A question has no screen assigned.");

                if (q.correctButton == null)
                    Debug.LogError($"Correct button missing on {q.screen?.name}.");
            }
        }

        HideEverything();

        if (screen1 != null)
            screen1.SetActive(true);

        PlayGuideMusic();
        SetupButtons();

        // Block the first "Next" button until the saved quiz state is loaded.
        if (guide2Button != null)
            guide2Button.interactable = false;

        await LoadSavedQuizState();

        // Do not let the quiz be played again after both attempts.
        // QuizLockManager should normally prevent the scene from opening.
        if (secondAttemptAlreadyCompleted)
        {
            Debug.Log("Both attempts are complete. This quiz is locked.");
        }
    }

    void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -= ApplyQuizAudioSettings;
            AudioManager.Instance.ResumeBackgroundMusic();
        }
    }

    // =========================================================
    // AUDIO SETTINGS
    // =========================================================

    void ApplyQuizAudioSettings()
    {
        if (AudioManager.Instance == null)
            return;

        if (bgMusicSource != null)
        {
            bgMusicSource.volume = AudioManager.Instance.GetFinalMusicVolume();
            bgMusicSource.mute = !AudioManager.Instance.IsMusicEnabled();
        }

        if (sfxSource != null)
        {
            sfxSource.volume = AudioManager.Instance.GetFinalSFXVolume();
            sfxSource.mute = !AudioManager.Instance.IsSFXEnabled();
        }
    }

    // =========================================================
    // LOAD SAVED STATE
    // =========================================================

    async Task LoadSavedQuizState()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("Cannot load quiz state. No user is logged in.");
            return;
        }

        try
        {
            DocumentReference quizRef = db
                .Collection("users")
                .Document(user.UserId)
                .Collection("quizScores")
                .Document(quizId);

            DocumentSnapshot quizSnapshot = await quizRef.GetSnapshotAsync();

            DocumentReference attempt1Ref =
                quizRef.Collection("attempts").Document("attempt1");

            DocumentReference attempt2Ref =
                quizRef.Collection("attempts").Document("attempt2");

            DocumentSnapshot attempt1Snapshot =
                await attempt1Ref.GetSnapshotAsync();

            DocumentSnapshot attempt2Snapshot =
                await attempt2Ref.GetSnapshotAsync();

            if (!this)
                return;

            bool attempt1Completed = attempt1Snapshot.Exists;
            bool attempt2Completed = attempt2Snapshot.Exists;

            secondAttemptAlreadyCompleted = attempt2Completed;

            if (attempt2Completed)
            {
                attemptsUsed = 2;
            }
            else if (attempt1Completed)
            {
                attemptsUsed = 1;
            }
            else if (quizSnapshot.Exists)
            {
                // Compatibility with older records that only saved the
                // main quiz document.
                int savedAttempts = 0;

                if (quizSnapshot.TryGetValue("attemptsUsed", out int storedAttempts))
                    savedAttempts = storedAttempts;

                attemptsUsed = Mathf.Clamp(savedAttempts, 0, MaxAttempts);
            }
            else
            {
                attemptsUsed = 0;
            }

            // The next attempt is based on completed attempts, not on
            // whether the player clicked the Retry button.
            attemptNumber = attemptsUsed + 1;

            quizStateLoaded = true;

            // Loading finished, so the first "Next" button can be used.
            if (guide2Button != null)
                guide2Button.interactable = true;

            // IMPORTANT:
            // Never restore the old result screen when the quiz opens.
            // Guide Screen 1 remains the normal entry point.
            HideEverything();

            if (screen1 != null && attemptsUsed < MaxAttempts)
                screen1.SetActive(true);

            if (attemptsUsed >= MaxAttempts)
            {
                Debug.Log("Quiz has reached the two-attempt limit.");
            }

            Debug.Log(
                $"Loaded {quizId}. Attempts used: {attemptsUsed}. " +
                $"Next attempt: {attemptNumber}. " +
                $"Attempt 2 completed: {secondAttemptAlreadyCompleted}."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load quiz state: " + e);
            // TODO: show a "No internet / try again" popup here.
        }
    }

    // =========================================================
    // HIDE EVERYTHING
    // =========================================================

    void HideEverything()
    {
        if (screen1 != null) screen1.SetActive(false);
        if (screen2 != null) screen2.SetActive(false);
        if (screen3 != null) screen3.SetActive(false);
        if (screen4 != null) screen4.SetActive(false);

        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        if (resultScreen != null)
            resultScreen.SetActive(false);

        if (questions != null)
        {
            foreach (QuestionData q in questions)
            {
                if (q != null && q.screen != null)
                    q.screen.SetActive(false);
            }
        }
    }

    // =========================================================
    // GUIDE NAVIGATION
    // =========================================================

    public void Guide2()
    {
        if (!CanEnterQuiz())
            return;

        if (screen1 != null) screen1.SetActive(false);
        if (screen2 != null) screen2.SetActive(true);
    }

    public void BackGuide1()
    {
        if (screen2 != null) screen2.SetActive(false);
        if (screen1 != null) screen1.SetActive(true);
    }

    public void Guide3()
    {
        if (screen2 != null) screen2.SetActive(false);
        if (screen3 != null) screen3.SetActive(true);
    }

    public void BackGuide2()
    {
        if (screen3 != null) screen3.SetActive(false);
        if (screen2 != null) screen2.SetActive(true);
    }

    public void Guide4()
    {
        if (!CanEnterQuiz())
            return;

        if (screen3 != null) screen3.SetActive(false);
        if (screen4 != null) screen4.SetActive(true);
    }

    public void BackGuide3()
    {
        if (screen4 != null) screen4.SetActive(false);
        if (screen3 != null) screen3.SetActive(true);
    }

    bool CanEnterQuiz()
    {
        if (!quizStateLoaded)
        {
            Debug.LogWarning("Quiz state is still loading.");
            return false;
        }

        if (attemptsUsed >= MaxAttempts || secondAttemptAlreadyCompleted)
        {
            Debug.LogWarning("Both quiz attempts have already been completed.");
            return false;
        }

        return true;
    }

    // =========================================================
    // START QUIZ
    // =========================================================

    public void StartQuiz()
    {
        if (!quizStateLoaded)
        {
            Debug.LogWarning("Quiz state is not loaded yet.");
            return;
        }

        if (questions == null || questions.Length == 0)
        {
            Debug.LogError("No questions are assigned to this quiz.");
            return;
        }

        if (!CanEnterQuiz())
            return;

        // Determine the attempt from completed attempts.
        // Attempt 2 is available even if Retry was never clicked.
        attemptNumber = attemptsUsed + 1;

        if (screen4 != null)
            screen4.SetActive(false);

        StopAllCoroutines();
        timerRoutine = null;

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        if (countdownScreen != null)
            countdownScreen.SetActive(true);

        if (bgMusicSource != null)
            bgMusicSource.Stop();

        if (sfxSource != null && countdownAudio != null)
            sfxSource.PlayOneShot(countdownAudio);

        string[] countdown = { "3", "2", "1", "START!" };

        foreach (string value in countdown)
        {
            if (countdownTMP != null)
                countdownTMP.text = value;

            yield return new WaitForSeconds(1f);
        }

        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        score = 0;
        currentQuestion = 0;
        answered = false;
        currentAnswers.Clear();

        ShowQuestion(currentQuestion);
    }

    // =========================================================
    // BUTTON SETUP
    // =========================================================

    void SetupButtons()
    {
        if (questions == null)
            return;

        foreach (QuestionData q in questions)
        {
            if (q == null)
                continue;

            QuestionData localQuestion = q;

            Button a = localQuestion.buttonA;
            Button b = localQuestion.buttonB;
            Button c = localQuestion.buttonC;
            Button d = localQuestion.buttonD;

            if (a == null || b == null || c == null || d == null)
            {
                Debug.LogError($"Answer buttons are missing on {localQuestion.screen?.name}.");
                continue;
            }

            a.onClick.RemoveAllListeners();
            b.onClick.RemoveAllListeners();
            c.onClick.RemoveAllListeners();
            d.onClick.RemoveAllListeners();

            a.onClick.AddListener(() => Answer(localQuestion, a));
            b.onClick.AddListener(() => Answer(localQuestion, b));
            c.onClick.AddListener(() => Answer(localQuestion, c));
            d.onClick.AddListener(() => Answer(localQuestion, d));

            CheckButtonOverlap(localQuestion);
        }
    }

    // =========================================================
    // SHOW QUESTION
    // =========================================================

    void ShowQuestion(int index)
    {
        if (index < 0 || index >= questions.Length)
        {
            Debug.LogError("Invalid question index: " + index);
            return;
        }

        if (bgMusicSource != null)
        {
            bgMusicSource.clip = quizMusic;
            bgMusicSource.loop = true;

            ApplyQuizAudioSettings();

            if (AudioManager.Instance == null ||
                AudioManager.Instance.IsMusicEnabled())
            {
                bgMusicSource.Play();
            }
        }

        QuestionData q = questions[index];

        q.screen.SetActive(true);
        answered = false;
        ResetButtons(q);

        timerRoutine = StartCoroutine(StartTimer(q));
    }

    // =========================================================
    // TIMER
    // =========================================================

    IEnumerator StartTimer(QuestionData q)
    {
        float timer = questionTime;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            if (q.timerSlider != null)
                q.timerSlider.value = Mathf.Clamp01(timer / questionTime);

            if (q.timerTMP != null)
                q.timerTMP.text = Mathf.Ceil(timer) + "s";

            yield return null;
        }

        if (!answered)
            Answer(q, null);
    }

    // =========================================================
    // ANSWER
    // =========================================================

    void Answer(QuestionData q, Button selectedButton)
    {
        if (answered)
            return;

        answered = true;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (q.correctButton == null)
        {
            Debug.LogError($"Correct button is missing for {q.screen.name}.");
            StartCoroutine(NextQuestion(q));
            return;
        }

        Button[] buttons = { q.buttonA, q.buttonB, q.buttonC, q.buttonD };

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            ColorBlock colors = button.colors;
            colors.normalColor = button == q.correctButton ? correctColor : wrongColor;
            colors.highlightedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = colors.normalColor;
            button.colors = colors;
        }

        bool isCorrect = selectedButton == q.correctButton;

        currentAnswers.Add(new Dictionary<string, object>
        {
            { "questionNumber", currentQuestion + 1 },
            { "selectedAnswer", GetAnswerLetter(q, selectedButton) },
            { "correctAnswer", GetAnswerLetter(q, q.correctButton) },
            { "isCorrect", isCorrect },
            { "timeExpired", selectedButton == null }
        });

        if (isCorrect)
        {
            score++;

            if (sfxSource != null && correctAudio != null)
                sfxSource.PlayOneShot(correctAudio);
        }
        else
        {
            if (sfxSource != null && wrongAudio != null)
                sfxSource.PlayOneShot(wrongAudio);
        }

        StartCoroutine(NextQuestion(q));
    }

    string GetAnswerLetter(QuestionData q, Button button)
    {
        if (button == null) return null;
        if (button == q.buttonA) return "A";
        if (button == q.buttonB) return "B";
        if (button == q.buttonC) return "C";
        if (button == q.buttonD) return "D";
        return null;
    }

    void CheckButtonOverlap(QuestionData q)
    {
        Button[] buttons = { q.buttonA, q.buttonB, q.buttonC, q.buttonD };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            RectTransform rtA = buttons[i].GetComponent<RectTransform>();

            for (int j = i + 1; j < buttons.Length; j++)
            {
                if (buttons[j] == null)
                    continue;

                RectTransform rtB = buttons[j].GetComponent<RectTransform>();

                if (rtA != null && rtB != null &&
                    rtA.anchoredPosition == rtB.anchoredPosition)
                {
                    Debug.LogWarning(
                        $"OVERLAP DETECTED: {buttons[i].name} overlaps {buttons[j].name}"
                    );
                }
            }
        }
    }

    // =========================================================
    // NEXT QUESTION
    // =========================================================

    IEnumerator NextQuestion(QuestionData q)
    {
        yield return new WaitForSeconds(1.5f);

        q.screen.SetActive(false);
        currentQuestion++;

        if (currentQuestion < questions.Length)
            ShowQuestion(currentQuestion);
        else
            ShowResults();
    }

    // =========================================================
    // RESET BUTTONS
    // =========================================================

    void ResetButtons(QuestionData q)
    {
        Button[] buttons = { q.buttonA, q.buttonB, q.buttonC, q.buttonD };

        foreach (Button b in buttons)
        {
            if (b == null)
                continue;

            ColorBlock colors = b.colors;
            colors.normalColor = defaultColor;
            colors.highlightedColor = defaultColor;
            colors.selectedColor = defaultColor;
            colors.pressedColor = defaultColor;
            b.colors = colors;
        }

        if (q.timerSlider != null)
            q.timerSlider.value = 1f;

        if (q.timerTMP != null)
            q.timerTMP.text = questionTime.ToString("0") + "s";
    }

    // =========================================================
    // RESULTS
    // =========================================================

    async void ShowResults()
    {
        if (bgMusicSource != null)
            bgMusicSource.Stop();

        if (sfxSource != null && resultsAudio != null)
            sfxSource.PlayOneShot(resultsAudio);

        if (resultScreen != null)
            resultScreen.SetActive(true);

        int attemptXP = score * xpPerCorrect;

        if (scoreTMP != null)
            scoreTMP.text = score + "/" + questions.Length;

        if (xpTMP != null)
            xpTMP.text = "+ " + attemptXP + " XP";

        if (messageTMP != null && resultImage != null)
        {
            if (score == questions.Length)
            {
                messageTMP.text = "GREAT WORK!";
                resultImage.sprite = perfectSprite;
            }
            else if (score >= passingScore)
            {
                messageTMP.text = "NICE!";
                resultImage.sprite = passSprite;
            }
            else
            {
                messageTMP.text = "BETTER LUCK NEXT TIME!";
                resultImage.sprite = failSprite;
            }
        }

        UpdateStars();

        isSavingResult = true;

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
            retryButton.interactable = false;
        }

        await SaveQuizProgress(attemptXP);

        isSavingResult = false;

        bool canRetry = attemptsUsed < MaxAttempts &&
                        !secondAttemptAlreadyCompleted;

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(canRetry);
            retryButton.interactable = canRetry;
        }

        if (attemptsUsed >= MaxAttempts)
            SetExitButtonPosition();
    }

    // =========================================================
    // EXIT BUTTON POSITION
    // =========================================================

    void SetExitButtonPosition()
    {
        if (exitButton == null)
            return;

        RectTransform rectTransform = exitButton.GetComponent<RectTransform>();

        if (rectTransform != null)
            rectTransform.anchoredPosition = exitAfterRetryPosition;
    }

    // =========================================================
    // STARS
    // =========================================================

    void UpdateStars()
    {
        if (star1 != null) star1.sprite = noStarSprite;
        if (star2 != null) star2.sprite = noStarSprite;
        if (star3 != null) star3.sprite = noStarSprite;

        int stars = CalculateStars(score);

        if (stars >= 1 && star1 != null) star1.sprite = starSprite;
        if (stars >= 2 && star2 != null) star2.sprite = starSprite;
        if (stars >= 3 && star3 != null) star3.sprite = starSprite;
    }

    int CalculateStars(int resultScore)
    {
        if (resultScore == questions.Length) return 3;
        if (resultScore >= passingScore) return 2;
        if (resultScore > 0) return 1;
        return 0;
    }

    // =========================================================
    // RETRY BUTTON
    // Optional shortcut to begin Attempt 2.
    // Attempt 2 is also available after reopening the quiz.
    // =========================================================

    public void RetryQuiz()
    {
        if (retryInProgress || isSavingResult)
            return;

        if (!quizStateLoaded || attemptsUsed >= MaxAttempts ||
            secondAttemptAlreadyCompleted)
        {
            Debug.LogWarning("No attempts are available.");
            return;
        }

        retryInProgress = true;

        attemptNumber = attemptsUsed + 1;

        if (attemptNumber > MaxAttempts)
        {
            retryInProgress = false;
            return;
        }

        if (retryButton != null)
        {
            retryButton.interactable = false;
            retryButton.gameObject.SetActive(false);
        }

        currentQuestion = 0;
        score = 0;
        answered = false;
        currentAnswers.Clear();

        StopAllCoroutines();
        timerRoutine = null;

        HideEverything();

        if (screen1 != null)
            screen1.SetActive(true);

        PlayGuideMusic();

        retryInProgress = false;
    }

    // =========================================================
    // SAVE ATTEMPT AND BEST SCORE
    // =========================================================

    async Task SaveQuizProgress(int earnedXP)
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user logged in.");
            return;
        }

        // Verify the attempt number immediately before saving.
        attemptNumber = attemptsUsed + 1;

        if (attemptNumber < 1 || attemptNumber > MaxAttempts)
        {
            Debug.LogWarning("Attempt limit reached. Result was not saved.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        DocumentReference quizRef =
            userRef.Collection("quizScores").Document(quizId);

        DocumentReference attemptRef =
            quizRef.Collection("attempts").Document("attempt" + attemptNumber);

        bool passed = score >= passingScore;
        int stars = CalculateStars(score);

        List<Dictionary<string, object>> answerCopy =
            new List<Dictionary<string, object>>();

        foreach (Dictionary<string, object> answer in currentAnswers)
            answerCopy.Add(new Dictionary<string, object>(answer));

        Dictionary<string, object> attemptData =
            new Dictionary<string, object>
            {
                { "attemptNumber", attemptNumber },
                { "score", score },
                { "totalQuestions", questions.Length },
                { "passed", passed },
                { "stars", stars },
                { "earnedXP", earnedXP },
                { "answers", answerCopy },
                { "completed", true },
                { "completedAt", Timestamp.GetCurrentTimestamp() }
            };

        try
        {
            int additionalXP = await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot quizSnapshot =
                    await transaction.GetSnapshotAsync(quizRef);

                DocumentSnapshot attemptSnapshot =
                    await transaction.GetSnapshotAsync(attemptRef);

                DocumentSnapshot userSnapshot =
                    await transaction.GetSnapshotAsync(userRef);

                // Never overwrite an already-saved attempt.
                if (attemptSnapshot.Exists)
                    return 0;

                int oldBestScore = -1;
                int oldBestXP = 0;
                int oldBestAttempt = 1;
                int oldAttemptsUsed = 0;
                int currentXP = 0;

                if (quizSnapshot.Exists)
                {
                    if (quizSnapshot.TryGetValue("score", out int storedScore))
                        oldBestScore = storedScore;

                    if (quizSnapshot.TryGetValue("awardedXP", out int storedAwardedXP))
                        oldBestXP = storedAwardedXP;
                    else if (oldBestScore >= 0)
                        oldBestXP = oldBestScore * xpPerCorrect;

                    if (quizSnapshot.TryGetValue("bestAttempt", out int storedBestAttempt))
                        oldBestAttempt = storedBestAttempt;

                    if (quizSnapshot.TryGetValue("attemptsUsed", out int storedAttempts))
                        oldAttemptsUsed = storedAttempts;
                }

                if (userSnapshot.Exists)
                {
                    if (userSnapshot.TryGetValue("xp", out long storedXP))
                        currentXP = (int)storedXP;
                    else if (userSnapshot.TryGetValue("xp", out int storedXPInt))
                        currentXP = storedXPInt;
                }

                // Highest score from either attempt wins.
                bool isNewBest = oldBestScore < 0 || score > oldBestScore;

                int bestScore = Mathf.Max(oldBestScore, score);
                int bestXP = bestScore * xpPerCorrect;

                // Award only the increase in the best-result XP reward.
                // A lower score never removes XP.
                int xpDifference = Mathf.Max(0, bestXP - oldBestXP);

                int bestAttempt = isNewBest ? attemptNumber : oldBestAttempt;
                int completedAttempts = Mathf.Max(oldAttemptsUsed, attemptNumber);

                Dictionary<string, object> officialQuizData =
                    new Dictionary<string, object>
                    {
                        { "completed", completedAttempts >= MaxAttempts },
                        { "score", bestScore },
                        { "bestScore", bestScore },
                        { "totalQuestions", questions.Length },
                        { "passed", bestScore >= passingScore },
                        { "stars", CalculateStars(bestScore) },
                        { "earnedXP", bestXP },
                        { "awardedXP", bestXP },
                        { "bestAttempt", bestAttempt },
                        { "attemptsUsed", completedAttempts },
                        { "retryUsed", completedAttempts >= 2 },
                        { "lastAttemptAt", Timestamp.GetCurrentTimestamp() },
                        { "completedAt", Timestamp.GetCurrentTimestamp() }
                    };

                transaction.Set(attemptRef, attemptData);
                transaction.Set(quizRef, officialQuizData, SetOptions.MergeAll);

                if (xpDifference > 0)
                {
                    transaction.Update(
                        userRef,
                        new Dictionary<string, object>
                        {
                            { "xp", currentXP + xpDifference }
                        });
                }

                return xpDifference;
            });

            // Refresh local state after a successful save.
            attemptsUsed = Mathf.Max(attemptsUsed, attemptNumber);
            secondAttemptAlreadyCompleted = attemptsUsed >= MaxAttempts;

            Debug.Log(
                $"Saved {quizId} attempt {attemptNumber}. " +
                $"Score: {score}/{questions.Length}. " +
                $"Best score saved. Additional XP awarded: {additionalXP}."
            );

            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.QuizCompleted(quizId);

                if (additionalXP > 0)
                {
                    DocumentSnapshot updatedUser = await userRef.GetSnapshotAsync();
                    int updatedXP = 0;

                    if (updatedUser.Exists)
                    {
                        if (updatedUser.TryGetValue("xp", out long xpLong))
                            updatedXP = (int)xpLong;
                        else if (updatedUser.TryGetValue("xp", out int xpInt))
                            updatedXP = xpInt;
                    }

                    BadgeManager.Instance.CheckXPBadges(updatedXP);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Quiz Save Error: " + e);
        }
    }

    // =========================================================
    // AUDIO
    // =========================================================

    void PlayGuideMusic()
    {
        if (bgMusicSource == null)
            return;

        ApplyQuizAudioSettings();

        bgMusicSource.clip = guideMusic;
        bgMusicSource.loop = true;

        if (AudioManager.Instance == null ||
            AudioManager.Instance.IsMusicEnabled())
        {
            bgMusicSource.Play();
        }
    }

    // =========================================================
    // EXIT QUIZ
    // =========================================================

    public void BackToLevels()
    {
        if (bgMusicSource != null)
            bgMusicSource.Stop();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeBackgroundMusic();

        SceneManager.LoadScene("SelectLevelScene");
    }
}