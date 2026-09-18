using UnityEngine;
using UnityEngine.UI;

public class Level7DropZone : MonoBehaviour
{
    // =====================================================
    // PART TYPE
    // =====================================================

    public enum PartType
    {
        PSU,
        Motherboard,
        SSD,
        Cover,
        CPU,
        GPU,
        RAM,
        Fan,
        CPUCooler
    }

    // =====================================================
    // DROP ZONE
    // =====================================================

    [Header("Drop Zone")]
    public PartType partType;

    [Header("Zone Image")]
    public Image zoneImage;

    [Header("Highlight")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.blue;

    [HideInInspector]
    public bool occupied = false;
    private int originalSiblingIndex;
    private bool isHighlighted = false;

    // =====================================================
    // START
    // =====================================================

    private void Awake()
    {
        if (zoneImage == null)
            zoneImage = GetComponent<Image>();

        if (transform.parent != null)
        {
            originalSiblingIndex =
                transform.GetSiblingIndex();
        }

        ResetColor();
    }

    // =====================================================
    // HIGHLIGHT
    // =====================================================

    public void Highlight()
    {
        if (occupied)
            return;
        if (!isHighlighted)
        {
            if (transform.parent != null)
                transform.SetAsLastSibling();

            isHighlighted = true;
        }

        if (zoneImage != null)
            zoneImage.color = highlightColor;
    }

    // =====================================================
    // RESET COLOR
    // =====================================================

    public void ResetColor()
    {
        if (zoneImage != null)
            zoneImage.color = normalColor;

        if (transform.parent != null)
        {
            transform.SetSiblingIndex(
                originalSiblingIndex
            );
        }

        isHighlighted = false;
    }

    // =====================================================
    // OCCUPIED
    // =====================================================

    public void SetOccupied(bool value)
    {
        occupied = value;

        if (occupied)
            ResetColor();
    }

    // =====================================================
    // AVAILABLE
    // =====================================================

    public bool IsAvailable()
    {
        return !occupied;
    }
}