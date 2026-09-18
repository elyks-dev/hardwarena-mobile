using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level5DragLabel : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // =====================================================
    // LABEL SETTINGS
    // =====================================================

    [Header("Label Settings")]
    public string labelName;

    // =====================================================
    // DROP AREA
    // =====================================================

    [Header("Correct Drop Area")]
    public RectTransform correctDropArea;

    // =====================================================
    // FEEDBACK COLORS
    // =====================================================

    [Header("Feedback Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    // =====================================================
    // CORRECT PLACEMENT SIZE
    // =====================================================

    [Header("Correct Placement Scale")]
    public Vector3 placedScale = new Vector3(0.65f, 0.65f, 0.65f);

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;

    private Vector2 originalPosition;
    private Transform originalParent;
    private Vector3 originalScale;

    private Canvas canvas;
    private Camera uiCamera;

    private bool isCorrect = false;
    private Level5Manager manager;

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

        manager = FindAnyObjectByType<Level5Manager>();

        canvas = GetComponentInParent<Canvas>();

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

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
        if (isCorrect)
            return;

        if (canvas == null)
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

        if (IsCorrectDropArea(droppedObject))
        {
            CorrectPlacement();
        }
        else
        {
            WrongPlacement();
        }
    }

    // =====================================================
    // CHECK DROP AREA
    // =====================================================

    private bool IsCorrectDropArea(GameObject droppedObject)
    {
        if (correctDropArea == null)
            return false;

        Transform droppedTransform = droppedObject.transform;

        if (droppedTransform == correctDropArea)
            return true;

        if (droppedTransform.IsChildOf(correctDropArea))
            return true;

        return false;
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
        // Flash green
        if (image != null)
            image.color = correctColor;

        // Play correct sound
        if (manager != null)
            manager.PlayCorrectSFX();

        yield return new WaitForSeconds(0.25f);

        // Snap into the correct drop area
        rectTransform.position = correctDropArea.position;

        // Make label smaller
        rectTransform.localScale = placedScale;

        if (image != null)
            image.color = correctColor;

        if (manager != null)
            manager.LabelPlacedCorrectly();
    }

    // =====================================================
    // WRONG PLACEMENT
    // =====================================================

    private void WrongPlacement()
    {
        if (manager != null)
            manager.PlayWrongSFX();

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

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;

        if (image != null)
            image.color = normalColor;
    }
}