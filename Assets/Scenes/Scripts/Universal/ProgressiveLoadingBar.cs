using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressiveLoadingBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private TMP_Text xpText; // Assign your XP TMP text here

    [Header("Main Screen")]
    [SerializeField] private GameObject mainScreenObject;

    [Header("Timing")]
    [SerializeField] private float startDelay = 5f;
    [SerializeField] private float duration = 3f;

    [Header("XP Settings")]
    [SerializeField] private int targetXP = 1000;

    private Coroutine progressCoroutine;

    // =========================================================
    // ENABLED
    // =========================================================

    private void OnEnable()
    {
        ResetProgress();

        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
        }

        progressCoroutine = StartCoroutine(StartProgress());
    }

    // =========================================================
    // DISABLED
    // =========================================================

    private void OnDisable()
    {
        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
            progressCoroutine = null;
        }

        ResetProgress();
    }

    // =========================================================
    // START PROGRESS
    // =========================================================

    private IEnumerator StartProgress()
    {
        if (mainScreenObject != null)
        {
            while (!mainScreenObject.activeInHierarchy)
            {
                yield return null;
            }
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (!isActiveAndEnabled)
        {
            yield break;
        }

        yield return StartCoroutine(AnimateProgress());

        progressCoroutine = null;
    }

    // =========================================================
    // ANIMATE PROGRESS
    // =========================================================

    private IEnumerator AnimateProgress()
    {
        float elapsed = 0f;

        SetSliderValues();

        while (elapsed < duration)
        {
            if (!isActiveAndEnabled)
            {
                yield break;
            }

            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            // Loading bar
            float sliderValue = Mathf.Lerp(0f, 100f, progress);
            if (progressSlider != null)
            {
                progressSlider.value = sliderValue;
            }

            UpdatePercentage(sliderValue);

            // XP counter
            int currentXP = Mathf.RoundToInt(Mathf.Lerp(0, targetXP, progress));
            UpdateXP(currentXP);

            yield return null;
        }

        // Finish exactly at 100%
        if (progressSlider != null)
        {
            progressSlider.value = 100f;
        }

        UpdatePercentage(100f);
        UpdateXP(targetXP);
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    private void SetSliderValues()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 100f;
            progressSlider.value = 0f;
        }

        UpdatePercentage(0f);
        UpdateXP(0);
    }

    // =========================================================
    // UPDATE PERCENTAGE
    // =========================================================

    private void UpdatePercentage(float value)
    {
        if (percentageText == null) return;

        int percentage = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        percentageText.text = percentage + "%";
    }

    // =========================================================
    // UPDATE XP
    // =========================================================

    private void UpdateXP(int value)
    {
        if (xpText == null) return;

        xpText.text = $"+{value} XP";
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetProgress()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 100f;
            progressSlider.value = 0f;
        }

        UpdatePercentage(0f);
        UpdateXP(0);
    }
}