using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Level2HazardManager : MonoBehaviour
{
    [System.Serializable]
    public class HazardItem
    {
        [Header("Hazard Button")]
        public Button button;

        [HideInInspector]
        public bool identified = false;
    }

    // =====================================================
    // SCREENS
    // =====================================================

    [Header("Screens")]
    public GameObject introScreen;
    public GameObject hazardScreen;
    public GameObject cleanScreen;
    public GameObject failedPopup;

    [Header("Level Completion")]
    public GameObject levelCompletionScreen;

    [Tooltip("How long the clean screen stays before showing the completion screen.")]
    public float cleanScreenDuration = 3f;

    [Header("Scene Navigation")]
    public string selectLevelSceneName = "SelectLevelScene";

    // =====================================================
    // TIMER
    // =====================================================

    [Header("Timer")]
    [Tooltip("Adjust the level time here in seconds.")]
    public float levelTime = 60f;

    public TMP_Text timerTMP;
    public Slider timerSlider;

    // =====================================================
    // INCORRECT OBJECT PENALTY (NEW)
    // =====================================================

    [Header("Incorrect Object Penalty")]
    [Tooltip("Buttons that are NOT hazards.")]
    public Button[] incorrectButtons;

    [Tooltip("Seconds deducted for clicking a wrong object.")]
    public float incorrectTimePenalty = 5f;

    [Tooltip("How long the timer blinks red.")]
    public float timerBlinkDuration = 0.4f;

    private Color timerOriginalColor;
    private Coroutine timerBlinkRoutine;

    // =====================================================
    // HAZARD COUNTER
    // =====================================================

    [Header("Hazard Counter")]
    public TMP_Text hazardCounterTMP;

    // =====================================================
    // HAZARDS
    // =====================================================

    [Header("Hazard Buttons")]
    public HazardItem[] hazards;

    // =====================================================
    // INTERNAL VARIABLES
    // =====================================================

    private float currentTime;
    private int identifiedCount;
    private bool levelFinished;
    private Coroutine timerRoutine;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (timerTMP != null)
            timerOriginalColor = timerTMP.color;

        ResetLevel();
    }

    // =====================================================
    // RESET LEVEL
    // =====================================================

    private void ResetLevel()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (timerBlinkRoutine != null)
        {
            StopCoroutine(timerBlinkRoutine);
            timerBlinkRoutine = null;
        }

        currentTime = levelTime;
        identifiedCount = 0;
        levelFinished = false;

        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;

        if (introScreen != null)
            introScreen.SetActive(true);

        if (hazardScreen != null)
            hazardScreen.SetActive(false);

        if (cleanScreen != null)
            cleanScreen.SetActive(false);

        if (failedPopup != null)
            failedPopup.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        // Reset hazards
        if (hazards != null)
        {
            foreach (HazardItem hazard in hazards)
            {
                if (hazard == null)
                    continue;

                hazard.identified = false;

                if (hazard.button != null)
                {
                    hazard.button.gameObject.SetActive(true);
                    hazard.button.interactable = true;
                    hazard.button.onClick.RemoveAllListeners();

                    HazardItem currentHazard = hazard;
                    hazard.button.onClick.AddListener(() => IdentifyHazard(currentHazard));
                }
            }
        }

        // Reset incorrect buttons
        if (incorrectButtons != null)
        {
            foreach (Button btn in incorrectButtons)
            {
                if (btn == null)
                    continue;

                btn.interactable = true;
                btn.onClick.RemoveAllListeners();

                Button currentButton = btn;
                btn.onClick.AddListener(() => IncorrectObjectClicked(currentButton));
            }
        }

        UpdateTimerUI();
        UpdateHazardCounter();
    }

    // =====================================================
    // START HAZARD GAME
    // =====================================================

    public void StartHazardGame()
    {
        if (levelFinished)
            return;

        if (introScreen != null)
            introScreen.SetActive(false);

        if (hazardScreen != null)
            hazardScreen.SetActive(true);

        Level2AudioManager.Instance?.PlayLevelMusic();

        if (cleanScreen != null)
            cleanScreen.SetActive(false);

        if (failedPopup != null)
            failedPopup.SetActive(false);

        currentTime = levelTime;
        identifiedCount = 0;
        levelFinished = false;

        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;

        ResetHazards();

        UpdateTimerUI();
        UpdateHazardCounter();

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(TimerRoutine());
    }

    // =====================================================
    // TIMER
    // =====================================================

    private IEnumerator TimerRoutine()
    {
        while (currentTime > 0f && !levelFinished)
        {
            currentTime -= Time.deltaTime;

            if (currentTime < 0f)
                currentTime = 0f;

            UpdateTimerUI();

            yield return null;
        }

        if (!levelFinished && identifiedCount < hazards.Length)
            FailLevel();

        timerRoutine = null;
    }

    // =====================================================
    // TIMER UI
    // =====================================================

    private void UpdateTimerUI()
    {
        if (timerTMP != null)
        {
            int seconds = Mathf.CeilToInt(currentTime);
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            if (minutes > 0)
                timerTMP.text = string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
            else
                timerTMP.text = remainingSeconds.ToString();
        }

        if (timerSlider != null)
        {
            timerSlider.maxValue = levelTime;
            timerSlider.value = currentTime;
        }
    }
        // =====================================================
    // WRONG OBJECT CLICK (NEW)
    // =====================================================

    public void IncorrectObjectClicked(Button clickedButton)
    {
        if (levelFinished)
            return;

        // Play incorrect sound
        Level2AudioManager.Instance?.PlayIncorrectClick();

        // Deduct time
        currentTime -= incorrectTimePenalty;

        if (currentTime < 0f)
            currentTime = 0f;

        UpdateTimerUI();

        // Blink timer red
        if (timerBlinkRoutine != null)
            StopCoroutine(timerBlinkRoutine);

        timerBlinkRoutine = StartCoroutine(BlinkTimerRed());

        // Blink clicked wrong object red
        StartCoroutine(BlinkWrongObject(clickedButton));

        // Fail if timer reaches zero
        if (currentTime <= 0f)
            FailLevel();
    }

    private IEnumerator BlinkTimerRed()
    {
        if (timerTMP == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < timerBlinkDuration)
        {
            timerTMP.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            timerTMP.color = timerOriginalColor;
            yield return new WaitForSeconds(0.1f);

            elapsed += 0.2f;
        }

        timerTMP.color = timerOriginalColor;
        timerBlinkRoutine = null;
    }

    private IEnumerator BlinkWrongObject(Button button)
    {
        if (button == null)
            yield break;

        Image image = button.GetComponent<Image>();

        if (image == null)
            yield break;

        Color originalColor = image.color;

        float elapsed = 0f;

        while (elapsed < timerBlinkDuration)
        {
            image.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            image.color = originalColor;
            yield return new WaitForSeconds(0.1f);

            elapsed += 0.2f;
        }

        image.color = originalColor;
    }

    // =====================================================
    // IDENTIFY HAZARD
    // =====================================================

    public void IdentifyHazard(HazardItem hazard)
    {
        if (levelFinished)
            return;

        if (hazard == null)
            return;

        if (hazard.identified)
            return;

        hazard.identified = true;

        Level2AudioManager.Instance?.PlayHazardClick();

        identifiedCount++;

        if (hazard.button != null)
            hazard.button.gameObject.SetActive(false);

        UpdateHazardCounter();

        if (identifiedCount >= hazards.Length)
            SuccessLevel();
    }

    // =====================================================
    // HAZARD COUNTER
    // =====================================================

    private void UpdateHazardCounter()
    {
        if (hazardCounterTMP != null)
        {
            int totalHazards = hazards != null ? hazards.Length : 0;

            hazardCounterTMP.text =
                identifiedCount + "/" + totalHazards;
        }
    }

    // =====================================================
    // SUCCESS
    // =====================================================

    private void SuccessLevel()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        // Stop timer
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        // Hide hazard screen
        if (hazardScreen != null)
            hazardScreen.SetActive(false);

        // Show success screen
        if (cleanScreen != null)
            cleanScreen.SetActive(true);

        // Hide completion screen until delay finishes
        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        if (failedPopup != null)
            failedPopup.SetActive(false);

        UpdateHazardCounter();

        StartCoroutine(ShowCompletionScreenRoutine());
    }

    private IEnumerator ShowCompletionScreenRoutine()
    {
        yield return new WaitForSeconds(cleanScreenDuration);

        if (cleanScreen != null)
            cleanScreen.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(true);
    }

    // =====================================================
    // LEVEL COMPLETION
    // =====================================================

    public async void ContinueLevel()
    {
        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        Level2AudioManager.Instance?.ExitLevel();

        await SaveLevelCompletion();

        SceneManager.LoadScene(selectLevelSceneName);
    }

    private async Task SaveLevelCompletion()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user logged in.");
            return;
        }

        try
        {
            DocumentReference userRef =
                db.Collection("users").Document(user.UserId);

            DocumentReference levelRef =
                userRef.Collection("progress").Document("level2");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log("Level 2 already completed.");
                return;
            }

            Dictionary<string, object> levelData =
                new Dictionary<string, object>()
                {
                    { "completed", true },
                    { "completedAt", FieldValue.ServerTimestamp }
                };

            await levelRef.SetAsync(levelData, SetOptions.MergeAll);

            DocumentSnapshot userSnapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (userSnapshot.Exists &&
                userSnapshot.TryGetValue("xp", out int xp))
            {
                currentXP = xp;
            }

            await userRef.UpdateAsync(new Dictionary<string, object>()
            {
                { "xp", currentXP + 1000 }
            });

            Debug.Log("Level 2 progress saved successfully.");

            // Award badges
            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.LevelCompleted("level2");
                BadgeManager.Instance.CheckXPBadges(currentXP + 1000);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Level 2 progress save error: " + e.Message);
        }
    }
        // =====================================================
    // FAILURE
    // =====================================================

    private void FailLevel()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        // Stop timer
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        // Stop timer blink if it's running
        if (timerBlinkRoutine != null)
        {
            StopCoroutine(timerBlinkRoutine);
            timerBlinkRoutine = null;
        }

        // Make sure timer shows 0
        currentTime = 0f;
        UpdateTimerUI();

        // Restore timer color
        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;

        // Hide gameplay screen
        if (hazardScreen != null)
            hazardScreen.SetActive(false);

        // Show failed popup
        if (failedPopup != null)
            failedPopup.SetActive(true);

        // Disable remaining hazards
        if (hazards != null)
        {
            foreach (HazardItem hazard in hazards)
            {
                if (hazard != null && hazard.button != null)
                    hazard.button.interactable = false;
            }
        }

        // Disable incorrect buttons too
        if (incorrectButtons != null)
        {
            foreach (Button btn in incorrectButtons)
            {
                if (btn != null)
                    btn.interactable = false;
            }
        }
    }

    // =====================================================
    // TRY AGAIN
    // =====================================================

    public void TryAgain()
    {
        ResetLevel();
    }

    // =====================================================
    // RESET HAZARDS
    // =====================================================

    private void ResetHazards()
    {
        if (hazards != null)
        {
            foreach (HazardItem hazard in hazards)
            {
                if (hazard == null)
                    continue;

                hazard.identified = false;

                if (hazard.button != null)
                {
                    hazard.button.gameObject.SetActive(true);
                    hazard.button.interactable = true;
                }
            }
        }

        // Reset incorrect buttons
        if (incorrectButtons != null)
        {
            foreach (Button btn in incorrectButtons)
            {
                if (btn == null)
                    continue;

                btn.interactable = true;

                // Reset button color in case it was blinking red
                Image image = btn.GetComponent<Image>();
                if (image != null)
                    image.color = Color.white;
            }
        }

        // Reset timer color
        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;
    }
}