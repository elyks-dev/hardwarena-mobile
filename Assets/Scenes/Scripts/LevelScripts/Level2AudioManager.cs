using UnityEngine;

public class Level2AudioManager : MonoBehaviour
{
    public static Level2AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 2 Audio Clips")]
    public AudioClip level2BGMusic;
    public AudioClip hazardClickSFX;
    public AudioClip incorrectClickSFX; // NEW

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged += ApplyVolumeSettings;
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -= ApplyVolumeSettings;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMainMenuMusic();
        }

        ApplyVolumeSettings();
    }

    public void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    // =====================================================
    // MUSIC
    // =====================================================

    public void PlayLevelMusic()
    {
        if (bgmSource == null || level2BGMusic == null)
            return;

        if (bgmSource.clip == level2BGMusic && bgmSource.isPlaying)
            return;

        bgmSource.clip = level2BGMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopLevelMusic()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // =====================================================
    // SOUND EFFECTS
    // =====================================================

    // Correct hazard click
    public void PlayHazardClick()
    {
        if (sfxSource == null || hazardClickSFX == null)
            return;

        sfxSource.PlayOneShot(hazardClickSFX);
    }

    // Wrong object click (NEW)
    public void PlayIncorrectClick()
    {
        if (sfxSource == null || incorrectClickSFX == null)
            return;

        sfxSource.PlayOneShot(incorrectClickSFX);
    }

    // =====================================================
    // EXIT LEVEL
    // =====================================================

    public void ExitLevel()
    {
        StopLevelMusic();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMainMenuMusic();
        }
    }
}