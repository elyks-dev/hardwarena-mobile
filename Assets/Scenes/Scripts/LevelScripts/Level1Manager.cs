using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;

public class Level1Manager : MonoBehaviour
{
    // =====================================================
    // CAREER DATA
    // =====================================================

    [System.Serializable]
    public class Career
    {
        [Header("Career")]
        public string careerName;

        [Header("Mascot")]
        public GameObject mascotObject;

        [Header("Slot Result")]
        public GameObject slotObject;

        [Header("Career Information")]
        public Sprite infoImage;

        [TextArea(4, 8)]
        public string description;

        [Header("Speech Bubble")]
        [TextArea]
        public string introMessage;

        [TextArea]
        public string wrongMessage;

        [TextArea]
        public string correctMessage;
    }

    // =====================================================
    // BUTTONS
    // =====================================================

    [Header("Buttons")]
    public Button pullButton;
    public Button doneButton;

    [Header("Level Completion Screen")]
    public GameObject levelCompletionScreen;
    public Button continueButton;

    [Tooltip("Delay before showing the completion screen.")]
    public float completionScreenDelay = 3f;

    // =====================================================
    // SLOT MACHINE
    // =====================================================

    [Header("Rolling Slot Animation")]
    public GameObject rollingSlot;
    public Animator rollingAnimator;

    [Header("Default Slot")]
    public GameObject defaultSlot;

    // =====================================================
    // SPEECH BUBBLE
    // =====================================================

    [Header("Speech Bubble")]
    public GameObject mascotMessage;
    public TMP_Text messageText;

    [Header("Mascot Default")]
    public GameObject mascotDefault;

    // =====================================================
    // UNIVERSAL INFO SCREEN
    // =====================================================

    [Header("Universal Info Screen")]
    public GameObject infoScreen;
    public Image infoImage;
    public TMP_Text careerNameText;
    public TMP_Text descriptionText;

    [Header("Timing")]
    [Tooltip("How long the mascot talks before the Info Screen opens.")]
    public float infoScreenDelay = 2.5f;

    // =====================================================
    // CAREERS
    // =====================================================

    [Header("Careers (Size = 4)")]
    public List<Career> careers = new List<Career>();

    // =====================================================
    // RESULT SEQUENCE
    // =====================================================

    [Header("Result Sequence")]
    [Tooltip("0=Artist, 1=Technician, 2=Telcom, 3=IT")]
    public List<int> resultSequence = new List<int>();

    // =====================================================
    // PRIVATE
    // =====================================================

    private int currentCareerIndex = 0;
    private int currentPullIndex = 0;

    private bool isRolling = false;
    private bool waitingForDone = false;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // START
    // =====================================================

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        pullButton.onClick.AddListener(PullButtonPressed);
        doneButton.onClick.AddListener(DoneButtonPressed);
        continueButton.onClick.AddListener(ContinueButtonPressed);

