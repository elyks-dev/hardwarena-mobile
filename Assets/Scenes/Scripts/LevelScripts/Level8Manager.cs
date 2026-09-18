using System.Collections.Generic;
using System.Threading.Tasks;

using Firebase.Auth;
using Firebase.Firestore;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level8Manager : MonoBehaviour
{
    // =====================================================
    // SCREEN 1
    // =====================================================

    [Header("Screen 1")]
    public GameObject screen1;
    public Button startButton;

    // =====================================================
    // SCREEN 2
    // =====================================================

    [Header("Screen 2")]
    public GameObject screen2;

    // =====================================================
    // DRAGGABLE ITEMS
    // =====================================================

    [Header("Draggable Items")]
    public Level8DragItem[] draggableItems;

    // =====================================================
    // DROP AREAS
    // =====================================================

    [Header("Drop Areas")]
    public Level8DropArea[] dropAreas;

    // =====================================================
    // PLUGGED-IN ITEMS
    // =====================================================

    [Header("Plugged-In Items")]
    public GameObject[] pluggedInItems;

    // =====================================================
    // AUDIO
    // =====================================================

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 8 Audio")]
    public AudioClip level8BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // INTERNAL
    // =====================================================

    private int completedItems = 0;
    private bool levelFinished = false;

    // =====================================================
    // AUDIO EVENTS
    // =====================================================

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged +=
                ApplyVolumeSettings;
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -=
                ApplyVolumeSettings;
        }
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        completedItems = 0;
        levelFinished = false;

        // Screen 1 visible.
        if (screen1 != null)
            screen1.SetActive(true);

        // Screen 2 hidden.
        if (screen2 != null)
            screen2.SetActive(false);

        // Hide all plugged-in images.
        ResetPluggedInItems();

        // Start button.
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();

            startButton.onClick.AddListener(
                StartLevel
            );
        }

        // Audio.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMainMenuMusic();
        }

        ApplyVolumeSettings();
        PlayLevelMusic();
    }

    // =====================================================
    // START LEVEL
    // =====================================================

    public void StartLevel()
    {
        if (levelFinished)
            return;

        if (screen1 != null)
            screen1.SetActive(false);

        if (screen2 != null)
            screen2.SetActive(true);

        completedItems = 0;

        ResetPluggedInItems();

        // Reset draggable items.
        if (draggableItems != null)
        {
            foreach (Level8DragItem item in draggableItems)
            {
                if (item != null)
                    item.ResetItem();
            }
        }

        // Reset drop areas.
        if (dropAreas != null)
        {
            foreach (Level8DropArea area in dropAreas)
            {
                if (area != null)
                    area.ResetZone();
            }
        }
    }

    // =====================================================
    // RESET PLUGGED-IN ITEMS
    // =====================================================

    private void ResetPluggedInItems()
    {
        if (pluggedInItems == null)
            return;

        foreach (GameObject plugged in pluggedInItems)
        {
            if (plugged != null)
                plugged.SetActive(false);
        }
    }

    // =====================================================
    // CORRECT PLACEMENT
    // =====================================================

    public void ItemPlacedCorrectly()
    {
        if (levelFinished)
            return;

        completedItems++;

        PlayCorrectSFX();

        Debug.Log(
            "Level 8 part placed correctly: " +
            completedItems +
            "/" +
            draggableItems.Length
        );

        // All 13 parts completed.
        if (draggableItems != null &&
            completedItems >= draggableItems.Length)
        {
            FinishLevel();
        }
    }

    // =====================================================
    // WRONG PLACEMENT
    // =====================================================

    public void WrongPlacement()
    {
        if (levelFinished)
            return;

        PlayWrongSFX();
    }

    // =====================================================
    // FINISH LEVEL
    // =====================================================

    private async void FinishLevel()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        Debug.Log(
            "Level 8 completed. Saving progress..."
        );

        bool saved =
            await SaveLevelCompletion();

        if (!saved)
        {
            Debug.LogError(
                "Level 8 save failed. " +
                "Final cutscene will NOT load."
            );

            levelFinished = false;

            return;
        }

        Debug.Log(
            "Level 8 saved successfully. " +
            "Loading FinalCutsceneScene..."
        );

        StopLevelMusic();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMainMenuMusic();
        }

        SceneManager.LoadScene(
            "FinalCutsceneScene"
        );
    }

    // =====================================================
    // SAVE LEVEL 8 + XP + BADGES
    // =====================================================

    private async Task<bool> SaveLevelCompletion()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "No Firebase user logged in."
            );

            return false;
        }

        try
        {
            // =================================================
            // USER
            // =================================================

            DocumentReference userRef =
                db.Collection("users")
                  .Document(user.UserId);

            // =================================================
            // LEVEL 8
            // =================================================

            DocumentReference levelRef =
                userRef.Collection("progress")
                       .Document("level8");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            // Already completed.
            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue(
                    "completed",
                    out bool alreadyCompleted) &&
                alreadyCompleted)
            {
                Debug.Log(
                    "Level 8 already completed. " +
                    "No duplicate XP."
                );

                // Still check progression badges in case
                // something was missing before.
                if (BadgeManager.Instance != null)
                {
                    BadgeManager.Instance.LevelCompleted(
                        "level8"
                    );
                }

                return true;
            }

            // =================================================
            // SAVE LEVEL COMPLETION
            // =================================================

            await levelRef.SetAsync(
                new Dictionary<string, object>()
                {
                    {
                        "completed",
                        true
                    },
                    {
                        "completedAt",
                        FieldValue.ServerTimestamp
                    }
                },
                SetOptions.MergeAll
            );

            // =================================================
            // GET CURRENT XP
            // =================================================

            DocumentSnapshot userSnapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (userSnapshot.Exists &&
                userSnapshot.TryGetValue(
                    "xp",
                    out int savedXP))
            {
                currentXP = savedXP;
            }

            int newXP =
                currentXP + 1000;

            // =================================================
            // SAVE XP
            // =================================================

            await userRef.UpdateAsync(
                new Dictionary<string, object>()
                {
                    {
                        "xp",
                        newXP
                    }
                }
            );

            Debug.Log(
                "Level 8 saved. " +
                "+1000 XP awarded."
            );

            // =================================================
            // BADGES
            // =================================================

            if (BadgeManager.Instance != null)
            {
                // Level 8 completion.
                //
                // This handles:
                // Master of All
                // Next Please if applicable
                // Ultimate Collector checks
                BadgeManager.Instance.LevelCompleted(
                    "level8"
                );

                // XP badges:
                //
                // Top Contender > 0
                // Rising Player >= 3000
                // The Final Ascent >= 7000
                BadgeManager.Instance.CheckXPBadges(
                    newXP
                );
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Level 8 save failed: " +
                e.Message
            );

            return false;
        }
    }

    // =====================================================
    // AUDIO
    // =====================================================

    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplyMusicVolume(
            bgmSource
        );

        AudioManager.Instance.ApplySFXVolume(
            sfxSource
        );
    }

    // =====================================================
    // PLAY LEVEL MUSIC
    // =====================================================

    private void PlayLevelMusic()
    {
        if (bgmSource == null ||
            level8BGMusic == null)
            return;

        if (bgmSource.clip == level8BGMusic &&
            bgmSource.isPlaying)
            return;

        bgmSource.clip =
            level8BGMusic;

        bgmSource.loop = true;

        bgmSource.Play();
    }

    // =====================================================
    // STOP LEVEL MUSIC
    // =====================================================

    private void StopLevelMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // =====================================================
    // CORRECT SFX
    // =====================================================

    public void PlayCorrectSFX()
    {
        if (sfxSource == null ||
            correctSFX == null)
            return;

        sfxSource.PlayOneShot(
            correctSFX
        );
    }

    // =====================================================
    // WRONG SFX
    // =====================================================

    public void PlayWrongSFX()
    {
        if (sfxSource == null ||
            wrongSFX == null)
            return;

        sfxSource.PlayOneShot(
            wrongSFX
        );
    }
}