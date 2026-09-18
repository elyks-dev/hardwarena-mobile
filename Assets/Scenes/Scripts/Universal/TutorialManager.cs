using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialManager : MonoBehaviour
{
    // =====================================================
    // VIDEO
    // =====================================================

    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Tooltip("AudioSource used when the VideoPlayer output mode is AudioSource.")]
    public AudioSource tutorialAudioSource;

    [Tooltip("The parent RectTransform containing the video.")]
    public RectTransform videoArea;

    [Tooltip("The TutorialPanel RectTransform used as the fullscreen container.")]
    public RectTransform tutorialPanel;

    [Tooltip("The RawImage displaying the video.")]
    public RawImage videoRawImage;

    [Tooltip("Aspect Ratio Fitter attached to the Video RawImage.")]
    public AspectRatioFitter aspectRatioFitter;

    // =====================================================
    // PLAY / PAUSE
    // =====================================================

    [Header("Play / Pause")]
    public Button playPauseButton;

    public Sprite playSprite;
    public Sprite pauseSprite;

    // =====================================================
    // FULLSCREEN
    // =====================================================

    [Header("Fullscreen")]
    public Button fullscreenButton;

    public RectTransform fullscreenBackground;

    public Sprite fullscreenSprite;
    public Sprite unfullscreenSprite;

    // =====================================================
    // STATE
    // =====================================================

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private bool tutorialCompleted = false;
    private bool isFullscreen = false;
    private bool audioMutedForTutorial = false;

    private float previousMasterVolume;
    private float previousMusicVolume;
    private float previousSFXVolume;
    private bool previousMusicEnabled;
    private bool previousSFXEnabled;
    private bool audioManagerWarningLogged;

    // =====================================================
    // ORIGINAL VIDEO AREA
    // =====================================================

    private int originalSiblingIndex;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalSizeDelta;
    private Vector2 originalAnchoredPosition;
    private Vector2 originalOffsetMin;
    private Vector2 originalOffsetMax;
    private Vector3 originalLocalScale;

    private int originalBackgroundSiblingIndex;
    private Vector2 originalBackgroundAnchorMin;
    private Vector2 originalBackgroundAnchorMax;
    private Vector2 originalBackgroundPivot;
    private Vector2 originalBackgroundSizeDelta;
    private Vector2 originalBackgroundAnchoredPosition;
    private Vector2 originalBackgroundOffsetMin;
    private Vector2 originalBackgroundOffsetMax;
    private Vector3 originalBackgroundLocalScale;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        tutorialCompleted = false;
        isFullscreen = false;

        if (fullscreenBackground != null)
        {
            fullscreenBackground.gameObject.SetActive(false);
        }

        // =================================================
        // VIDEO SETUP
        // =================================================

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached -= OnTutorialFinished;
            videoPlayer.loopPointReached += OnTutorialFinished;

            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;

            ConfigureTutorialAudioOutput();

            videoPlayer.Stop();
        }

        // =================================================
        // PLAY / PAUSE BUTTON
        // =================================================

        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveAllListeners();
            playPauseButton.onClick.AddListener(TogglePlayPause);

            SetPlaySprite();
        }

        // =================================================
        // FULLSCREEN BUTTON
        // =================================================

        if (fullscreenButton != null)
        {
            fullscreenButton.onClick.RemoveAllListeners();
            fullscreenButton.onClick.AddListener(ToggleFullscreen);

            SetFullscreenSprite();
        }

        // =================================================
        // INITIAL ASPECT RATIO
        // =================================================

        UpdateAspectRatio();
    }

    // =====================================================
    // PLAY / PAUSE
    // =====================================================

    public void TogglePlayPause()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
        {
            // Pause video
            videoPlayer.Pause();

            RestoreAudioAfterTutorial();

            // Change button to Play
            SetPlaySprite();
        }
        else
        {
            MuteAudioForTutorial();
            UpdateTutorialAudioVolume();

            // Play video
            videoPlayer.Play();

            // Change button to Pause
            SetPauseSprite();
        }
    }

    // =====================================================
    // PLAY SPRITE
    // =====================================================

    private void SetPlaySprite()
    {
        if (playPauseButton == null)
            return;

        Image buttonImage =
            playPauseButton.GetComponent<Image>();

        if (buttonImage != null &&
            playSprite != null)
        {
            buttonImage.sprite = playSprite;
        }
    }

    // =====================================================
    // PAUSE SPRITE
    // =====================================================

    private void SetPauseSprite()
    {
        if (playPauseButton == null)
            return;

        Image buttonImage =
            playPauseButton.GetComponent<Image>();

        if (buttonImage != null &&
            pauseSprite != null)
        {
            buttonImage.sprite = pauseSprite;
        }
    }

    // =====================================================
    // VIDEO PREPARED
    // =====================================================

    private void OnVideoPrepared(VideoPlayer player)
    {
        ConfigureTutorialAudioOutput();
        UpdateAspectRatio();
        UpdateTutorialAudioVolume();
    }

    // =====================================================
    // UPDATE ASPECT RATIO
    // =====================================================

    private void UpdateAspectRatio()
    {
        if (videoPlayer == null)
            return;

        if (aspectRatioFitter == null)
            return;

        if (videoPlayer.width <= 0 ||
            videoPlayer.height <= 0)
        {
            return;
        }

        float aspectRatio =
            (float)videoPlayer.width /
            (float)videoPlayer.height;

        aspectRatioFitter.aspectRatio =
            aspectRatio;
    }

    // =====================================================
    // TUTORIAL AUDIO OUTPUT
    // =====================================================

    private void ConfigureTutorialAudioOutput()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.audioOutputMode ==
            VideoAudioOutputMode.AudioSource)
        {
            if (tutorialAudioSource == null)
            {
                Debug.LogWarning(
                    "TutorialManager: VideoPlayer uses AudioSource output, " +
                    "but tutorialAudioSource is not assigned."
                );

                return;
            }

            if (videoPlayer.audioTrackCount > 0)
            {
                videoPlayer.SetTargetAudioSource(
                    0,
                    tutorialAudioSource
                );
            }
        }
    }

    private void UpdateTutorialAudioVolume()
    {
        if (videoPlayer == null)
            return;

        if (AudioManager.Instance == null)
        {
            if (!audioManagerWarningLogged)
            {
                Debug.LogWarning(
                    "TutorialManager: AudioManager is not available. " +
                    "Tutorial audio volume cannot be synchronized."
                );

                audioManagerWarningLogged = true;
            }

            return;
        }

        float masterVolume =
            audioMutedForTutorial ?
            previousMasterVolume :
            AudioManager.Instance.GetMasterVolume();

        float musicVolume =
            AudioManager.Instance.GetMusicVolume();

        bool musicEnabled =
            AudioManager.Instance.IsMusicEnabled();

        float tutorialVolume =
            masterVolume > 0f &&
            musicVolume > 0f &&
            musicEnabled ?
            masterVolume * musicVolume :
            0f;

        tutorialVolume =
            Mathf.Clamp01(tutorialVolume);

        if (videoPlayer.audioOutputMode ==
            VideoAudioOutputMode.AudioSource)
        {
            if (tutorialAudioSource != null)
            {
                tutorialAudioSource.volume =
                    tutorialVolume;

                tutorialAudioSource.mute =
                    false;
            }
        }
        else if (videoPlayer.audioOutputMode ==
                 VideoAudioOutputMode.Direct &&
                 videoPlayer.audioTrackCount > 0)
        {
            videoPlayer.SetDirectAudioVolume(
                0,
                tutorialVolume
            );

            videoPlayer.SetDirectAudioMute(
                0,
                tutorialVolume <= 0f
            );
        }
    }

    // =====================================================
    // FULLSCREEN TOGGLE
    // =====================================================

    public void ToggleFullscreen()
    {
        if (isFullscreen)
        {
            ExitFullscreen();
        }
        else
        {
            EnterFullscreen();
        }
    }

    // =====================================================
    // ENTER FULLSCREEN
    // =====================================================

    private void EnterFullscreen()
    {
        if (videoArea == null)
            return;

        if (tutorialPanel == null)
            return;

        originalSiblingIndex =
            videoArea.GetSiblingIndex();

        originalAnchorMin =
            videoArea.anchorMin;

        originalAnchorMax =
            videoArea.anchorMax;

        originalPivot =
            videoArea.pivot;

        originalSizeDelta =
            videoArea.sizeDelta;

        originalAnchoredPosition =
            videoArea.anchoredPosition;

        originalOffsetMin =
            videoArea.offsetMin;

        originalOffsetMax =
            videoArea.offsetMax;

        originalLocalScale =
            videoArea.localScale;

        if (fullscreenBackground != null)
        {
            originalBackgroundSiblingIndex =
                fullscreenBackground.GetSiblingIndex();

            originalBackgroundAnchorMin =
                fullscreenBackground.anchorMin;

            originalBackgroundAnchorMax =
                fullscreenBackground.anchorMax;

            originalBackgroundPivot =
                fullscreenBackground.pivot;

            originalBackgroundSizeDelta =
                fullscreenBackground.sizeDelta;

            originalBackgroundAnchoredPosition =
                fullscreenBackground.anchoredPosition;

            originalBackgroundOffsetMin =
                fullscreenBackground.offsetMin;

            originalBackgroundOffsetMax =
                fullscreenBackground.offsetMax;

            originalBackgroundLocalScale =
                fullscreenBackground.localScale;

        }

        isFullscreen = true;

        if (fullscreenBackground != null)
        {
            fullscreenBackground.localScale =
                Vector3.one;

            fullscreenBackground.anchorMin =
                new Vector2(0f, 0f);

            fullscreenBackground.anchorMax =
                new Vector2(1f, 1f);

            fullscreenBackground.pivot =
                new Vector2(0.5f, 0.5f);

            fullscreenBackground.offsetMin =
                Vector2.zero;

            fullscreenBackground.offsetMax =
                Vector2.zero;

            fullscreenBackground.anchoredPosition =
                Vector2.zero;

            fullscreenBackground.sizeDelta =
                Vector2.zero;

            fullscreenBackground.gameObject.SetActive(true);

            fullscreenBackground.SetSiblingIndex(0);
        }

        videoArea.localScale =
            Vector3.one;

        videoArea.anchorMin =
            new Vector2(0f, 0f);

        videoArea.anchorMax =
            new Vector2(1f, 1f);

        videoArea.pivot =
            new Vector2(0.5f, 0.5f);

        videoArea.sizeDelta =
            Vector2.zero;

        videoArea.offsetMin =
            Vector2.zero;

        videoArea.offsetMax =
            Vector2.zero;

        videoArea.anchoredPosition =
            Vector2.zero;

        if (fullscreenBackground != null)
        {
            videoArea.SetSiblingIndex(
                fullscreenBackground.GetSiblingIndex() + 1
            );
        }

        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;
        }

        UpdateAspectRatio();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "TutorialManager: Entered fullscreen. " +
            "VideoPanel parent=" +
            videoArea.parent.name +
            ", size=" +
            videoArea.rect.width +
            "x" +
            videoArea.rect.height +
            ", VideoRawImage size=" +
            GetRawImageRectSize() +
            ", aspect ratio=" +
            GetVideoAspectRatio()
        );

        // Change button icon
        SetUnfullscreenSprite();
    }

    // =====================================================
    // EXIT FULLSCREEN
    // =====================================================

    private void ExitFullscreen()
    {
        if (videoArea == null)
            return;

        isFullscreen = false;

        videoArea.anchorMin =
            originalAnchorMin;

        videoArea.anchorMax =
            originalAnchorMax;

        videoArea.pivot =
            originalPivot;

        videoArea.sizeDelta =
            originalSizeDelta;

        videoArea.anchoredPosition =
            originalAnchoredPosition;

        videoArea.offsetMin =
            originalOffsetMin;

        videoArea.offsetMax =
            originalOffsetMax;

        videoArea.localScale =
            originalLocalScale;

        videoArea.SetSiblingIndex(
            originalSiblingIndex
        );

        if (fullscreenBackground != null)
        {
            fullscreenBackground.anchorMin =
                originalBackgroundAnchorMin;

            fullscreenBackground.anchorMax =
                originalBackgroundAnchorMax;

            fullscreenBackground.pivot =
                originalBackgroundPivot;

            fullscreenBackground.sizeDelta =
                originalBackgroundSizeDelta;

            fullscreenBackground.anchoredPosition =
                originalBackgroundAnchoredPosition;

            fullscreenBackground.offsetMin =
                originalBackgroundOffsetMin;

            fullscreenBackground.offsetMax =
                originalBackgroundOffsetMax;

            fullscreenBackground.localScale =
                originalBackgroundLocalScale;

            fullscreenBackground.SetSiblingIndex(
                originalBackgroundSiblingIndex
            );

            fullscreenBackground.gameObject.SetActive(
                false
            );
        }

        // Preserve aspect ratio
        UpdateAspectRatio();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "TutorialManager: Exited fullscreen. " +
            "VideoPanel parent=" +
            videoArea.parent.name +
            ", size=" +
            videoArea.rect.width +
            "x" +
            videoArea.rect.height +
            ", VideoRawImage size=" +
            GetRawImageRectSize() +
            ", aspect ratio=" +
            GetVideoAspectRatio()
        );

        // Change button icon
        SetFullscreenSprite();
    }

    // =====================================================
    // FULLSCREEN SPRITE
    // =====================================================

    private void SetFullscreenSprite()
    {
        if (fullscreenButton == null)
            return;

        Image buttonImage =
            fullscreenButton.GetComponent<Image>();

        if (buttonImage != null &&
            fullscreenSprite != null)
        {
            buttonImage.sprite =
                fullscreenSprite;
        }
    }

    // =====================================================
    // UNFULLSCREEN SPRITE
    // =====================================================

    private void SetUnfullscreenSprite()
    {
        if (fullscreenButton == null)
            return;

        Image buttonImage =
            fullscreenButton.GetComponent<Image>();

        if (buttonImage != null &&
            unfullscreenSprite != null)
        {
            buttonImage.sprite =
                unfullscreenSprite;
        }
    }

    private string GetRawImageRectSize()
    {
        if (videoRawImage == null)
            return "0x0";

        Rect rawImageRect =
            videoRawImage.rectTransform.rect;

        return rawImageRect.width +
               "x" +
               rawImageRect.height;
    }

    private float GetVideoAspectRatio()
    {
        if (videoPlayer == null ||
            videoPlayer.width <= 0 ||
            videoPlayer.height <= 0)
        {
            return 0f;
        }

        return (float)videoPlayer.width /
               (float)videoPlayer.height;
    }

    // =====================================================
    // TUTORIAL FINISHED
    // =====================================================

    private void OnTutorialFinished(VideoPlayer player)
    {
        // Video has reached the end
        RestoreAudioAfterTutorial();

        SetPlaySprite();

        CompleteTutorial();
    }

    // =====================================================
    // TEMPORARY TUTORIAL AUDIO MUTE
    // =====================================================

    private void MuteAudioForTutorial()
    {
        if (audioMutedForTutorial)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "TutorialManager: AudioManager is not available."
            );

            return;
        }

        previousMasterVolume =
            AudioManager.Instance.GetMasterVolume();

        previousMusicVolume =
            AudioManager.Instance.GetMusicVolume();

        previousSFXVolume =
            AudioManager.Instance.GetSFXVolume();

        previousMusicEnabled =
            AudioManager.Instance.IsMusicEnabled();

        previousSFXEnabled =
            AudioManager.Instance.IsSFXEnabled();

        audioMutedForTutorial = true;

        // Master volume zero mutes both music and SFX without changing
        // their individual volume or enabled states.
        AudioManager.Instance.SetMasterVolume(0f);
    }

    private void RestoreAudioAfterTutorial()
    {
        if (!audioMutedForTutorial)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "TutorialManager: AudioManager is not available while restoring audio."
            );

            audioMutedForTutorial = false;
            return;
        }

        AudioManager.Instance.SetMasterVolume(
            previousMasterVolume
        );

        AudioManager.Instance.SetMusicVolume(
            previousMusicVolume
        );

        AudioManager.Instance.SetSFXVolume(
            previousSFXVolume
        );

        AudioManager.Instance.SetMusicEnabled(
            previousMusicEnabled
        );

        AudioManager.Instance.SetSFXEnabled(
            previousSFXEnabled
        );

        UpdateTutorialAudioVolume();

        audioMutedForTutorial = false;
    }

    // =====================================================
    // COMPLETE TUTORIAL
    // =====================================================

    private async void CompleteTutorial()
    {
        if (tutorialCompleted)
            return;

        tutorialCompleted = true;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "TutorialManager: No Firebase user logged in."
            );

            return;
        }

        try
        {
            DocumentReference badgeRef =
                db.Collection("users")
                  .Document(user.UserId)
                  .Collection("badges")
                  .Document("first_steps");

            await badgeRef.SetAsync(
                new Dictionary<string, object>()
                {
                    {
                        "unlocked",
                        true
                    },
                    {
                        "unlockedAt",
                        Timestamp.GetCurrentTimestamp()
                    }
                },
                SetOptions.MergeAll
            );

            Debug.Log(
                "Tutorial completed. First Steps badge saved."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "TutorialManager: Failed to save First Steps badge: " +
                e.Message
            );
        }
    }

    // =====================================================
    // MANUAL COMPLETE
    // =====================================================

    public void MarkTutorialCompleted()
    {
        CompleteTutorial();
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        RestoreAudioAfterTutorial();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -=
                OnTutorialFinished;

            videoPlayer.prepareCompleted -=
                OnVideoPrepared;
        }
    }
}