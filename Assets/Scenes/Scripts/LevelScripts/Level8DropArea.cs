using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Level8DropArea :
    MonoBehaviour,
    IDropHandler
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Level 8 References")]
    public Level8Manager manager;

    // =====================================================
    // VISUAL
    // =====================================================

    [Header("Highlight")]
    public Image zoneImage;

    public Color normalColor = Color.white;
    public Color highlightColor = Color.blue;

    // =====================================================
    // STATE
    // =====================================================

    private bool occupied = false;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (zoneImage == null)
        {
            zoneImage =
                GetComponent<Image>();
        }

        ResetZone();
    }

    // =====================================================
    // DROP
    // =====================================================

    public void OnDrop(
        PointerEventData eventData)
    {
        if (occupied)
            return;

        if (eventData.pointerDrag == null)
            return;

        Level8DragItem item =
            eventData.pointerDrag
                .GetComponent<Level8DragItem>();

        if (item == null)
            return;

        // =================================================
        // CORRECT DROP
        // =================================================

        if (item.correctDropArea == this)
        {
            occupied = true;

            ResetColor();

            item.PlaceCorrectly();

            if (manager != null)
            {
                manager.ItemPlacedCorrectly();
            }

            return;
        }

        // =================================================
        // WRONG DROP
        // =================================================

        item.FlickerWrong();

        if (manager != null)
        {
            manager.WrongPlacement();
        }
    }

    // =====================================================
    // HIGHLIGHT
    // =====================================================

    public void Highlight()
    {
        if (occupied)
            return;

        if (zoneImage != null)
        {
            zoneImage.color =
                highlightColor;
        }
    }

    // =====================================================
    // RESET COLOR
    // =====================================================

    public void ResetColor()
    {
        if (zoneImage != null)
        {
            zoneImage.color =
                normalColor;
        }
    }

    // =====================================================
    // RESET ZONE
    // =====================================================

    public void ResetZone()
    {
        occupied = false;

        ResetColor();

        gameObject.SetActive(true);
    }
}