using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Level9Manager : MonoBehaviour
{
    // =====================================================
    // STATE IMAGES
    // =====================================================

    [Header("State Images")]

    public GameObject firstImage;

    public GameObject secondImage;

    public GameObject thirdImage;

    // =====================================================
    // BUTTON
    // =====================================================

    [Header("Toggle Button")]

    public Button toggleButton;

    [Header("Button Sprites")]

    public Sprite offSprite;

    public Sprite onSprite;

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Transition Settings")]

    [Tooltip("How long the second image stays visible.")]
    public float secondImageDuration = 3f;

    // =====================================================
    // INTERNAL VARIABLES
    // =====================================================

    private bool isOn = false;

    private Coroutine transitionRoutine;

    private Image buttonImage;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        // Get the Image component from the Button
        if (toggleButton != null)
        {
            buttonImage = toggleButton.GetComponent<Image>();
        }

        // Set original state
        SetOffState();

        // Connect button automatically
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(ToggleButton);
        }
    }

    // =====================================================
    // TOGGLE BUTTON
    // =====================================================

    private void ToggleButton()
    {
        // If currently OFF
        if (!isOn)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    // =====================================================
    // TURN ON
    // =====================================================

    private void TurnOn()
    {
        isOn = true;

        // Change button to ON sprite
        if (buttonImage != null && onSprite != null)
        {
            buttonImage.sprite = onSprite;
        }

        // Stop previous transition if one exists
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        // Start transition
        transitionRoutine = StartCoroutine(OnTransition());
    }

    // =====================================================
    // ON TRANSITION
    // =====================================================

    private IEnumerator OnTransition()
    {
        // Hide first image
        if (firstImage != null)
        {
            firstImage.SetActive(false);
        }

        // Show second image
        if (secondImage != null)
        {
            secondImage.SetActive(true);
        }

        // Wait for configured duration
        yield return new WaitForSeconds(secondImageDuration);

        // If button was turned OFF during the transition,
        // don't continue to the final state.
        if (!isOn)
        {
            yield break;
        }

        // Hide second image
        if (secondImage != null)
        {
            secondImage.SetActive(false);
        }

        // Show final image
        if (thirdImage != null)
        {
            thirdImage.SetActive(true);
        }

        transitionRoutine = null;
    }

    // =====================================================
    // TURN OFF
    // =====================================================

    private void TurnOff()
    {
        isOn = false;

        // Stop transition
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        // Restore original state
        SetOffState();
    }

    // =====================================================
    // OFF STATE
    // =====================================================

    private void SetOffState()
    {
        isOn = false;

        // Button OFF sprite
        if (buttonImage != null && offSprite != null)
        {
            buttonImage.sprite = offSprite;
        }

        // First image ON
        if (firstImage != null)
        {
            firstImage.SetActive(true);
        }

        // Second image OFF
        if (secondImage != null)
        {
            secondImage.SetActive(false);
        }

        // Third image OFF
        if (thirdImage != null)
        {
            thirdImage.SetActive(false);
        }
    }
}