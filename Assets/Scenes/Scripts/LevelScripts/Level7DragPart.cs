using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level7DragPart : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // =====================================================
    // PART SETTINGS
    // =====================================================

    [Header("Part Settings")]
    public string partName;
    public Level7DropZone.PartType partType;

    // =====================================================
    // CORRECT DROP ZONES
    // =====================================================

    [Header("Correct Drop Zones")]
    public Level7DropZone[] correctDropZones;

    // =====================================================
    // PLACED SPRITE
    // =====================================================

    [Header("Placed Sprite")]
    public Sprite placedSprite;

    // =====================================================
    // FEEDBACK COLORS
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
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Vector2 originalSizeDelta;

    private Sprite originalSprite;
    private bool placed = false;

    private Level7Manager manager;

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

        manager = FindAnyObjectByType<Level7Manager>();

        // Save original transform settings.
        originalParent = transform.parent;
        originalSiblingIndex =
            transform.parent != null ?
            transform.GetSiblingIndex() :
            0;
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        originalSizeDelta = rectTransform.sizeDelta;

        // Save original sprite.
        if (image != null)
            originalSprite = image.sprite;

        // Original appearance.
        if (image != null)
            image.color = normalColor;
    }

    // =====================================================
    // BEGIN DRAG
    // =====================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        if (manager == null)
            return;

        // Only the part required for the current step
        // can be dragged.
        if (!manager.CanDragPart(this))
            return;

        canvasGroup.blocksRaycasts = false;

        // Bring the dragged part to the front.
        transform.SetAsLastSibling();

        // Highlight valid drop zones.
        manager.HighlightDropZones(partType);

        // Keep the dragged part above the highlighted drop zone.
        transform.SetAsLastSibling();
    }

    // =====================================================
    // DRAG
    // =====================================================

    public void OnDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        if (manager == null)
            return;

        if (!manager.CanDragPart(this))
            return;

        if (canvas == null)
            return;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

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
        if (placed)
            return;

        if (manager == null)
            return;

        if (!manager.CanDragPart(this))
            return;

        if (transform.parent != null)
        {
            transform.SetSiblingIndex(
                originalSiblingIndex
            );
        }

        canvasGroup.blocksRaycasts = true;

        manager.ResetDropZoneHighlights();

        GameObject droppedObject =
            eventData.pointerCurrentRaycast.gameObject;

        if (droppedObject == null)
        {
            WrongPlacement();
            return;
        }

        Level7DropZone zone =
            droppedObject.GetComponentInParent<Level7DropZone>();

        if (zone == null)
        {
            WrongPlacement();
            return;
        }

        if (IsValidDropZone(zone))
        {
            CorrectPlacement(zone);
        }
        else
        {
            WrongPlacement();
        }
    }

    // =====================================================
    // CHECK DROP ZONE
    // =====================================================

    private bool IsValidDropZone(Level7DropZone zone)
    {
        if (zone == null)
            return false;

        if (zone.partType != partType)
            return false;

        if (!zone.IsAvailable())
            return false;

        if (correctDropZones == null ||
            correctDropZones.Length == 0)
            return false;

        for (int i = 0; i < correctDropZones.Length; i++)
        {
            if (correctDropZones[i] == zone)
                return true;
        }

        return false;
    }

    // =====================================================
    // CORRECT PLACEMENT
    // =====================================================

    private void CorrectPlacement(Level7DropZone zone)
    {
        if (zone != null)
            zone.ResetColor();

        placed = true;

        StartCoroutine(CorrectPlacementRoutine(zone));
    }

    private IEnumerator CorrectPlacementRoutine(Level7DropZone zone)
    {
        // Turn green.
        if (image != null)
            image.color = correctColor;

        // Play correct sound.
        if (manager != null)
            manager.PlayCorrectSFX();

        yield return new WaitForSeconds(0.15f);

        // Mark this drop zone as occupied.
        zone.SetOccupied(true);

        // =====================================================
        // KEEP ORIGINAL PARENT
        // =====================================================

        // IMPORTANT:
        // The part stays under its original parent.
        // This prevents it from disappearing when a
        // case screen is disabled.
        transform.SetParent(originalParent, false);

        // =====================================================
        // RESET TRANSFORM
        // =====================================================

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.localRotation =
            Quaternion.identity;

        rectTransform.localScale =
            Vector3.one;

        // =====================================================
        // SNAP TO DROP ZONE
        // =====================================================

        rectTransform.position =
            zone.transform.position;

        // =====================================================
        // CHANGE SPRITE
        // =====================================================

        if (image != null && placedSprite != null)
        {
            image.sprite = placedSprite;

            // Prevent Image from changing the size
            // based on aspect ratio.
            image.preserveAspect = false;
        }

        // =====================================================
        // COPY DROP ZONE SIZE
        // =====================================================

        RectTransform zoneRect =
            zone.GetComponent<RectTransform>();

        if (zoneRect != null)
        {
            // Copy Width and Height.
            rectTransform.sizeDelta =
                zoneRect.sizeDelta;

            // Copy Scale.
            rectTransform.localScale =
                zoneRect.localScale;
        }

// =====================================================
// RETURN TO NORMAL COLOR
// =====================================================

if (image != null)
    image.color = normalColor;
    
        // =====================================================
        // LOCK PART
        // =====================================================

        canvasGroup.blocksRaycasts = false;

        if (image != null)
            image.raycastTarget = false;

        // =====================================================
        // NOTIFY MANAGER
        // =====================================================

        if (manager != null)
            manager.PartPlacedCorrectly(
                this,
                zone
            );
    }

    // =====================================================
    // WRONG PLACEMENT
    // =====================================================

    private void WrongPlacement()
    {
        if (manager != null)
            manager.PlayWrongSFX();

        StartCoroutine(WrongPlacementRoutine());
    }

    private IEnumerator WrongPlacementRoutine()
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
    // RETURN TO ORIGINAL
    // =====================================================

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent, false);

        rectTransform.anchoredPosition =
            originalPosition;

        rectTransform.localScale =
            originalScale;

        rectTransform.sizeDelta =
            originalSizeDelta;

        rectTransform.localRotation =
            Quaternion.identity;

        if (image != null)
        {
            image.sprite = originalSprite;
            image.color = normalColor;
            image.raycastTarget = true;
        }

        canvasGroup.blocksRaycasts = true;
    }

    // =====================================================
    // RESET PART
    // =====================================================

    public void ResetPart()
    {
        StopAllCoroutines();

        placed = false;

        transform.SetParent(originalParent, false);

        rectTransform.anchoredPosition =
            originalPosition;

        rectTransform.localScale =
            originalScale;

        rectTransform.sizeDelta =
            originalSizeDelta;

        rectTransform.localRotation =
            Quaternion.identity;

        if (image != null)
        {
            image.sprite = originalSprite;
            image.color = normalColor;
            image.raycastTarget = true;
        }

        canvasGroup.blocksRaycasts = true;
    }
}