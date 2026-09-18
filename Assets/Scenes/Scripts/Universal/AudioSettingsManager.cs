using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    // =========================================================
    // MASTER VOLUME
    // =========================================================

    [Header("Master Volume")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button volumeEnableButton;
    [SerializeField] private Button volumeDisableButton;


    // =========================================================
    // MUSIC
    // =========================================================

    [Header("Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button musicEnableButton;
    [SerializeField] private Button musicDisableButton;


    // =========================================================
    // SFX
    // =========================================================

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button sfxEnableButton;
    [SerializeField] private Button sfxDisableButton;

    // =========================================================
    // PREVIOUS AUDIO STATE
    // =========================================================

    private float previousMasterVolume = 1f;
    private float previousMusicVolume = 1f;
    private float previousSFXVolume = 1f;
    private bool previousMusicEnabled = true;
    private bool previousSFXEnabled = true;
    private bool masterMuteStateSaved;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "AudioManager does not exist."
            );

            return;
        }

        // Load saved settings into the UI.
        LoadUI();

        // Connect buttons and sliders.
        SetupListeners();
    }


    // =========================================================
    // LOAD UI
    // =========================================================

    void LoadUI()
    {
        float masterVolume =
            AudioManager.Instance.GetMasterVolume();

        float musicVolume =
            AudioManager.Instance.GetMusicVolume();

        float sfxVolume =
            AudioManager.Instance.GetSFXVolume();

        bool musicEnabled =
            AudioManager.Instance.IsMusicEnabled();

        bool sfxEnabled =
            AudioManager.Instance.IsSFXEnabled();

        previousMasterVolume =
            masterVolume > 0f ? masterVolume : 1f;

        previousMusicVolume =
            musicVolume > 0f ? musicVolume : 1f;

        previousSFXVolume =
            sfxVolume > 0f ? sfxVolume : 1f;

        previousMusicEnabled =
            musicEnabled;

        previousSFXEnabled =
            sfxEnabled;

        masterMuteStateSaved =
            masterVolume <= 0f;

        // -------------------------
        // MASTER VOLUME
        // -------------------------

        volumeSlider.value =
            masterVolume;

        bool masterEnabled =
            masterVolume > 0f;

        UpdateButtonAppearance(
            volumeEnableButton,
            volumeDisableButton,
            masterEnabled
        );


        // -------------------------
        // MUSIC
        // -------------------------

        musicSlider.value =
            musicVolume;

        bool musicVisibleEnabled =
            masterVolume > 0f &&
            musicVolume > 0f &&
            musicEnabled;

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            musicVisibleEnabled
        );


        // -------------------------
        // SFX
        // -------------------------

        sfxSlider.value =
            sfxVolume;

        bool sfxVisibleEnabled =
            masterVolume > 0f &&
            sfxVolume > 0f &&
            sfxEnabled;

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            sfxVisibleEnabled
        );
    }


    // =========================================================
    // SETUP LISTENERS
    // =========================================================

    void SetupListeners()
    {
        // -------------------------
        // MASTER VOLUME
        // -------------------------

        volumeSlider.onValueChanged.AddListener(
            OnVolumeChanged
        );

        volumeEnableButton.onClick.AddListener(
            EnableVolume
        );

        volumeDisableButton.onClick.AddListener(
            DisableVolume
        );


        // -------------------------
        // MUSIC
        // -------------------------

        musicSlider.onValueChanged.AddListener(
            OnMusicVolumeChanged
        );

        musicEnableButton.onClick.AddListener(
            EnableMusic
        );

        musicDisableButton.onClick.AddListener(
            DisableMusic
        );


        // -------------------------
        // SFX
        // -------------------------

        sfxSlider.onValueChanged.AddListener(
            OnSFXVolumeChanged
        );

        sfxEnableButton.onClick.AddListener(
            EnableSFX
        );

        sfxDisableButton.onClick.AddListener(
            DisableSFX
        );
    }


    // =========================================================
    // MASTER VOLUME SLIDER
    // =========================================================

    void OnVolumeChanged(float value)
    {
        if (value <= 0f && !masterMuteStateSaved)
        {
            SaveMusicAndSFXState();
            masterMuteStateSaved = true;

            Debug.Log(
                "AudioSettingsManager: Master muted. " +
                "previousSFXEnabled=" +
                previousSFXEnabled +
                ", currentSFXEnabled=" +
                AudioManager.Instance.IsSFXEnabled() +
                ", master=" +
                value
            );
        }

        AudioManager.Instance.SetMasterVolume(
            value
        );

        if (value <= 0f)
        {
            UpdateButtonAppearance(
                musicEnableButton,
                musicDisableButton,
                false
            );

            UpdateButtonAppearance(
                sfxEnableButton,
                sfxDisableButton,
                false
            );
        }
        else if (masterMuteStateSaved)
        {
            RestoreMusicAndSFXState();
            masterMuteStateSaved = false;

            Debug.Log(
                "AudioSettingsManager: Master restored. " +
                "previousSFXEnabled=" +
                previousSFXEnabled +
                ", currentSFXEnabled=" +
                AudioManager.Instance.IsSFXEnabled() +
                ", master=" +
                value +
                ", restoredSFXState=" +
                AudioManager.Instance.IsSFXEnabled()
            );
        }

        if (value > 0f)
        {
            previousMasterVolume = value;
        }

        bool enabled = value > 0f;

        UpdateButtonAppearance(
            volumeEnableButton,
            volumeDisableButton,
            enabled
        );
    }


    // =========================================================
    // ENABLE MASTER VOLUME
    // =========================================================

    void EnableVolume()
    {
        float value =
            previousMasterVolume > 0f ? previousMasterVolume : 1f;

        volumeSlider.SetValueWithoutNotify(value);

        AudioManager.Instance.SetMasterVolume(
            value
        );

        if (masterMuteStateSaved)
        {
            RestoreMusicAndSFXState();
            masterMuteStateSaved = false;

            Debug.Log(
                "AudioSettingsManager: Master enabled. " +
                "previousSFXEnabled=" +
                previousSFXEnabled +
                ", currentSFXEnabled=" +
                AudioManager.Instance.IsSFXEnabled() +
                ", master=" +
                value +
                ", restoredSFXState=" +
                AudioManager.Instance.IsSFXEnabled()
            );
        }

        UpdateButtonAppearance(
            volumeEnableButton,
            volumeDisableButton,
            true
        );
    }


    // =========================================================
    // DISABLE MASTER VOLUME
    // =========================================================

    void DisableVolume()
    {
        if (!masterMuteStateSaved)
        {
            SaveMusicAndSFXState();
            masterMuteStateSaved = true;
        }

        previousMasterVolume =
            volumeSlider.value > 0f ? volumeSlider.value : previousMasterVolume;

        volumeSlider.SetValueWithoutNotify(0f);

        AudioManager.Instance.SetMasterVolume(
            0f
        );

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            false
        );

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            false
        );

        UpdateButtonAppearance(
            volumeEnableButton,
            volumeDisableButton,
            false
        );
    }


    // =========================================================
    // MUSIC SLIDER
    // =========================================================

    void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(
            value
        );

        if (value > 0f)
        {
            previousMusicVolume = value;
        }

        bool enabled = value > 0f;

        AudioManager.Instance.SetMusicEnabled(enabled);

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            masterMuteStateSaved ? false : enabled
        );
    }


    // =========================================================
    // ENABLE MUSIC
    // =========================================================

    void EnableMusic()
    {
        float value =
            musicSlider.value > 0f ?
            musicSlider.value :
            (previousMusicVolume > 0f ? previousMusicVolume : 1f);

        musicSlider.SetValueWithoutNotify(value);

        AudioManager.Instance.SetMusicVolume(value);

        AudioManager.Instance.SetMusicEnabled(
            true
        );

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            !masterMuteStateSaved
        );
    }


    // =========================================================
    // DISABLE MUSIC
    // =========================================================

    void DisableMusic()
    {
        if (musicSlider.value > 0f)
        {
            previousMusicVolume =
                musicSlider.value;
        }

        AudioManager.Instance.SetMusicVolume(0f);
        musicSlider.SetValueWithoutNotify(0f);

        AudioManager.Instance.SetMusicEnabled(
            false
        );

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            false
        );
    }


    // =========================================================
    // SFX SLIDER
    // =========================================================

    void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(
            value
        );

        if (value > 0f)
        {
            previousSFXVolume = value;
        }

        bool enabled = value > 0f;

        AudioManager.Instance.SetSFXEnabled(enabled);

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            masterMuteStateSaved ? false : enabled
        );
    }


    // =========================================================
    // ENABLE SFX
    // =========================================================

    void EnableSFX()
    {
        float value =
            sfxSlider.value > 0f ?
            sfxSlider.value :
            (previousSFXVolume > 0f ? previousSFXVolume : 1f);

        sfxSlider.SetValueWithoutNotify(value);

        AudioManager.Instance.SetSFXVolume(value);

        AudioManager.Instance.SetSFXEnabled(
            true
        );

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            !masterMuteStateSaved
        );
    }


    // =========================================================
    // DISABLE SFX
    // =========================================================

    void DisableSFX()
    {
        if (sfxSlider.value > 0f)
        {
            previousSFXVolume =
                sfxSlider.value;
        }

        AudioManager.Instance.SetSFXVolume(0f);
        sfxSlider.SetValueWithoutNotify(0f);

        AudioManager.Instance.SetSFXEnabled(
            false
        );

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            false
        );
    }


    // =========================================================
    // SAVE AND RESTORE MUSIC/SFX STATE
    // =========================================================

    void SaveMusicAndSFXState()
    {
        float musicVolume =
            AudioManager.Instance.GetMusicVolume();

        float sfxVolume =
            AudioManager.Instance.GetSFXVolume();

        if (musicVolume > 0f)
        {
            previousMusicVolume =
                musicVolume;
        }

        if (sfxVolume > 0f)
        {
            previousSFXVolume =
                sfxVolume;
        }

        previousMusicEnabled =
            AudioManager.Instance.IsMusicEnabled();

        previousSFXEnabled =
            AudioManager.Instance.IsSFXEnabled();

        Debug.Log(
            "AudioSettingsManager: Saved master-mute state. " +
            "previousSFXEnabled=" +
            previousSFXEnabled +
            ", currentSFXEnabled=" +
            AudioManager.Instance.IsSFXEnabled() +
            ", master slider=" +
            volumeSlider.value
        );
    }

    void RestoreMusicAndSFXState()
    {
        float musicVolume =
            previousMusicVolume > 0f ? previousMusicVolume : 1f;

        float sfxVolume =
            previousSFXVolume > 0f ? previousSFXVolume : 1f;

        musicSlider.SetValueWithoutNotify(musicVolume);
        sfxSlider.SetValueWithoutNotify(sfxVolume);

        AudioManager.Instance.SetMusicVolume(musicVolume);
        AudioManager.Instance.SetSFXVolume(sfxVolume);

        AudioManager.Instance.SetMusicEnabled(
            previousMusicEnabled
        );

        AudioManager.Instance.SetSFXEnabled(
            previousSFXEnabled
        );

        Debug.Log(
            "AudioSettingsManager: Restored SFX state. " +
            "previousSFXEnabled=" +
            previousSFXEnabled +
            ", currentSFXEnabled=" +
            AudioManager.Instance.IsSFXEnabled() +
            ", restoredSFXState=" +
            previousSFXEnabled
        );

        UpdateButtonAppearance(
            musicEnableButton,
            musicDisableButton,
            previousMusicEnabled
        );

        UpdateButtonAppearance(
            sfxEnableButton,
            sfxDisableButton,
            previousSFXEnabled
        );
    }


    // =========================================================
    // BUTTON APPEARANCE
    // =========================================================

    void UpdateButtonAppearance(
        Button enableButton,
        Button disableButton,
        bool enabled
    )
    {
        SetButtonAlpha(
            enableButton,
            enabled ? 1f : 0f
        );

        SetButtonAlpha(
            disableButton,
            enabled ? 0f : 1f
        );
    }


    // =========================================================
    // SET BUTTON ALPHA
    // =========================================================

    void SetButtonAlpha(
        Button button,
        float alpha
    )
    {
        if (button == null)
            return;

        Image image =
            button.GetComponent<Image>();

        if (image == null)
            return;

        Color color =
            image.color;

        color.a = alpha;

        image.color = color;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(
                OnVolumeChanged
            );
        }

        if (volumeEnableButton != null)
        {
            volumeEnableButton.onClick.RemoveListener(
                EnableVolume
            );
        }

        if (volumeDisableButton != null)
        {
            volumeDisableButton.onClick.RemoveListener(
                DisableVolume
            );
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(
                OnMusicVolumeChanged
            );
        }

        if (musicEnableButton != null)
        {
            musicEnableButton.onClick.RemoveListener(
                EnableMusic
            );
        }

        if (musicDisableButton != null)
        {
            musicDisableButton.onClick.RemoveListener(
                DisableMusic
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(
                OnSFXVolumeChanged
            );
        }

        if (sfxEnableButton != null)
        {
            sfxEnableButton.onClick.RemoveListener(
                EnableSFX
            );
        }

        if (sfxDisableButton != null)
        {
            sfxDisableButton.onClick.RemoveListener(
                DisableSFX
            );
        }
    }
}