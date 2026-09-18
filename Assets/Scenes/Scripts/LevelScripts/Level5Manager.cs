using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

public class Level5Manager : MonoBehaviour
{
    // =====================================================
    // LEVEL COMPLETION SCREEN
    // =====================================================

    [Header("Level Completion Screen")]
    public GameObject levelCompletionScreen;

    [Header("Exit Level Button")]
    public Button exitLevelButton;

    // =====================================================
    // DRAGGABLE LABELS
    // =====================================================

    [Header("Draggable Labels")]
    public Level5DragLabel[] labels;

    // =====================================================
    // AUDIO
    // =====================================================

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 5 Audio Clips")]
    public AudioClip level5BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    // =====================================================
    // INTERNAL VARIABLES
    // =====================================================

    private int completedLabels = 0;
    private bool levelFinished = false;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // AUDIO EVENTS
    // =====================================================

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

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        completedLabels = 0;
        levelFinished = false;

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

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

    // =====================================================
    // AUDIO METHODS
    // =====================================================

    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    public void PlayLevelMusic()
    {
        if (bgmSource == null || level5BGMusic == null) return;

        if (bgmSource.clip == level5BGMusic && bgmSource.isPlaying) return;

        bgmSource.clip = level5BGMusic;
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
        if (sfxSource == null || correctSFX == null) return;

        sfxSource.PlayOneShot(correctSFX);
    }

    public void PlayWrongSFX()
    {
        if (sfxSource == null || wrongSFX == null) return;

        sfxSource.PlayOneShot(wrongSFX);
    }

    // =====================================================
    // LABEL COMPLETED
    // =====================================================

    public void LabelPlacedCorrectly()
    {
        if (levelFinished) return;

        completedLabels++;

        if (labels != null && completedLabels >= labels.Length)
        {
            FinishLevel();
        }
    }

    // =====================================================
    // FINISH LEVEL
    // =====================================================

    private void FinishLevel()
    {
        levelFinished = true;

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(true);
    }

    // =====================================================
    // EXIT LEVEL
    // =====================================================

    public async void ExitLevel()
    {
        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        await SaveLevelCompletion();

        SceneManager.LoadScene("SelectLevelScene");
    }

    // =====================================================
    // SAVE LEVEL COMPLETION + XP
    // =====================================================

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
                .Document("level5");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            // Already completed? Don't award XP again.
            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log("Level 5 already completed.");
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

            Debug.Log("Level 5 progress saved successfully.");

            // =====================================================
            // BADGES
            // =====================================================

            if (BadgeManager.Instance != null)
            {
                // Level 5 completed
                BadgeManager.Instance.LevelCompleted("level5");

                // XP-based badges
                BadgeManager.Instance.CheckXPBadges(
                    currentXP + 1000
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Level 5 progress save error: " + e.Message);
        }
    }
}