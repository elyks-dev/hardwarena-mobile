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
        [Tooltip("Label for the inspector only, e.g. Headset.")]
        public string hazardName;

        [Header("Clickable Objects")]
        [Tooltip("Clicking ANY of these counts as finding this hazard (e.g. both headsets).")]
        public Button[] buttons;

        [Header("Shown When Found")]
        [Tooltip("All circle images are activated when found (e.g. both headset circles).")]
        public GameObject[] circles;

        [Tooltip("One info card per hazard.")]
        public GameObject infoCard;

        [HideInInspector]
        public bool identified = false;
    }

    // =====================================================
    // SCREENS
    // =====================================================

    [Header("Screens")]
    public GameObject failedPopup;

    [Header("Level Completion")]
    public GameObject levelCompletionScreen;

    [Header("Scene Navigation")]
    public string selectLevelSceneName = "SelectLevelScene";

    // =====================================================
    // TIMER
    // =====================================================

    [Header("Timer")]
    [Tooltip("Adjust the level time here in seconds.")]
    public float levelTime = 60f;

    public TMP_Text timerTMP;

    // =====================================================
    // INCORRECT OBJECT PENALTY
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
    // INFO CARDS PANEL
    // =====================================================

    [Header("Info Cards Panel")]
    [Tooltip("Parent object that holds all the info cards.")]
    public GameObject infoCardsObject;

    [Tooltip("Opens the info cards, then hides itself.")]
    public Button openButton;

    [Tooltip("Closes the info cards and shows the open button again.")]
    public Button closeButton;

    // =====================================================
    // FINISH BUTTON
    // =====================================================

    [Header("Finish Level")]
    [Tooltip("Appears once every hazard is found. Clicking it finishes the level.")]
    public Button finishLevelButton;

    // =====================================================
    // HAZARDS
    // =====================================================

    [Header("Hazards (Headset, Aircon, PowerCable, Chair, Socket)")]
    public HazardItem[] hazards;

    // =====================================================
    // INTERNAL VARIABLES
    // =====================================================

    private float currentTime;
    private int identifiedCount;
    private bool levelFinished;
    private bool allHazardsFound;
    private Coroutine timerRoutine;
    private Task saveTask;

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
        StopAllCoroutines();
        timerRoutine = null;
        timerBlinkRoutine = null;
        saveTask = null;

        currentTime = levelTime;
        identifiedCount = 0;
        levelFinished = false;
        allHazardsFound = false;

        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;

        if (failedPopup != null)
            failedPopup.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        // Finish button stays hidden until every hazard is found
        if (finishLevelButton != null)
        {
            finishLevelButton.gameObject.SetActive(false);
            finishLevelButton.onClick.RemoveAllListeners();
            finishLevelButton.onClick.AddListener(FinishLevel);
        }

        // Hook up hazard buttons
        if (hazards != null)
        {
            foreach (HazardItem hazard in hazards)
            {
                if (hazard == null || hazard.buttons == null)
                    continue;

                HazardItem currentHazard = hazard;

                foreach (Button button in hazard.buttons)
                {
                    if (button == null)
                        continue;

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => IdentifyHazard(currentHazard));
                }
            }
        }

        // Hook up incorrect buttons
        if (incorrectButtons != null)
        {
            foreach (Button btn in incorrectButtons)
            {
                if (btn == null)
                    continue;

                btn.onClick.RemoveAllListeners();

                Button currentButton = btn;
                btn.onClick.AddListener(() => IncorrectObjectClicked(currentButton));
            }
        }

        // Hook up open / close buttons
        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(OpenInfoCards);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseInfoCards);
        }

        ResetHazards();

        UpdateTimerUI();
        UpdateHazardCounter();

        // The game starts right away: music + timer.
        Level2AudioManager.Instance?.PlayLevelMusic();

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
    }

    // =====================================================
    // WRONG OBJECT CLICK
    // =====================================================

    public void IncorrectObjectClicked(Button clickedButton)
    {
        if (levelFinished || allHazardsFound)
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

        // Clicking either headset (or any button of this hazard) only counts once.
        if (hazard.identified)
            return;

        hazard.identified = true;

        Level2AudioManager.Instance?.PlayHazardClick();

        identifiedCount++;

        // Activate every circle for this hazard (both headsets, etc.)
        if (hazard.circles != null)
        {
            foreach (GameObject circle in hazard.circles)
            {
                if (circle != null)
                    circle.SetActive(true);
            }
        }

        // Activate the single info card
        if (hazard.infoCard != null)
            hazard.infoCard.SetActive(true);

        UpdateHazardCounter();

        if (identifiedCount >= hazards.Length)
            AllHazardsFound();
    }

    // =====================================================
    // INFO CARDS PANEL
    // =====================================================

    public void OpenInfoCards()
    {
        SetInfoCardsOpen(true);
    }

    public void CloseInfoCards()
    {
        SetInfoCardsOpen(false);
    }

    private void SetInfoCardsOpen(bool open)
    {
        if (infoCardsObject != null)
            infoCardsObject.SetActive(open);

        if (openButton != null)
            openButton.gameObject.SetActive(!open);

        if (closeButton != null)
            closeButton.gameObject.SetActive(open);
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

    // Last hazard found: stop the timer and show the finish button.
    // The level does NOT end yet, so the player can read the info cards.
    private void AllHazardsFound()
    {
        if (levelFinished || allHazardsFound)
            return;

        allHazardsFound = true;

        // Stop timer so the player can't run out of time while reading
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (finishLevelButton != null)
            finishLevelButton.gameObject.SetActive(true);
    }

    // Clicking the finish button actually completes the level.
    public void FinishLevel()
    {
        if (levelFinished || !allHazardsFound)
            return;

        levelFinished = true;

        if (failedPopup != null)
            failedPopup.SetActive(false);

        UpdateHazardCounter();

        if (finishLevelButton != null)
            finishLevelButton.gameObject.SetActive(false);

        // Show the completion screen.
        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(true);

        // Save progress + XP as soon as the level is finished.
        saveTask = SaveLevelCompletion();
    }

    // =====================================================
    // LEVEL COMPLETION
    // =====================================================

    public async void ContinueLevel()
    {
        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        Level2AudioManager.Instance?.ExitLevel();

        // Progress was already saved when the level was completed;
        // just make sure that save has finished before leaving.
        if (saveTask != null)
            await saveTask;

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

        // Show failed popup
        if (failedPopup != null)
            failedPopup.SetActive(true);

        // Disable hazard buttons
        if (hazards != null)
        {
            foreach (HazardItem hazard in hazards)
            {
                if (hazard == null || hazard.buttons == null)
                    continue;

                foreach (Button button in hazard.buttons)
                {
                    if (button != null)
                        button.interactable = false;
                }
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

                if (hazard.buttons != null)
                {
                    foreach (Button button in hazard.buttons)
                    {
                        if (button == null)
                            continue;

                        button.gameObject.SetActive(true);
                        button.interactable = true;
                    }
                }

                // Hide circles and info card until found
                if (hazard.circles != null)
                {
                    foreach (GameObject circle in hazard.circles)
                    {
                        if (circle != null)
                            circle.SetActive(false);
                    }
                }

                if (hazard.infoCard != null)
                    hazard.infoCard.SetActive(false);
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

        // Info cards start closed, with the open button showing
        SetInfoCardsOpen(false);

        // Reset timer color
        if (timerTMP != null)
            timerTMP.color = timerOriginalColor;
    }
}