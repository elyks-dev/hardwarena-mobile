using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;

public class Level3Manager : MonoBehaviour
{
    // =========================================================
    // START SCREEN
    // =========================================================

    [Header("Start Screen")]
    public GameObject startScreen;
    public Button startButton;

    // =========================================================
    // GUIDE POPUP
    // =========================================================

    [Header("Guide Popup")]
    public GameObject guideScreen;
    public Button closeGuideButton;

    // =========================================================
    // TIMER
    // =========================================================

    [Header("Timer")]
    public TMP_Text timerTMP;
    public float startingTime = 120f;

    private float remainingTime;
    private bool timerRunning = false;

    [Header("Timer Penalty")]
    public TMP_Text timePenaltyTMP;
    public float wrongAnswerPenalty = 5f;

    // =========================================================
    // FLASH SCREENS
    // =========================================================

    [Header("Feedback Screens")]
    public GameObject greenScreen;
    public GameObject redScreen;
    public float flashDuration = 0.25f;

    // =========================================================
    // QUESTIONS
    // =========================================================

    [System.Serializable]
    public class QuestionData
    {
        [Header("Question Screen")]
        public GameObject questionScreen;

        [Header("Input")]
        public TMP_InputField inputField;

        [Header("Submit Button")]
        public Button submitButton;

        [Header("Guide Button (This Question Only)")]
        public Button guideButton;

        [Header("Correct Binary Answer")]
        [TextArea]
        public string correctAnswer;
    }

    [Header("Questions")]
    public List<QuestionData> questions = new List<QuestionData>();

    private int currentQuestion = 0;
    private bool answering = false;

    // =========================================================
    // LEVEL COMPLETE
    // =========================================================

    [Header("Level Completion")]
    public GameObject levelCompletionScreen;
    public Button exitButton;

    // =========================================================
    // TRY AGAIN POPUP
    // =========================================================

    void ShowTryAgainPopup()
    {
        // Prevent duplicate popup calls.
        if (!timerRunning)
            return;

        timerRunning = false;
        answering = true;

        HideAllQuestions();

        if (guideScreen != null)
            guideScreen.SetActive(false);

        if (greenScreen != null)
            greenScreen.SetActive(false);

        if (redScreen != null)
            redScreen.SetActive(false);

        if (timePenaltyTMP != null)
            timePenaltyTMP.gameObject.SetActive(false);

        StopLevelMusic();

        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(true);
    }

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip level3BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    // =========================================================
    // FIREBASE
    // =========================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =========================================================
    // START
    // =========================================================

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnAudioSettingsChanged += ApplyVolumeSettings;
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnAudioSettingsChanged -= ApplyVolumeSettings;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        remainingTime = startingTime;
        timerRunning = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMainMenuMusic();

        ApplyVolumeSettings();
        PlayLevelMusic();

        greenScreen.SetActive(false);
        redScreen.SetActive(false);
        guideScreen.SetActive(false);
        levelCompletionScreen.SetActive(false);

