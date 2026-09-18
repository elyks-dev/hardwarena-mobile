using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level8DragItem :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Level 8 References")]
    public Level8Manager manager;

    [Header("Correct Drop Area")]
    public Level8DropArea correctDropArea;

    [Header("Plugged-In Image")]
    public GameObject pluggedInItem;

    // =====================================================
    // TRANSFORM
    // =====================================================

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    // =====================================================
    // STATE
    // =====================================================

    private bool dragging = false;
    private bool placed = false;
    private Coroutine flickerCoroutine;
    private Color flickerOriginalColor;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        originalParent =
            transform.parent;

        originalPosition =
            rectTransform.anchoredPosition;

        originalScale =
            rectTransform.localScale;
    }

    // =====================================================
    // BEGIN DRAG
    // =====================================================

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (placed)
            return;

        if (manager == null)
            return;

        dragging = true;

        canvasGroup.blocksRaycasts = false;

        // Highlight this item's correct drop area.
        if (correctDropArea != null)
        {
            correctDropArea.Highlight();
        }
    }

    // =====================================================
    // DRAG
    // =====================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging || placed)
            return;

        rectTransform.position =
            eventData.position;
    }

    // =====================================================
    // END DRAG
    // =====================================================

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || placed)
            return;

        dragging = false;

        canvasGroup.blocksRaycasts = true;

        if (correctDropArea != null)
        {
            correctDropArea.ResetColor();
        }

        ResetPosition();
    }

    // =====================================================
    // WRONG DROP VISUAL
    // =====================================================

    public void FlickerWrong()
    {
        Image image =
            GetComponent<Image>();

        if (image == null)
            return;

        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            image.color =
                flickerOriginalColor;
        }

        flickerOriginalColor =
            image.color;

        flickerCoroutine =
            StartCoroutine(FlickerRed(image));
    }

    private IEnumerator FlickerRed(Image image)
    {
        Color originalColor =
            flickerOriginalColor;

        Color redColor =
            new Color(
                1f,
                0f,
                0f,
                originalColor.a
            );

        image.color = redColor;
        yield return new WaitForSeconds(0.1f);

        image.color = originalColor;
        yield return new WaitForSeconds(0.1f);

        image.color = redColor;
        yield return new WaitForSeconds(0.1f);

        image.color = originalColor;
        flickerCoroutine = null;
    }

    // =====================================================
    // CORRECT PLACEMENT
    // =====================================================

    public void PlaceCorrectly()
    {
        if (placed)
            return;

        placed = true;
        dragging = false;

        canvasGroup.blocksRaycasts = false;

        // Show plugged-in image.
        if (pluggedInItem != null)
        {
            pluggedInItem.SetActive(true);
        }

        // Hide draggable image.
        gameObject.SetActive(false);
    }

    // =====================================================
    // RESET POSITION
    // =====================================================

    private void ResetPosition()
    {
        transform.SetParent(
            originalParent,
            false
        );

        rectTransform.anchoredPosition =
            originalPosition;

        rectTransform.localScale =
            originalScale;
    }

    // =====================================================
    // RESET ITEM
    // =====================================================

    public void ResetItem()
    {
        placed = false;
        dragging = false;

        gameObject.SetActive(true);

        transform.SetParent(
            originalParent,
            false
        );

        rectTransform.anchoredPosition =
            originalPosition;

        rectTransform.localScale =
            originalScale;

        canvasGroup.blocksRaycasts = true;
    }
}