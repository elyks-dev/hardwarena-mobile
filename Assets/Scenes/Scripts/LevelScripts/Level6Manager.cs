using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;

public class Level6Manager : MonoBehaviour
{
    [Header("Instructions")]
    public GameObject instructionsPopup;
    public Button instructionsButton;

    [Header("Input Popup")]
    public GameObject inputPopup;
    public Button openInputButton;

    [Header("Output Popup")]
    public GameObject outputPopup;
    public Button openOutputButton;

    [Header("Timer")]
    public TMP_Text timerTMP;
    public float levelTime = 90f;

    private float remainingTime;
    private bool timerRunning = false;
    private Coroutine timerBlinkRoutine;
    private Color timerDefaultColor = Color.white;
    private readonly Color timerWarningColor = Color.red;

    [Header("Level Completion Screen")]
    public GameObject levelCompletionScreen;
    public Button exitLevelButton;

    [Header("Try Again Popup")]
    public GameObject tryAgainPopup;
    public Button tryAgainButton;

    [Header("Draggable Devices")]
    public Level6DragDevice[] devices;

    [Header("Universal Drop Areas")]
    public RectTransform inputDropArea;
    public RectTransform outputDropArea;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 6 Audio Clips")]
    public AudioClip level6BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private int completedDevices = 0;
    private bool levelFinished = false;

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
        Time.timeScale = 1f;

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        completedDevices = 0;
        levelFinished = false;

        remainingTime = Mathf.Max(0f, levelTime);
        timerRunning = true;

        if (timerTMP != null)
            timerDefaultColor = timerTMP.color;

        UpdateTimerDisplay();

        if (instructionsPopup != null) instructionsPopup.SetActive(false);
        if (inputPopup != null) inputPopup.SetActive(false);
        if (outputPopup != null) outputPopup.SetActive(false);
        if (levelCompletionScreen != null) levelCompletionScreen.SetActive(false);
        if (tryAgainPopup != null) tryAgainPopup.SetActive(false);

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(RestartLevel);
        }

        if (exitLevelButton != null)
        {
            exitLevelButton.onClick.RemoveAllListeners();
            exitLevelButton.onClick.AddListener(ExitLevel);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMainMenuMusic();

        ApplyVolumeSettings();
        PlayLevelMusic();
    }

    private void Update()
    {
        if (!timerRunning || levelFinished)
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

    private void UpdateTimerDisplay()
    {
        if (timerTMP == null) return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerTMP.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void StopTimer()
    {
        timerRunning = false;
    }

    // ========================= PENALTY + RED BLINK =========================

    public void DeductTime(float seconds)
    {
        if (!timerRunning || levelFinished)
            return;

        remainingTime -= seconds;

        if (remainingTime < 0f)
            remainingTime = 0f;

        UpdateTimerDisplay();

        if (timerBlinkRoutine != null)
            StopCoroutine(timerBlinkRoutine);

        timerBlinkRoutine = StartCoroutine(BlinkTimerRed());

        if (remainingTime <= 0f)
            ShowTryAgainPopup();
    }

    private IEnumerator BlinkTimerRed()
    {
        if (timerTMP == null)
            yield break;

        for (int i = 0; i < 3; i++)
        {
            timerTMP.color = timerWarningColor;
            yield return new WaitForSeconds(0.12f);

            timerTMP.color = timerDefaultColor;
            yield return new WaitForSeconds(0.12f);
        }

        timerTMP.color = timerDefaultColor;
        timerBlinkRoutine = null;
    }

    // ========================= TRY AGAIN =========================

    private void ShowTryAgainPopup()
    {
        if (!timerRunning)
            return;

        timerRunning = false;

        StopLevelMusic();

        if (instructionsPopup != null) instructionsPopup.SetActive(false);
        if (inputPopup != null) inputPopup.SetActive(false);
        if (outputPopup != null) outputPopup.SetActive(false);
        if (levelCompletionScreen != null) levelCompletionScreen.SetActive(false);
        if (tryAgainPopup != null) tryAgainPopup.SetActive(true);
    }

    public void RestartLevel()
    {
        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        StopTimer();
        levelFinished = false;
        completedDevices = 0;

        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ========================= AUDIO =========================

    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    public void PlayLevelMusic()
    {
        if (bgmSource == null || level6BGMusic == null) return;

        if (bgmSource.clip == level6BGMusic && bgmSource.isPlaying)
            return;

        bgmSource.clip = level6BGMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopLevelMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayCorrectSFX()
    {
        if (sfxSource != null && correctSFX != null)
            sfxSource.PlayOneShot(correctSFX);
    }

    public void PlayWrongSFX()
    {
        if (sfxSource != null && wrongSFX != null)
            sfxSource.PlayOneShot(wrongSFX);
    }

    // ========================= POPUPS =========================

    public void OpenInstructions()
    {
        instructionsPopup.SetActive(true);
        instructionsButton.gameObject.SetActive(false);
    }

    public void CloseInstructions()
    {
        instructionsPopup.SetActive(false);
        instructionsButton.gameObject.SetActive(true);
    }

    public void OpenInputPopup()
    {
        inputPopup.SetActive(true);
        openInputButton.gameObject.SetActive(false);
    }

    public void CloseInputPopup()
    {
        inputPopup.SetActive(false);
        openInputButton.gameObject.SetActive(true);
    }

    public void OpenOutputPopup()
    {
        outputPopup.SetActive(true);
        openOutputButton.gameObject.SetActive(false);
    }

    public void CloseOutputPopup()
    {
        outputPopup.SetActive(false);
        openOutputButton.gameObject.SetActive(true);
    }

    // ========================= DROP AREAS =========================

    public RectTransform GetDropArea(Level6DragDevice.DeviceCategory category)
    {
        return category == Level6DragDevice.DeviceCategory.Input
            ? inputDropArea
            : outputDropArea;
    }

    public void DevicePlacedCorrectly()
    {
        if (levelFinished)
            return;

        completedDevices++;

        if (devices != null && completedDevices >= devices.Length)
            FinishLevel();
    }

    private void FinishLevel()
    {
        levelFinished = true;
        StopTimer();

        if (tryAgainPopup != null)
            tryAgainPopup.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(true);
    }

    // ========================= EXIT =========================

    public async void ExitLevel()
    {
        StopTimer();
        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        await SaveLevelCompletion();

        SceneManager.LoadScene("SelectLevelScene");
    }

    // ========================= FIREBASE =========================

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
            DocumentReference userRef =
                db.Collection("users").Document(user.UserId);

            DocumentReference levelRef =
                userRef.Collection("progress").Document("level6");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log("Level 6 already completed.");
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

            await userRef.UpdateAsync(new Dictionary<string, object>()
            {
                { "xp", currentXP + 1000 }
            });

            Debug.Log("Level 6 saved. +1000 XP awarded.");

            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.LevelCompleted("level6");
                BadgeManager.Instance.CheckXPBadges(currentXP + 1000);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Level 6 save failed: " + e.Message);
        }
    }
}