        ResetScene();
        ShowCurrentCareer();
    }

    // =====================================================
    // RESET LEVEL
    // =====================================================

    void ResetScene()
    {
        rollingSlot.SetActive(false);

        defaultSlot.SetActive(true);

        mascotMessage.SetActive(false);

        infoScreen.SetActive(false);
        levelCompletionScreen.SetActive(false);

        foreach (Career career in careers)
        {
            career.slotObject.SetActive(false);
            career.mascotObject.SetActive(false);
        }

        mascotDefault.SetActive(true);

        currentCareerIndex = 0;
        currentPullIndex = 0;

        isRolling = false;
        waitingForDone = false;
    }

    // =====================================================
    // SHOW CURRENT CAREER
    // =====================================================

    void ShowCurrentCareer()
    {
        mascotDefault.SetActive(false);

        foreach (Career career in careers)
        {
            career.mascotObject.SetActive(false);
        }

        careers[currentCareerIndex].mascotObject.SetActive(true);

        ShowMessage(careers[currentCareerIndex].introMessage);

        defaultSlot.SetActive(true);

        foreach (Career career in careers)
        {
            career.slotObject.SetActive(false);
        }
    }

    // =====================================================
    // SPEECH BUBBLE
    // =====================================================

    void ShowMessage(string message)
    {
        mascotMessage.SetActive(true);
        messageText.text = message;
    }

    void HideMessage()
    {
        mascotMessage.SetActive(false);
    }

    // =====================================================
    // PULL BUTTON
    // =====================================================

    void PullButtonPressed()
    {
        if (isRolling)
            return;

        if (waitingForDone)
            return;

        if (currentPullIndex >= resultSequence.Count)
            return;

        isRolling = true;

        pullButton.interactable = false;

        if (careers[currentCareerIndex].mascotObject != null)
        {
            careers[currentCareerIndex].mascotObject.SetActive(false);
        }

        HideMessage();

        defaultSlot.SetActive(false);

        foreach (Career career in careers)
        {
            career.slotObject.SetActive(false);
        }

        rollingSlot.SetActive(true);

        Level1AudioManager.Instance?.PlaySlotRolling();

        rollingAnimator.Play("SlotRoll", 0, 0f);
    }

    // =====================================================
    // CALLED BY ANIMATION EVENT
    // =====================================================

    public void SlotRollFinished()
    {
        Level1AudioManager.Instance?.StopSlotRolling();

        rollingSlot.SetActive(false);

        int rolledCareer = resultSequence[currentPullIndex];

        if (careers[currentCareerIndex].mascotObject != null)
        {
            careers[currentCareerIndex].mascotObject.SetActive(true);
        }

        careers[rolledCareer].slotObject.SetActive(true);

        currentPullIndex++;

        isRolling = false;

        // -------------------------------
        // WRONG RESULT
        // -------------------------------

        if (rolledCareer != currentCareerIndex)
        {
            ShowMessage(careers[currentCareerIndex].wrongMessage);

            pullButton.interactable = true;
            return;
        }

        // -------------------------------
        // CORRECT RESULT
        // -------------------------------

        ShowMessage(careers[currentCareerIndex].correctMessage);

        waitingForDone = true;
        pullButton.interactable = false;

        Invoke(nameof(OpenInfoScreen), infoScreenDelay);
    }

    // =====================================================
    // OPEN UNIVERSAL INFO SCREEN
    // =====================================================

    void OpenInfoScreen()
    {
        infoScreen.SetActive(true);

        Career currentCareer = careers[currentCareerIndex];

        infoImage.sprite = currentCareer.infoImage;
        careerNameText.text = currentCareer.careerName;
        descriptionText.text = currentCareer.description;
    }

    // =====================================================
    // DONE BUTTON
    // =====================================================

    void DoneButtonPressed()
    {
        infoScreen.SetActive(false);

        waitingForDone = false;

        careers[currentCareerIndex].slotObject.SetActive(false);

        currentCareerIndex++;

        if (currentCareerIndex >= careers.Count)
        {
            ShowMessage("Congratulations! You finished Level 1!");
            pullButton.interactable = false;
            Invoke(nameof(OpenLevelCompletionScreen), completionScreenDelay);
            return;
        }

        ShowCurrentCareer();

        pullButton.interactable = true;
    }

    // =====================================================
    // LEVEL COMPLETION SCREEN
    // =====================================================

    void OpenLevelCompletionScreen()
    {
        HideMessage();
        levelCompletionScreen.SetActive(true);
    }

    async void ContinueButtonPressed()
    {
        levelCompletionScreen.SetActive(false);

        Level1AudioManager.Instance?.ExitLevel();

        await SaveLevelCompletion();

        Debug.Log("Continue pressed.");
        SceneManager.LoadScene("SelectLevelScene");
    }

    async Task SaveLevelCompletion()
    {
        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No Firebase user logged in.");
            return;
        }

        try
        {
            DocumentReference userRef = db
                .Collection("users")
                .Document(user.UserId);

            DocumentReference levelRef = userRef
                .Collection("progress")
                .Document("level1");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue("completed", out bool completed) &&
                completed)
            {
                Debug.Log("Level 1 already completed.");
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

        Debug.Log("Level 1 progress saved successfully.");

        // =====================================================
        // BADGES
        // =====================================================

        if (BadgeManager.Instance != null)
        {
            // Next Please! → Level 1 completed
            BadgeManager.Instance.LevelCompleted("level1");

            // XP-based badges
            BadgeManager.Instance.CheckXPBadges(
                currentXP + 1000
            );
        }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Level 1 progress save error: " + e.Message);
        }
    }
}
