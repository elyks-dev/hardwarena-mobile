using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Default Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMasterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 1f;

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;

    private bool musicEnabled;
    private bool sfxEnabled;

    // =========================================================
    // PAUSE STATES
    // =========================================================

    // True when the quiz has paused the universal BGM.
    private bool musicPausedByQuiz = false;

    // Used by cutscenes.
    private bool musicWasPlayingBeforeCutscene = false;

    // =========================================================
    // AUDIO SETTINGS EVENT
    // =========================================================

    // Other scripts, such as QuizManager, can listen to this
    // whenever the audio settings change.
    public event Action OnAudioSettingsChanged;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        // Prevent duplicate AudioManagers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep AudioManager alive between scenes.
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        StartBackgroundMusic();
        ApplyAudioSettings();
    }


    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    void LoadSettings()
    {
        masterVolume =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                defaultMasterVolume
            );

        musicVolume =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                defaultMusicVolume
            );

        sfxVolume =
            PlayerPrefs.GetFloat(
                "SFXVolume",
                defaultSFXVolume
            );

        musicEnabled =
            PlayerPrefs.GetInt(
                "MusicEnabled",
                1
            ) == 1;

        sfxEnabled =
            PlayerPrefs.GetInt(
                "SFXEnabled",
                1
            ) == 1;
    }


    // =========================================================
    // SAVE SETTINGS
    // =========================================================

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(
            "MasterVolume",
            masterVolume
        );

        PlayerPrefs.SetFloat(
            "MusicVolume",
            musicVolume
        );

        PlayerPrefs.SetFloat(
            "SFXVolume",
            sfxVolume
        );

        PlayerPrefs.SetInt(
            "MusicEnabled",
            musicEnabled ? 1 : 0
        );

        PlayerPrefs.SetInt(
            "SFXEnabled",
            sfxEnabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    // =========================================================
    // START BACKGROUND MUSIC
    // =========================================================

    void StartBackgroundMusic()
    {
        if (bgmSource == null)
            return;

        if (backgroundMusic == null)
        {
            Debug.LogWarning(
                "Background music has not been assigned."
            );

            return;
        }

        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;

        if (musicEnabled)
        {
            bgmSource.Play();
        }
    }


    // =========================================================
    // APPLY AUDIO SETTINGS
    // =========================================================

    void ApplyAudioSettings()
    {
        if (bgmSource != null)
        {
            bgmSource.volume =
                masterVolume * musicVolume;

            bgmSource.mute =
                !musicEnabled;
        }

        if (sfxSource != null)
        {
            sfxSource.volume =
                masterVolume * sfxVolume;

            sfxSource.mute =
                !sfxEnabled;
        }
    }


    // =========================================================
    // MASTER VOLUME
    // =========================================================

    public void SetMasterVolume(float value)
    {
        masterVolume =
            Mathf.Clamp01(value);

        ApplyAudioSettings();
        SaveSettings();

        NotifyAudioSettingsChanged();
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }


    // =========================================================
    // MUSIC VOLUME
    // =========================================================

    public void SetMusicVolume(float value)
    {
        musicVolume =
            Mathf.Clamp01(value);

        ApplyAudioSettings();
        SaveSettings();

        NotifyAudioSettingsChanged();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }


    // =========================================================
    // MUSIC ENABLE / DISABLE
    // =========================================================

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;

        ApplyAudioSettings();

        if (!musicEnabled)
        {
            // Mute/pause universal BGM.
            if (bgmSource != null)
            {
                bgmSource.Pause();
            }
        }
        else
        {
            // Only automatically play the universal BGM if
            // nothing is intentionally keeping it paused.
            if (!musicPausedByQuiz &&
                !musicWasPlayingBeforeCutscene &&
                bgmSource != null &&
                !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        SaveSettings();

        NotifyAudioSettingsChanged();
    }

    public bool IsMusicEnabled()
    {
        return musicEnabled;
    }


    // =========================================================
    // SFX VOLUME
    // =========================================================

    public void SetSFXVolume(float value)
    {
        sfxVolume =
            Mathf.Clamp01(value);

        ApplyAudioSettings();
        SaveSettings();

        NotifyAudioSettingsChanged();
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }


    // =========================================================
    // SFX ENABLE / DISABLE
    // =========================================================

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;

        ApplyAudioSettings();
        SaveSettings();

        NotifyAudioSettingsChanged();
    }

    public bool IsSFXEnabled()
    {
        return sfxEnabled;
    }


    // =========================================================
    // FINAL MUSIC VOLUME
    // =========================================================

    public float GetFinalMusicVolume()
    {
        return masterVolume * musicVolume;
    }


    // =========================================================
    // FINAL SFX VOLUME
    // =========================================================

    public float GetFinalSFXVolume()
    {
        return masterVolume * sfxVolume;
    }


    // =========================================================
    // EXTERNAL AUDIO SOURCES
    // =========================================================

    public void PauseMainMenuMusic()
    {
        if (bgmSource == null)
            return;

        bgmSource.Pause();
    }

    public void ResumeMainMenuMusic()
    {
        if (bgmSource == null)
            return;

        if (!musicEnabled)
            return;

        bgmSource.UnPause();
    }

    public void ApplyMusicVolume(AudioSource source)
    {
        if (source == null)
            return;

        source.volume = GetFinalMusicVolume();
        source.mute = !musicEnabled;
    }

    public void ApplySFXVolume(AudioSource source)
    {
        if (source == null)
            return;

        source.volume = GetFinalSFXVolume();
        source.mute = !sfxEnabled;
    }


    // =========================================================
    // BUTTON CLICK SOUND
    // =========================================================

    public void PlayButtonClick()
    {
        if (sfxSource == null)
            return;

        if (buttonClickSound == null)
            return;

        if (!sfxEnabled)
            return;

        float finalVolume =
            masterVolume * sfxVolume;

        sfxSource.PlayOneShot(
            buttonClickSound,
            finalVolume
        );
    }


    // =========================================================
    // NOTIFY OTHER AUDIO USERS
    // =========================================================

    void NotifyAudioSettingsChanged()
    {
        OnAudioSettingsChanged?.Invoke();
    }


    // =========================================================
    // QUIZ: PAUSE BGM
    // =========================================================

    public void PauseBackgroundMusic()
    {
        if (bgmSource == null)
            return;

        // Remember that the quiz is responsible for the pause.
        musicPausedByQuiz = true;

        // Pause instead of Stop.
        // This preserves the current playback position.
        bgmSource.Pause();

        Debug.Log(
            "Universal BGM paused by Quiz."
        );
    }


    // =========================================================
    // QUIZ: RESUME BGM
    // =========================================================

    public void ResumeBackgroundMusic()
    {
        if (bgmSource == null)
            return;

        if (!musicEnabled)
            return;

        if (!musicPausedByQuiz)
            return;

        // Resume from the exact position where it paused.
        bgmSource.UnPause();

        musicPausedByQuiz = false;

        Debug.Log(
            "Universal BGM resumed after Quiz."
        );
    }


    // =========================================================
    // CUTSCENE: PAUSE BGM
    // =========================================================

    public void StopBackgroundMusic()
    {
        if (bgmSource == null)
            return;

        musicWasPlayingBeforeCutscene =
            bgmSource.isPlaying;

        bgmSource.Pause();
    }


    // =========================================================
    // CUTSCENE: RESUME BGM
    // =========================================================

    public void ResumeBackgroundMusicAfterCutscene()
    {
        if (bgmSource == null)
            return;

        if (musicWasPlayingBeforeCutscene &&
            musicEnabled)
        {
            bgmSource.UnPause();

            musicWasPlayingBeforeCutscene = false;
        }
    }
}
