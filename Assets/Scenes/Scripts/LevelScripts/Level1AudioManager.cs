using UnityEngine;

public class Level1AudioManager : MonoBehaviour
{
    public static Level1AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 1 Audio Clips")]
    public AudioClip level1BGMusic;
    public AudioClip slotRollingAudio;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged += ApplyVolumeSettings;
        }
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -= ApplyVolumeSettings;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMainMenuMusic();
        }

        PlayLevelMusic();
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
    // LEVEL 1 BACKGROUND MUSIC
    // =====================================================

    public void PlayLevelMusic()
    {
        if (bgmSource == null || level1BGMusic == null)
            return;

        bgmSource.clip = level1BGMusic;
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
    // SLOT ROLLING SOUND
    // =====================================================

    public void PlaySlotRolling()
    {
        if (sfxSource == null || slotRollingAudio == null)
            return;

        sfxSource.clip = slotRollingAudio;
        sfxSource.loop = true;
        sfxSource.Play();
    }

    public void StopSlotRolling()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
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
