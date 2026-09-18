using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;

public class Level4Manager : MonoBehaviour
{
    // =========================================================
    // INTRO SCREEN
    // =========================================================

    [Header("Intro Screen")]
    public GameObject introScreen;
    public Button introNextButton;

    // =========================================================
    // TIMER
    // =========================================================

    [Header("Timer")]
    public TMP_Text timerTMP;
    public float levelTime = 90f;

    private float remainingTime;
    private bool timerRunning = false;

    // =========================================================
    // QUESTION DATA
    // =========================================================

    public enum CorrectChoice
    {
        Choice1,
        Choice2,
        Choice3
    }

    [System.Serializable]
    public class QuestionData
    {
        [Header("Question Screen")]
        public GameObject questionScreen;

        [Header("Text Objects")]
        public TMP_Text questionTMP;
        public TMP_Text correctTMP;
        public TMP_Text incorrectTMP;

        [Header("Choice Buttons")]
        public Button choiceButton1;
        public Button choiceButton2;
        public Button choiceButton3;

        [Header("Next Button")]
        public Button nextButton;

        [Header("Correct Answer")]
        public CorrectChoice correctChoice;
    }

    [Header("Questions (9)")]
    public List<QuestionData> questions = new List<QuestionData>();

    private int currentQuestionIndex = 0;
    private bool answered = false;

    // =========================================================
    // LEVEL COMPLETION
    // =========================================================

    [Header("Level Completion")]
    public GameObject levelCompletionScreen;

    // =========================================================
    // TRY AGAIN POPUP
    // =========================================================

    [Header("Try Again Popup")]
    public GameObject tryAgainPopup;
    public Button tryAgainButton;

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 4 Audio Clips")]
    public AudioClip level4BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    // =========================================================
    // FIREBASE
    // =========================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =========================================================
    // AUDIO EVENTS
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

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        foreach (QuestionData question in questions)
        {
            question.questionScreen.SetActive(false);

            if (question.questionTMP != null)
                question.questionTMP.gameObject.SetActive(true);

            if (question.correctTMP != null)
                question.correctTMP.gameObject.SetActive(false);

            if (question.incorrectTMP != null)
                question.incorrectTMP.gameObject.SetActive(false);

            if (question.nextButton != null)
                question.nextButton.gameObject.SetActive(false);
        }

        levelCompletionScreen.SetActive(false);
        introScreen.SetActive(true);