        // Hide Try Again popup at the start.
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        // Try Again button listener.
        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(RestartLevel);
        }

        if (timePenaltyTMP != null)
        {
            timePenaltyTMP.gameObject.SetActive(false);
        }

        HideAllQuestions();

        startScreen.SetActive(true);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartGame);

        closeGuideButton.onClick.RemoveAllListeners();
        closeGuideButton.onClick.AddListener(CloseGuide);

        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(ExitLevel);

        StartCoroutine(TimerCountdown());
    }

    // =========================================================
    // START GAME
    // =========================================================

    public void StartGame()
    {
        startScreen.SetActive(false);
        currentQuestion = 0;
        ShowQuestion(currentQuestion);
    }

    // =========================================================
    // TIMER
    // =========================================================

    IEnumerator TimerCountdown()
    {
        while (timerRunning)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0)
            {
                remainingTime = 0;
                UpdateTimerText();

                ShowTryAgainPopup();
                yield break;
            }

            UpdateTimerText();
            yield return null;
        }
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerTMP.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void RestartLevel()
    {
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        timerRunning = false;
        answering = false;

        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =========================================================
    // QUESTIONS
    // =========================================================

    void HideAllQuestions()
    {
        foreach (QuestionData q in questions)
        {
            q.questionScreen.SetActive(false);

            if (q.inputField != null)
                q.inputField.text = "";

            if (q.guideButton != null)
                q.guideButton.gameObject.SetActive(false);
        }
    }
    void ShowQuestion(int index)
    {
        answering = false;

        HideAllQuestions();

        QuestionData question = questions[index];

        question.questionScreen.SetActive(true);
        question.inputField.text = "";

        // Submit Button
        question.submitButton.onClick.RemoveAllListeners();
        question.submitButton.onClick.AddListener(() => SubmitAnswer(index));

        // Guide Button for THIS question only
        if (question.guideButton != null)
        {
            question.guideButton.gameObject.SetActive(true);
            question.guideButton.onClick.RemoveAllListeners();
            question.guideButton.onClick.AddListener(OpenGuide);
        }
    }

    void SubmitAnswer(int index)
    {
        if (answering) return;

        answering = true;

        QuestionData question = questions[index];

        string playerAnswer = question.inputField.text.Trim().Replace(" ", "");
        string correctAnswer = question.correctAnswer.Trim().Replace(" ", "");

        if (playerAnswer.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(CorrectRoutine());
        }
        else
        {
            StartCoroutine(WrongRoutine(question));
        }
    }

    IEnumerator CorrectRoutine()
    {
        PlayCorrectSFX();

        greenScreen.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        greenScreen.SetActive(false);

        currentQuestion++;

        if (currentQuestion >= questions.Count)
        {
            FinishLevel();
        }
        else
        {
            ShowQuestion(currentQuestion);
        }
    }

    IEnumerator WrongRoutine(QuestionData question)
    {
        PlayWrongSFX();

        // Deduct time.
        remainingTime = Mathf.Max(0, remainingTime - wrongAnswerPenalty);
        UpdateTimerText();

        // Show "-5 SECONDS" message.
        if (timePenaltyTMP != null)
        {
            timePenaltyTMP.text = $"-{wrongAnswerPenalty:0} SECONDS";
            timePenaltyTMP.gameObject.SetActive(true);
        }

        redScreen.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        redScreen.SetActive(false);

        if (timePenaltyTMP != null)
            timePenaltyTMP.gameObject.SetActive(false);

        question.inputField.text = "";
        question.inputField.ActivateInputField();

        answering = false;

        // If penalty reduced timer to 0.
        if (remainingTime <= 0)
        {
            ShowTryAgainPopup();
        }
    }

    // =========================================================
    // GUIDE
    // =========================================================

    public void OpenGuide()
    {
        guideScreen.SetActive(true);

        if (questions[currentQuestion].guideButton != null)
            questions[currentQuestion].guideButton.gameObject.SetActive(false);
    }

    public void CloseGuide()
    {
        guideScreen.SetActive(false);

        if (questions[currentQuestion].guideButton != null)
            questions[currentQuestion].guideButton.gameObject.SetActive(true);
    }


    // =========================================================
    // FINISH LEVEL
    // =========================================================

    async void FinishLevel()
    {
        timerRunning = false;

        HideAllQuestions();

        levelCompletionScreen.SetActive(true);

        await SaveLevelCompletion();
    }

    // =========================================================
    // TRY AGAIN POPUP
    // =========================================================

    [Header("Try Again Popup")]
    public GameObject tryAgainPopup;
    public Button tryAgainButton;

    // =========================================================
    // AUDIO
    // =========================================================

    void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    void PlayLevelMusic()
    {
        if (bgmSource == null || level3BGMusic == null)
            return;

        bgmSource.clip = level3BGMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    void StopLevelMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    void PlayCorrectSFX()
    {
        if (sfxSource != null && correctSFX != null)
            sfxSource.PlayOneShot(correctSFX);
    }

    void PlayWrongSFX()
    {
        if (sfxSource != null && wrongSFX != null)
            sfxSource.PlayOneShot(wrongSFX);
    }

    // =========================================================
    // FIREBASE SAVE + XP
    // =========================================================

    async Task SaveLevelCompletion()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        DocumentReference userRef =
            db.Collection("users").Document(user.UserId);

        DocumentReference levelRef =
            userRef.Collection("progress").Document("level3");

        DocumentSnapshot levelSnapshot =
            await levelRef.GetSnapshotAsync();

        if (levelSnapshot.Exists &&
            levelSnapshot.TryGetValue("completed", out bool completed) &&
            completed)
        {
            return;
        }

        await levelRef.SetAsync(new Dictionary<string, object>()
        {
            { "completed", true },
            { "completedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);

        DocumentSnapshot userSnapshot =
            await userRef.GetSnapshotAsync();

        int currentXP = 0;

        if (userSnapshot.Exists &&
            userSnapshot.TryGetValue("xp", out int xp))
        {
            currentXP = xp;
        }

        await userRef.SetAsync(
        new Dictionary<string, object>()
        {
            { "xp", currentXP + 1000 }
        },
        SetOptions.MergeAll
    );

    // =====================================================
    // BADGES
    // =====================================================

    if (BadgeManager.Instance != null)
    {
        // Level 3 completed
        BadgeManager.Instance.LevelCompleted("level3");

        // XP-based badges
        BadgeManager.Instance.CheckXPBadges(
            currentXP + 1000
        );
    }
    }

    // =========================================================
    // EXIT LEVEL
    // =========================================================

    public void ExitLevel()
    {
        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        SceneManager.LoadScene("SelectLevelScene");
    }
}