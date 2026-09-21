using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level6DragDevice : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // =====================================================
    // DEVICE CATEGORY
    // =====================================================

    public enum DeviceCategory
    {
        Input,
        Output
    }

    // =====================================================
    // DEVICE SETTINGS
    // =====================================================

    [Header("Device Settings")]
    public string deviceName;
    public DeviceCategory correctCategory;

    // =====================================================
    // SLOT POSITION
    // =====================================================

    [Header("Slot Offset (Relative to Center of Box)")]
    public Vector2 slotOffset;

    // =====================================================
    // COLORS
    // =====================================================

    [Header("Feedback Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;
    private Canvas canvas;
    private Camera uiCamera;

    private Transform originalParent;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    private bool isCorrect = false;
    private Level6Manager manager;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        manager = FindAnyObjectByType<Level6Manager>();

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;

        if (image != null)
            image.color = normalColor;
    }

    // =====================================================
    // BEGIN DRAG
    // =====================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isCorrect)
            return;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    // =====================================================
    // DRAG
    // =====================================================

    public void OnDrag(PointerEventData eventData)
    {
        if (isCorrect || canvas == null)
            return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            uiCamera,
            out localPointerPosition))
        {
            rectTransform.position =
                canvasRect.TransformPoint(localPointerPosition);
        }
    }

    // =====================================================
    // END DRAG
    // =====================================================

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isCorrect)
            return;

        canvasGroup.blocksRaycasts = true;

        GameObject droppedObject =
            eventData.pointerCurrentRaycast.gameObject;

        if (droppedObject == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (IsCorrectDrop(droppedObject))
            CorrectPlacement();
        else
            WrongPlacement();
    }

    // =====================================================
    // CHECK DROP AREA
    // =====================================================

    private bool IsCorrectDrop(GameObject droppedObject)
    {
        Level6DropZone zone =
            droppedObject.GetComponentInParent<Level6DropZone>();

        if (zone == null)
            return false;

        return zone.category == correctCategory;
    }

    // =====================================================
    // CORRECT PLACEMENT
    // =====================================================

    private void CorrectPlacement()
    {
        isCorrect = true;
        StartCoroutine(CorrectAnimation());
    }

    private IEnumerator CorrectAnimation()
    {
        if (image != null)
            image.color = correctColor;

        if (manager != null)
            manager.PlayCorrectSFX();

        yield return new WaitForSeconds(0.2f);

        RectTransform targetArea =
            manager.GetDropArea(correctCategory);

        if (targetArea == null)
        {
            Debug.LogError(
                "Level6: Correct drop area is not assigned for " +
                correctCategory
            );

            ReturnToOriginalPosition();
            yield break;
        }

        // Parent into correct box
        transform.SetParent(targetArea, false);

        // Center anchors
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Snap to assigned slot
        rectTransform.anchoredPosition = slotOffset;

        // Keep original scale
        rectTransform.localScale = originalScale;

        if (image != null)
            image.color = correctColor;

        // Lock device
        isCorrect = true;
        canvasGroup.blocksRaycasts = false;

        if (image != null)
            image.raycastTarget = false;

        if (manager != null)
            manager.DevicePlacedCorrectly();
    }

    // =====================================================
    // WRONG PLACEMENT
    // =====================================================

    private void WrongPlacement()
    {
        if (manager != null)
        {
            manager.PlayWrongSFX();
            manager.DeductTime(5f); // ✅ Deduct 5 seconds + blink timer red
        }

        StartCoroutine(WrongAnimation());
    }

    private IEnumerator WrongAnimation()
    {
        if (image != null)
            image.color = wrongColor;

        yield return new WaitForSeconds(0.12f);

        if (image != null)
            image.color = normalColor;

        yield return new WaitForSeconds(0.12f);

        if (image != null)
            image.color = wrongColor;

        yield return new WaitForSeconds(0.12f);

        if (image != null)
            image.color = normalColor;

        ReturnToOriginalPosition();
    }

    // =====================================================
    // RETURN TO ORIGINAL POSITION
    // =====================================================

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent, false);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;

        if (image != null)
        {
            image.color = normalColor;
            image.raycastTarget = true;
        }

        canvasGroup.blocksRaycasts = true;
    }
}