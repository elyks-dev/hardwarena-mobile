using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [SerializeField] private AudioSource cutsceneAudioSource;

    [Header("UI")]
    [SerializeField] private GameObject continueButton;

    [SerializeField] private Button buttonToDisable;

    [Header("Scene")]
    [SerializeField] private string levelScene = "Level1Scene";

    private bool audioManagerWarningLogged;
    private bool audioSourceWarningLogged;

    void Start()
    {
        continueButton.SetActive(false);

        // Make sure the button starts enabled
        if (buttonToDisable != null)
        {
            buttonToDisable.interactable = true;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.OnAudioSettingsChanged -=
                UpdateCutsceneAudioVolume;
            AudioManager.Instance.OnAudioSettingsChanged +=
                UpdateCutsceneAudioVolume;
        }
        else
        {
            LogAudioManagerWarning();
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;

            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;

            ConfigureCutsceneAudioOutput();
        }

        UpdateCutsceneAudioVolume();

        // VideoDisplayScaler handles Prepare() and Play().
    }

    void OnVideoFinished(VideoPlayer player)
    {
        // Show Continue button
        continueButton.SetActive(true);

        // Disable the other button
        DisableButton();
    }

    void OnVideoPrepared(VideoPlayer player)
    {
        ConfigureCutsceneAudioOutput();
        UpdateCutsceneAudioVolume();
    }

    void ConfigureCutsceneAudioOutput()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.audioOutputMode !=
            VideoAudioOutputMode.AudioSource)
        {
            return;
        }

        if (cutsceneAudioSource == null)
        {
            if (!audioSourceWarningLogged)
            {
                Debug.LogWarning(
                    "CutsceneManager: VideoPlayer uses AudioSource output, " +
                    "but cutsceneAudioSource is not assigned."
                );

                audioSourceWarningLogged = true;
            }

            return;
        }

        if (videoPlayer.audioTrackCount > 0)
        {
            videoPlayer.SetTargetAudioSource(
                0,
                cutsceneAudioSource
            );
        }
    }

    void UpdateCutsceneAudioVolume()
    {
        if (videoPlayer == null)
            return;

        if (AudioManager.Instance == null)
        {
            LogAudioManagerWarning();
            return;
        }

        float masterVolume =
            AudioManager.Instance.GetMasterVolume();

        float musicVolume =
            AudioManager.Instance.GetMusicVolume();

        bool musicEnabled =
            AudioManager.Instance.IsMusicEnabled();

        float cutsceneVolume =
            masterVolume > 0f &&
            musicVolume > 0f &&
            musicEnabled ?
            masterVolume * musicVolume :
            0f;

        cutsceneVolume =
            Mathf.Clamp01(cutsceneVolume);

        if (videoPlayer.audioOutputMode ==
            VideoAudioOutputMode.AudioSource)
        {
            if (cutsceneAudioSource == null)
                return;

            cutsceneAudioSource.volume =
                cutsceneVolume;

            cutsceneAudioSource.mute =
                cutsceneVolume <= 0f;
        }
        else if (videoPlayer.audioOutputMode ==
                 VideoAudioOutputMode.Direct &&
                 videoPlayer.audioTrackCount > 0)
        {
            videoPlayer.SetDirectAudioVolume(
                0,
                cutsceneVolume
            );

            videoPlayer.SetDirectAudioMute(
                0,
                cutsceneVolume <= 0f
            );
        }
    }

    void LogAudioManagerWarning()
    {
        if (audioManagerWarningLogged)
            return;

        Debug.LogWarning(
            "CutsceneManager: AudioManager is not available. " +
            "Cutscene audio volume cannot be synchronized."
        );

        audioManagerWarningLogged = true;
    }

    public void DisableButton()
    {
        if (buttonToDisable != null)
        {
            buttonToDisable.interactable = false;
        }
    }

    public void ContinueToLevel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeBackgroundMusic();
        }

        SceneManager.LoadScene(levelScene);
    }

    void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -=
                UpdateCutsceneAudioVolume;
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}