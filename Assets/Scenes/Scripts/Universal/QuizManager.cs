using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Drag the correct answer button here.")]
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
    [Tooltip("Document name inside users/{uid}/quizScores")]
    public string quizId = "quiz1";

    private int passingScore
    {
        get
        {
            return Mathf.FloorToInt(questions.Length * 0.75f);
        }
    }

    private int currentQuestion = 0;
    private int score = 0;
    private bool answered = false;
    private Coroutine timerRoutine;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private readonly Color correctColor =
        new Color(0.35f, 0.85f, 0.35f);

    private readonly Color wrongColor =
        new Color(0.95f, 0.35f, 0.35f);

    private readonly Color defaultColor =
        Color.white;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseBackgroundMusic();
            AudioManager.Instance.OnAudioSettingsChanged += ApplyQuizAudioSettings;
        }

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        ApplyQuizAudioSettings();

        foreach (QuestionData q in questions)
        {
            if (q.correctButton == null)
            {
                Debug.LogError(
                    $"Question '{q.screen.name}' has no Correct Button assigned."
                );
            }
        }

        HideEverything();

        screen1.SetActive(true);

        PlayGuideMusic();

        SetupButtons();
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged
                -= ApplyQuizAudioSettings;

            AudioManager.Instance.ResumeBackgroundMusic();
        }
    }


    // =========================================================
    // APPLY AUDIO SETTINGS
    // =========================================================

    void ApplyQuizAudioSettings()
    {
        if (AudioManager.Instance == null)
            return;

        // -------------------------
        // QUIZ MUSIC
        // -------------------------

        if (bgMusicSource != null)
        {
            bgMusicSource.volume =
                AudioManager.Instance.GetFinalMusicVolume();

            bgMusicSource.mute =
                !AudioManager.Instance.IsMusicEnabled();
        }

        // -------------------------
        // QUIZ SFX
        // -------------------------

        if (sfxSource != null)
        {
            sfxSource.volume =
                AudioManager.Instance.GetFinalSFXVolume();

            sfxSource.mute =
                !AudioManager.Instance.IsSFXEnabled();
        }
    }


    // =========================================================
    // HIDE EVERYTHING
    // =========================================================

    void HideEverything()
    {
        screen1.SetActive(false);
        screen2.SetActive(false);
        screen3.SetActive(false);
        screen4.SetActive(false);

        countdownScreen.SetActive(false);
        resultScreen.SetActive(false);

        foreach (var q in questions)
        {
            q.screen.SetActive(false);
        }
    }


    // =========================================================
    // GUIDE NAVIGATION
    // =========================================================

    public void Guide2()
    {
        screen1.SetActive(false);
        screen2.SetActive(true);
    }

    public void BackGuide1()
    {
        screen2.SetActive(false);
        screen1.SetActive(true);
    }

    public void Guide3()
    {
        screen2.SetActive(false);
        screen3.SetActive(true);
    }

    public void BackGuide2()
    {
        screen3.SetActive(false);
        screen2.SetActive(true);
    }

    public void Guide4()
    {
        screen3.SetActive(false);
        screen4.SetActive(true);
    }

    public void BackGuide3()
    {
        screen4.SetActive(false);
        screen3.SetActive(true);
    }


    // =========================================================
    // START QUIZ
    // =========================================================

    public void StartQuiz()
    {
        screen4.SetActive(false);

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        countdownScreen.SetActive(true);

        // Stop guide music during countdown.
        bgMusicSource.Stop();

        // Play countdown sound.
        sfxSource.PlayOneShot(countdownAudio);

        string[] countdown =
        {
            "3",
            "2",
            "1",
            "START!"
        };

        foreach (string value in countdown)
        {
            countdownTMP.text = value;

            yield return new WaitForSeconds(1f);
        }

        countdownScreen.SetActive(false);

        score = 0;
        currentQuestion = 0;

        ShowQuestion(currentQuestion);
    }


    // =========================================================
    // QUESTIONS
    // =========================================================

    void SetupButtons()
    {
        foreach (var q in questions)
        {
            QuestionData localQuestion = q;

            Button a = localQuestion.buttonA;
            Button b = localQuestion.buttonB;
            Button c = localQuestion.buttonC;
            Button d = localQuestion.buttonD;

            Debug.Log("===== BUTTON SETUP =====");
            Debug.Log($"Question: {localQuestion.screen.name}");
            Debug.Log($"Button A: {a.name} | Pos: {((RectTransform)a.transform).anchoredPosition}");
            Debug.Log($"Button B: {b.name} | Pos: {((RectTransform)b.transform).anchoredPosition}");
            Debug.Log($"Button C: {c.name} | Pos: {((RectTransform)c.transform).anchoredPosition}");
            Debug.Log($"Button D: {d.name} | Pos: {((RectTransform)d.transform).anchoredPosition}");
            Debug.Log($"Correct Button Assigned: {localQuestion.correctButton.name}");

            CheckButtonOverlap(localQuestion);

            Debug.Log("===== SETUP QUESTION =====");
            Debug.Log($"Question Screen: {q.screen.name}");
            Debug.Log($"Correct Button: {q.correctButton?.name ?? "Missing"}");
            Debug.Log($"Button A Object: {q.buttonA.name}");
            Debug.Log($"Button B Object: {q.buttonB.name}");
            Debug.Log($"Button C Object: {q.buttonC.name}");
            Debug.Log($"Button D Object: {q.buttonD.name}");

            a.onClick.RemoveAllListeners();
            b.onClick.RemoveAllListeners();
            c.onClick.RemoveAllListeners();
            d.onClick.RemoveAllListeners();

            Debug.Log($"Listener A -> {a.name}");
            Debug.Log($"Listener B -> {b.name}");
            Debug.Log($"Listener C -> {c.name}");
            Debug.Log($"Listener D -> {d.name}");

            a.onClick.AddListener(
                () => Answer(localQuestion, a)
            );

            b.onClick.AddListener(
                () => Answer(localQuestion, b)
            );

            c.onClick.AddListener(
                () => Answer(localQuestion, c)
            );

            d.onClick.AddListener(
                () => Answer(localQuestion, d)
            );
        }
    }


    // =========================================================
    // SHOW QUESTION
    // =========================================================

    void ShowQuestion(int index)
    {
        // Play quiz background music.
        bgMusicSource.clip = quizMusic;
        bgMusicSource.loop = true;

        // Make sure current settings are applied.
        ApplyQuizAudioSettings();

        if (AudioManager.Instance == null ||
            AudioManager.Instance.IsMusicEnabled())
        {
            bgMusicSource.Play();
        }

        QuestionData q = questions[index];

        Debug.Log("===== SHOW QUESTION =====");
        Debug.Log($"Question Index: {index}");
        Debug.Log($"Screen Activated: {q.screen.name}");
        Debug.Log($"Correct Button Loaded: {q.correctButton?.name ?? "Missing"}");

        q.screen.SetActive(true);

        answered = false;

        ResetButtons(q);

        timerRoutine =
            StartCoroutine(StartTimer(q));
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

            q.timerSlider.value =
                timer / questionTime;

            q.timerTMP.text =
                Mathf.Ceil(timer).ToString() + "s";

            yield return null;
        }

        if (!answered)
        {
            Answer(q, null);
        }
    }


    // =========================================================
    // ANSWER
    // =========================================================

    void Answer(QuestionData q, Button selectedButton)
    {
        Debug.Log("========== CLICK DEBUG ==========");
        Debug.Log($"Question Screen: {q.screen.name}");
        Debug.Log($"Clicked Button Object: {selectedButton?.name ?? "None (timer expired)"}");

        if (selectedButton != null)
        {
            Debug.Log(
                $"Clicked Button Position: {((RectTransform)selectedButton.transform).anchoredPosition}"
            );
        }

        Debug.Log($"Correct Button Object: {q.correctButton.name}");
        Debug.Log(
            $"Correct Button Position: {((RectTransform)q.correctButton.transform).anchoredPosition}"
        );

        Debug.Log("========== ANSWER DEBUG ==========");
        Debug.Log($"Question Screen: {q.screen.name}");
        Debug.Log($"Selected Button: {selectedButton?.name ?? "None (timer expired)"}");
        Debug.Log($"Correct Button: {q.correctButton?.name ?? "Missing"}");

        if (answered)
        {
            Debug.Log("IGNORED: Question already answered.");
            return;
        }

        answered = true;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
        }

        Button[] buttons =
        {
            q.buttonA,
            q.buttonB,
            q.buttonC,
            q.buttonD
        };

        // Show correct and wrong answers.
        foreach (Button button in buttons)
        {
            ColorBlock colors = button.colors;

            if (button == q.correctButton)
            {
                colors.normalColor = correctColor;
            }
            else
            {
                colors.normalColor = wrongColor;
            }

            colors.highlightedColor =
                colors.normalColor;

            colors.selectedColor =
                colors.normalColor;

            colors.pressedColor =
                colors.normalColor;

            button.colors = colors;
        }

        // Check answer.
        Debug.Log(
            $"Comparing {selectedButton?.name ?? "None (timer expired)"} against {q.correctButton?.name ?? "Missing"}"
        );

        bool isCorrect = selectedButton == q.correctButton;

        if (isCorrect)
        {
            Debug.Log("RESULT = CORRECT");

            score++;

            sfxSource.PlayOneShot(correctAudio);
        }
        else
        {
            Debug.Log("RESULT = WRONG");

            sfxSource.PlayOneShot(wrongAudio);
        }

        StartCoroutine(NextQuestion(q));
    }

    private void CheckButtonOverlap(QuestionData q)
    {
        Button[] buttons =
        {
            q.buttonA,
            q.buttonB,
            q.buttonC,
            q.buttonD
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rtA = buttons[i].GetComponent<RectTransform>();

            for (int j = i + 1; j < buttons.Length; j++)
            {
                RectTransform rtB = buttons[j].GetComponent<RectTransform>();

                if (rtA.anchoredPosition == rtB.anchoredPosition)
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
        {
            ShowQuestion(currentQuestion);
        }
        else
        {
            ShowResults();
        }
    }


    // =========================================================
    // RESET BUTTONS
    // =========================================================

    void ResetButtons(QuestionData q)
    {
        Button[] buttons =
        {
            q.buttonA,
            q.buttonB,
            q.buttonC,
            q.buttonD
        };

        foreach (Button b in buttons)
        {
            ColorBlock colors = b.colors;

            colors.normalColor = defaultColor;
            colors.highlightedColor = defaultColor;
            colors.selectedColor = defaultColor;
            colors.pressedColor = defaultColor;

            b.colors = colors;
        }

        q.timerSlider.value = 1;

        q.timerTMP.text =
            questionTime.ToString("0") + "s";
    }


    // =========================================================
    // RESULTS
    // =========================================================

    void ShowResults()
    {
        bgMusicSource.Stop();

        sfxSource.PlayOneShot(resultsAudio);

        resultScreen.SetActive(true);

        scoreTMP.text = score + "/" + questions.Length;

        int earnedXP = score * xpPerCorrect;

        xpTMP.text = "+ " + earnedXP + " XP";

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

        UpdateStars();

        SaveQuizProgress(earnedXP);
    }


    // =========================================================
    // UPDATE STARS
    // =========================================================

    void UpdateStars()
    {
        star1.sprite = noStarSprite;
        star2.sprite = noStarSprite;
        star3.sprite = noStarSprite;

        int stars = 0;

        // 3 Stars = Perfect Score
        if (score == questions.Length)
        {
            stars = 3;
        }
        // 2 Stars = Passing Score (75% rounded down)
        else if (score >= passingScore)
        {
            stars = 2;
        }
        // 1 Star = Any score below passing but above 0
        else if (score > 0)
        {
            stars = 1;
        }

        if (stars >= 1)
            star1.sprite = starSprite;

        if (stars >= 2)
            star2.sprite = starSprite;

        if (stars >= 3)
            star3.sprite = starSprite;
    }


    // =========================================================
    // AUDIO
    // =========================================================

    void PlayGuideMusic()
    {
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
    // LEAVE QUIZ
    // =========================================================
    // =========================================================
    // SAVE QUIZ PROGRESS TO FIRESTORE
    // =========================================================

    async void SaveQuizProgress(int earnedXP)
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user logged in.");
            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users").Document(user.UserId);

            DocumentReference quizRef =
                userRef.Collection("quizScores").Document(quizId);

            bool passed = score >= passingScore;

            // Star Rating (matches UI)
            int stars = 0;

            if (score == questions.Length)
            {
                stars = 3;
            }
            else if (score >= passingScore)
            {
                stars = 2;
            }
            else if (score > 0)
            {
                stars = 1;
            }

            Dictionary<string, object> quizData =
                new Dictionary<string, object>()
                {
                    { "completed", true },
                    { "passed", passed },
                    { "score", score },
                    { "totalQuestions", questions.Length },
                    { "earnedXP", earnedXP },
                    { "stars", stars },
                    { "completedAt", Timestamp.GetCurrentTimestamp() }
                };

            await quizRef.SetAsync(quizData);

            DocumentSnapshot userSnapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (userSnapshot.Exists &&
                userSnapshot.TryGetValue("xp", out int xp))
            {
                currentXP = xp;
            }

            await userRef.UpdateAsync(
                new Dictionary<string, object>()
                {
                    { "xp", currentXP + earnedXP }
                });

            Debug.Log(
                $"Quiz saved to quizScores/{quizId}. XP awarded: {earnedXP}"
            );

            // =====================================================
            // BADGES
            // =====================================================

            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.QuizCompleted(quizId);
                BadgeManager.Instance.CheckXPBadges(currentXP + earnedXP);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Quiz Save Error: " + e.Message);
        }
    }

    public void BackToLevels()
    {
        // Stop quiz music.
        if (bgMusicSource != null)
        {
            bgMusicSource.Stop();
        }

        // Resume universal background music.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeBackgroundMusic();
        }

        // Load level selection scene.
        SceneManager.LoadScene("SelectLevelScene");
    }
}
