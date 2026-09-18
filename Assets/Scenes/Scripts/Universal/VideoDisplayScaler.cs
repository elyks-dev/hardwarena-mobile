using UnityEngine;
using UnityEngine.Video;

public class VideoDisplayScaler : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private RectTransform rectTransform;
    private RectTransform parentRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoDisplayScaler: VideoPlayer is not assigned.");
            return;
        }

        if (parentRect == null)
        {
            Debug.LogError("VideoDisplayScaler: Parent RectTransform not found.");
            return;
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (rectTransform == null || parentRect == null)
            return;

        float videoWidth = player.width;
        float videoHeight = player.height;

        if (videoWidth <= 0 || videoHeight <= 0)
        {
            Debug.LogWarning("VideoDisplayScaler: Invalid video dimensions.");
            return;
        }

        float videoAspect = videoWidth / videoHeight;

        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        if (parentWidth <= 0 || parentHeight <= 0)
            return;

        float parentAspect = parentWidth / parentHeight;

        float finalWidth;
        float finalHeight;

        // Keep original video proportions.
        if (parentAspect > videoAspect)
        {
            // Parent is wider than the video.
            finalHeight = parentHeight;
            finalWidth = finalHeight * videoAspect;
        }
        else
        {
            // Parent is taller/narrower than the video.
            finalWidth = parentWidth;
            finalHeight = finalWidth / videoAspect;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);
        rectTransform.localScale = Vector3.one;

        player.Play();

        Debug.Log(
            $"VideoDisplayScaler: Video {videoWidth}x{videoHeight}, " +
            $"Display {finalWidth}x{finalHeight}"
        );
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}