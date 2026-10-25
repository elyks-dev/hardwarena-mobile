using System.Collections;
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
    // GAME SCREEN
    // =========================================================

    [Header("Game Screen (optional root of dialogue, next button and computer buttons)")]
    public GameObject gameScreen;

    // =========================================================
    // DIALOGUE
    // =========================================================

    [Header("Dialogue")]
    public TMP_Text dialogueTMP;

    [TextArea(3, 8)]
    public string instructionMessage;

    public Button nextButton;

    [Header("Next Button Text")]
    [Tooltip("Text inside the next button. Found automatically if left empty.")]
    public TMP_Text nextButtonText;
    public string startLabel = "TAP TO START";
    public string nextLabel = "TAP TO NEXT";
    public string finishLabel = "TAP TO FINISH";

    [Header("Cover Image")]
    [Tooltip("Covers the whole page except the dialogue and next button. Shown at the start and again on the finish step.")]
    public GameObject coverImage;

    // =========================================================
    // QUESTIONS
    // =========================================================

    [System.Serializable]
    public class QuestionData
    {
        [Tooltip("The computer-type button that is the correct answer for this question.")]
        public Button button;

        [TextArea(2, 6)]
        public string dialogue;

        [TextArea(2, 6)]
        public string wrongDialogue;

        [TextArea(2, 6)]
        public string correctDialogue;
    }

    [Header("Questions (in order)")]
    public List<QuestionData> questions = new List<QuestionData>();

    [Header("Extra Buttons (optional wrong-answer decoys)")]
    public List<Button> decoyButtons = new List<Button>();

    [Header("Button Feedback")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color defaultColor = Color.white;
    public int blinkCount = 3;
    public float blinkInterval = 0.2f;

    private enum GameState
    {
        Instruction,
        Question,
        Answered,
        Finished
    }

    private GameState state = GameState.Instruction;
    private int currentQuestionIndex = 0;

    private readonly HashSet<Button> solvedButtons = new HashSet<Button>();
    private readonly Dictionary<Button, Coroutine> blinkRoutines = new Dictionary<Button, Coroutine>();

    // =========================================================
    // TIMER
    // =========================================================

    [Header("Timer")]
    public TMP_Text timerTMP;
    public float levelTime = 90f;
    public float wrongAnswerPenalty = 5f;

    [Tooltip("How long the timer blinks red after a deduction.")]
    public float timerBlinkDuration = 0.4f;

    private Color timerOriginalColor;
    private Coroutine timerBlinkRoutine;

    private float remainingTime;
    private bool timerRunning = false;

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

    [Header("Firebase")]
    public string levelId = "level4";
    public int xpReward = 1000;

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

        if (timerTMP != null)
            timerOriginalColor = timerTMP.color;

        if (nextButtonText == null && nextButton != null)
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(RestartLevel);
        }

        // Next button (used for the instruction and after each correct answer).
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        SetupComputerButtons();

        remainingTime = Mathf.Max(0f, levelTime);
        UpdateTimerDisplay();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMainMenuMusic();

        ApplyVolumeSettings();
        PlayLevelMusic();

        ShowInstruction();
    }

    private void SetupComputerButtons()
    {
        List<Button> allButtons = new List<Button>();

        foreach (QuestionData q in questions)
        {
            if (q.button != null && !allButtons.Contains(q.button))
                allButtons.Add(q.button);
        }

        foreach (Button b in decoyButtons)
        {
            if (b != null && !allButtons.Contains(b))
                allButtons.Add(b);
        }

        foreach (Button b in allButtons)
        {
            Button captured = b;

            SetButtonColor(captured, defaultColor);
            captured.onClick.RemoveAllListeners();
            captured.onClick.AddListener(() => OnComputerButtonClicked(captured));
        }
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
    // FLOW
    // =========================================================

    private void ShowInstruction()
    {
        state = GameState.Instruction;
        currentQuestionIndex = 0;

        if (gameScreen != null)
            gameScreen.SetActive(true);

        if (dialogueTMP != null)
            dialogueTMP.text = instructionMessage;

        SetCoverActive(true);
        SetNextLabel(startLabel);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    private void OnNextClicked()
    {
        switch (state)
        {
            case GameState.Instruction:
                StartLevel();
                break;

            case GameState.Answered:
                NextQuestion();
                break;
        }
    }

    private void StartLevel()
    {
        if (questions.Count == 0)
        {
            Debug.LogError("Level 4 has no questions assigned.");
            return;
        }

        currentQuestionIndex = 0;
        remainingTime = Mathf.Max(0f, levelTime);
        timerRunning = true;
        UpdateTimerDisplay();

        // Game starts: remove the cover image.
        SetCoverActive(false);

        ShowQuestion(currentQuestionIndex);
    }

    private void ShowQuestion(int index)
    {
        state = GameState.Question;

        if (dialogueTMP != null)
            dialogueTMP.text = questions[index].dialogue;

        // Next button only appears after the correct button is clicked.
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
    }

    public void NextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex >= questions.Count)
        {
            state = GameState.Finished;
            StopTimer();

            // Don't let the cover image block the completion screen.
            SetCoverActive(false);

            if (gameScreen != null)
                gameScreen.SetActive(false);

            if (levelCompletionScreen != null)
                levelCompletionScreen.SetActive(true);
        }
        else
        {
            ShowQuestion(currentQuestionIndex);
        }
    }

    // =========================================================
    // ANSWER LOGIC
    // =========================================================

    private void OnComputerButtonClicked(Button clicked)
    {
        if (state != GameState.Question)
            return;

        QuestionData question = questions[currentQuestionIndex];

        if (clicked == question.button)
        {
            state = GameState.Answered;

            StopBlink(clicked);
            solvedButtons.Add(clicked);
            SetButtonColor(clicked, correctColor);

            PlayCorrectSFX();

            if (dialogueTMP != null)
                dialogueTMP.text = question.correctDialogue;

            bool isLastQuestion = currentQuestionIndex >= questions.Count - 1;

            SetNextLabel(isLastQuestion ? finishLabel : nextLabel);

            // Finish step: stop the timer and cover the page again
            // so only the dialogue and button show.
            if (isLastQuestion)
            {
                StopTimer();
                SetCoverActive(true);
            }

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
        else
        {
            if (dialogueTMP != null)
                dialogueTMP.text = question.wrongDialogue;

            PlayWrongSFX();

            StopBlink(clicked);
            blinkRoutines[clicked] = StartCoroutine(BlinkWrong(clicked));

            // Time penalty for a wrong answer.
            remainingTime -= wrongAnswerPenalty;

            // Blink the timer red.
            if (timerBlinkRoutine != null)
                StopCoroutine(timerBlinkRoutine);

            timerBlinkRoutine = StartCoroutine(BlinkTimerRed());

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                UpdateTimerDisplay();
                ShowTryAgainPopup();
            }
            else
            {
                UpdateTimerDisplay();
            }
        }
    }

    private IEnumerator BlinkWrong(Button button)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            SetButtonColor(button, wrongColor);
            yield return new WaitForSeconds(blinkInterval);

            SetButtonColor(button, GetRestColor(button));
            yield return new WaitForSeconds(blinkInterval);
        }

        blinkRoutines.Remove(button);
    }

    private void StopBlink(Button button)
    {
        if (blinkRoutines.TryGetValue(button, out Coroutine routine))
        {
            if (routine != null)
                StopCoroutine(routine);

            blinkRoutines.Remove(button);
        }

        SetButtonColor(button, GetRestColor(button));
    }

    // Solved buttons stay green for the whole game.
    private Color GetRestColor(Button button)
    {
        return solvedButtons.Contains(button) ? correctColor : defaultColor;
    }

    private IEnumerator BlinkTimerRed()
    {
        if (timerTMP == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < timerBlinkDuration)
        {
            timerTMP.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            timerTMP.color = timerOriginalColor;
            yield return new WaitForSeconds(0.1f);

            elapsed += 0.2f;
        }

        timerTMP.color = timerOriginalColor;
        timerBlinkRoutine = null;
    }

    // =========================================================
    // COVER IMAGE + NEXT BUTTON TEXT HELPERS
    // =========================================================

    private void SetCoverActive(bool active)
    {
        if (coverImage != null)
            coverImage.SetActive(active);
    }

    private void SetNextLabel(string text)
    {
        if (nextButtonText != null)
            nextButtonText.text = text;
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
        state = GameState.Finished;

        // Stop the timer blink and restore its color.
        if (timerBlinkRoutine != null)
        {
            StopCoroutine(timerBlinkRoutine);
            timerBlinkRoutine = null;
        }

        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;

        SetCoverActive(false);

        if (gameScreen != null)
            gameScreen.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        StopLevelMusic();

        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(true);
    }

    public void RestartLevel()
    {
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        timerRunning = false;

        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
                .Document(levelId);

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            // Prevent duplicate XP
            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log(levelId + " already completed.");
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
                { "xp", currentXP + xpReward }
            });

            Debug.Log(levelId + " progress saved successfully.");

            // =====================================================
            // BADGES
            // =====================================================

            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.LevelCompleted(levelId);
                BadgeManager.Instance.CheckXPBadges(currentXP + xpReward);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(levelId + " progress save error: " + e.Message);
        }
    }
}