        // Try Again Popup
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(RestartLevel);
        }

        introNextButton.onClick.RemoveAllListeners();
        introNextButton.onClick.AddListener(StartLevel);

        remainingTime = Mathf.Max(0f, levelTime);
        UpdateTimerDisplay();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMainMenuMusic();

        ApplyVolumeSettings();
        PlayLevelMusic();
    }

    // =========================================================
    // UPDATE TIMER
    // =========================================================

    private void Update()
    {
        if (!timerRunning)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimerDisplay();
            ShowTryAgainPopup();
            return;
        }

        UpdateTimerDisplay();
    }

    // =========================================================
    // START LEVEL
    // =========================================================

    private void StartLevel()
    {
        introScreen.SetActive(false);
        currentQuestionIndex = 0;
        remainingTime = Mathf.Max(0f, levelTime);
        timerRunning = true;
        UpdateTimerDisplay();
        ShowQuestion(currentQuestionIndex);
    }

    private void StopTimer()
    {
        timerRunning = false;
    }

    private void UpdateTimerDisplay()
    {
        if (timerTMP == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerTMP.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // =========================================================
    // TRY AGAIN POPUP
    // =========================================================

    void ShowTryAgainPopup()
    {
        // Prevent duplicate popup calls.
        if (!timerRunning)
            return;

        timerRunning = false;
        answered = true;

        // Hide current question.
        if (currentQuestionIndex < questions.Count)
            questions[currentQuestionIndex].questionScreen.SetActive(false);

        // Hide intro/completion screens if visible.
        if (introScreen != null)
            introScreen.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        // Stop background music.
        StopLevelMusic();

        // Show popup.
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(true);
    }

    public void RestartLevel()
    {
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        timerRunning = false;
        answered = false;

        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
        // =========================================================
    // SHOW QUESTION
    // =========================================================

    private void ShowQuestion(int index)
    {
        QuestionData question = questions[index];
        answered = false;

        question.questionScreen.SetActive(true);

        question.questionTMP.gameObject.SetActive(true);
        question.correctTMP.gameObject.SetActive(false);
        question.incorrectTMP.gameObject.SetActive(false);

        question.nextButton.gameObject.SetActive(false);
        question.nextButton.onClick.RemoveAllListeners();
        question.nextButton.onClick.AddListener(NextQuestion);

        Button[] buttons =
        {
            question.choiceButton1,
            question.choiceButton2,
            question.choiceButton3
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            int buttonIndex = i;

            buttons[i].interactable = true;

            ColorBlock colors = buttons[i].colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.disabledColor = Color.white;
            buttons[i].colors = colors;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => SelectAnswer(buttonIndex));
        }
    }

    // =========================================================
    // ANSWER LOGIC
    // =========================================================

    private void SelectAnswer(int selectedIndex)
    {
        if (answered) return;

        answered = true;

        QuestionData question = questions[currentQuestionIndex];

        Button[] buttons =
        {
            question.choiceButton1,
            question.choiceButton2,
            question.choiceButton3
        };

        int correctIndex = (int)question.correctChoice;

        foreach (Button button in buttons)
            button.interactable = false;

        question.questionTMP.gameObject.SetActive(false);

        if (selectedIndex == correctIndex)
        {
            SetButtonColor(buttons[correctIndex], Color.green);
            question.correctTMP.gameObject.SetActive(true);

            PlayCorrectSFX();
        }
        else
        {
            SetButtonColor(buttons[selectedIndex], Color.red);
            SetButtonColor(buttons[correctIndex], Color.green);
            question.incorrectTMP.gameObject.SetActive(true);

            PlayWrongSFX();
        }

        question.nextButton.gameObject.SetActive(true);
    }

    // =========================================================
    // NEXT QUESTION
    // =========================================================

    public void NextQuestion()
    {
        questions[currentQuestionIndex].questionScreen.SetActive(false);

        currentQuestionIndex++;

        if (currentQuestionIndex >= questions.Count)
        {
            StopTimer();
            levelCompletionScreen.SetActive(true);
        }
        else
        {
            ShowQuestion(currentQuestionIndex);
        }
    }

    // =========================================================
    // BUTTON COLOR HELPER
    // =========================================================

    private void SetButtonColor(Button button, Color color)
    {
        ColorBlock colors = button.colors;

        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.selectedColor = color;
        colors.pressedColor = color;
        colors.disabledColor = color;

        button.colors = colors;
    }

    // =========================================================
    // AUDIO METHODS
    // =========================================================

    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    private void PlayLevelMusic()
    {
        if (bgmSource == null || level4BGMusic == null)
            return;

        if (bgmSource.clip == level4BGMusic && bgmSource.isPlaying)
            return;

        bgmSource.clip = level4BGMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void StopLevelMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    private void PlayCorrectSFX()
    {
        if (sfxSource == null || correctSFX == null)
            return;

        sfxSource.PlayOneShot(correctSFX);
    }

    private void PlayWrongSFX()
    {
        if (sfxSource == null || wrongSFX == null)
            return;

        sfxSource.PlayOneShot(wrongSFX);
    }
        // =========================================================
    // EXIT LEVEL
    // =========================================================

    public async void ExitLevel()
    {
        StopTimer();
        StopLevelMusic();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMainMenuMusic();
        }

        await SaveLevelCompletion();

        SceneManager.LoadScene("SelectLevelScene");
    }

    // =========================================================
    // SAVE LEVEL COMPLETION + XP
    // =========================================================

    private async Task SaveLevelCompletion()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user logged in.");
            return;
        }

        try
        {
            DocumentReference userRef = db
                .Collection("users")
                .Document(user.UserId);

            DocumentReference levelRef = userRef
                .Collection("progress")
                .Document("level4");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            // Prevent duplicate XP
            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log("Level 4 already completed.");
                return;
            }

            Dictionary<string, object> levelData =
                new Dictionary<string, object>()
                {
                    { "completed", true },
                    { "completedAt", FieldValue.ServerTimestamp }
                };

            await levelRef.SetAsync(levelData, SetOptions.MergeAll);

            DocumentSnapshot userSnapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (userSnapshot.Exists &&
                userSnapshot.TryGetValue("xp", out int xp))
            {
                currentXP = xp;
            }

            await userRef.UpdateAsync(new Dictionary<string, object>()
            {
                { "xp", currentXP + 1000 }
            });

            Debug.Log("Level 4 progress saved successfully.");

            // =====================================================
            // BADGES
            // =====================================================

            if (BadgeManager.Instance != null)
            {
                // Level 4 completed
                BadgeManager.Instance.LevelCompleted("level4");

                // XP-based badges
                BadgeManager.Instance.CheckXPBadges(currentXP + 1000);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Level 4 progress save error: " + e.Message);
        }
    }
}