using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelCompletionCutsceneManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource cutsceneAudioSource;

    [Header("Level Completion Popup")]
    [SerializeField] private GameObject levelCompletionPopup;
    [SerializeField] private Button exitButton;
    [SerializeField] private float popupDelay = 2f;

    [Header("Scene")]
    [SerializeField] private string exitScene = "LevelsScene";

    private bool audioManagerWarningLogged;
    private bool audioSourceWarningLogged;

    void Start()
    {
        // Hide popup when scene starts.
        if (levelCompletionPopup != null)
        {
            levelCompletionPopup.SetActive(false);
        }

        // Exit button.
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitLevel);
        }

        // Stop background music during cutscene.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.OnAudioSettingsChanged -= UpdateCutsceneAudioVolume;
            AudioManager.Instance.OnAudioSettingsChanged += UpdateCutsceneAudioVolume;
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

    // =========================================================
    // VIDEO FINISHED
    // =========================================================

    void OnVideoFinished(VideoPlayer player)
    {
        StartCoroutine(ShowCompletionPopupAfterDelay());
    }

    private IEnumerator ShowCompletionPopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDelay);

        if (levelCompletionPopup != null)
        {
            levelCompletionPopup.SetActive(true);
        }
    }

    void OnVideoPrepared(VideoPlayer player)
    {
        ConfigureCutsceneAudioOutput();
        UpdateCutsceneAudioVolume();
    }

    // =========================================================
    // AUDIO SETUP
    // =========================================================

    void ConfigureCutsceneAudioOutput()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.audioOutputMode != VideoAudioOutputMode.AudioSource)
            return;

        if (cutsceneAudioSource == null)
        {
            if (!audioSourceWarningLogged)
            {
                Debug.LogWarning(
                    "LevelCompletionCutsceneManager: VideoPlayer uses AudioSource output, but cutsceneAudioSource is not assigned."
                );

                audioSourceWarningLogged = true;
            }

            return;
        }

        if (videoPlayer.audioTrackCount > 0)
        {
            videoPlayer.SetTargetAudioSource(0, cutsceneAudioSource);
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

        float masterVolume = AudioManager.Instance.GetMasterVolume();
        float musicVolume = AudioManager.Instance.GetMusicVolume();
        bool musicEnabled = AudioManager.Instance.IsMusicEnabled();

        float cutsceneVolume =
            masterVolume > 0f &&
            musicVolume > 0f &&
            musicEnabled
                ? masterVolume * musicVolume
                : 0f;

        cutsceneVolume = Mathf.Clamp01(cutsceneVolume);

        if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            if (cutsceneAudioSource == null)
                return;

            cutsceneAudioSource.volume = cutsceneVolume;
            cutsceneAudioSource.mute = cutsceneVolume <= 0f;
        }
        else if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct &&
                 videoPlayer.audioTrackCount > 0)
        {
            videoPlayer.SetDirectAudioVolume(0, cutsceneVolume);
            videoPlayer.SetDirectAudioMute(0, cutsceneVolume <= 0f);
        }
    }

    void LogAudioManagerWarning()
    {
        if (audioManagerWarningLogged)
            return;

        Debug.LogWarning(
            "LevelCompletionCutsceneManager: AudioManager is not available. Cutscene audio volume cannot be synchronized."
        );

        audioManagerWarningLogged = true;
    }

    // =========================================================
    // EXIT LEVEL
    // =========================================================

    public void ExitLevel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeBackgroundMusic();
        }

        SceneManager.LoadScene(exitScene);
    }

    // =========================================================
    // CLEAN UP
    // =========================================================

    void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -= UpdateCutsceneAudioVolume;
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitLevel);
        }
    }
}