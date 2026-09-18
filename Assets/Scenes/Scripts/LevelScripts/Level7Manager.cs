using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

public class Level7Manager : MonoBehaviour
{
    // =====================================================
    // CASE SCREENS
    // =====================================================

    [Header("Case Screens")]
    public GameObject backCaseScreen;
    public GameObject frontCaseScreen;

    // =====================================================
    // LEVEL COMPLETION
    // =====================================================

    [Header("Level Completion")]
    public Button finishButton;
    public GameObject levelCompletionScreen;
    public Button exitLevelButton;

    // =====================================================
    // ASSEMBLY PARTS
    // =====================================================

    [Header("Assembly Parts (14)")]
    public Level7DragPart[] parts;

    // =====================================================
    // DROP ZONES
    // =====================================================

    [Header("All Drop Zones")]
    public Level7DropZone[] dropZones;

    // =====================================================
    // AUDIO
    // =====================================================

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Level 7 Audio Clips")]
    public AudioClip level7BGMusic;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    // =====================================================
    // FIREBASE
    // =====================================================

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // =====================================================
    // ASSEMBLY STEP
    // =====================================================

    [System.Serializable]
    public class AssemblyStep
    {
        public Level7DropZone.PartType partType;

        public int requiredAmount = 1;

        [HideInInspector]
        public int completedAmount = 0;
    }

    [Header("Assembly Sequence")]
    public List<AssemblyStep> assemblySteps =
        new List<AssemblyStep>();

    private bool levelFinished = false;

    // =====================================================
    // AUDIO EVENTS
    // =====================================================

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged +=
                ApplyVolumeSettings;
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioSettingsChanged -=
                ApplyVolumeSettings;
        }
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        levelFinished = false;

        if (backCaseScreen != null)
            backCaseScreen.SetActive(true);

        if (frontCaseScreen != null)
            frontCaseScreen.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(false);

        if (finishButton != null)
        {
            finishButton.gameObject.SetActive(false);

            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(
                FinishButtonPressed
            );
        }

        if (exitLevelButton != null)
        {
            exitLevelButton.onClick.RemoveAllListeners();
            exitLevelButton.onClick.AddListener(
                ExitLevel
            );
        }

        ResetAssembly();

        // Enable ONLY the drop zones for the first step.
        UpdateActiveDropZones();

        // AUDIO
        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMainMenuMusic();

        ApplyVolumeSettings();
        PlayLevelMusic();
    }

    // =====================================================
    // RESET ASSEMBLY
    // =====================================================

    private void ResetAssembly()
    {
        foreach (AssemblyStep step in assemblySteps)
        {
            if (step != null)
                step.completedAmount = 0;
        }

        ResetDropZones();
        ResetParts();
    }

    // =====================================================
    // RESET DROP ZONES
    // =====================================================

    private void ResetDropZones()
    {
        if (dropZones == null)
            return;

        foreach (Level7DropZone zone in dropZones)
        {
            if (zone == null)
                continue;

            zone.SetOccupied(false);
            zone.ResetColor();

            // Start with all zones disabled.
            zone.gameObject.SetActive(false);
        }
    }

    // =====================================================
    // RESET PARTS
    // =====================================================

    private void ResetParts()
    {
        if (parts == null)
            return;

        foreach (Level7DragPart part in parts)
        {
            if (part != null)
                part.ResetPart();
        }
    }

    // =====================================================
    // CAN DRAG PART
    // =====================================================

    public bool CanDragPart(Level7DragPart part)
    {
        if (levelFinished)
            return false;

        if (part == null)
            return false;

        return CanPlacePartType(part.partType);
    }

    // =====================================================
    // HIGHLIGHT DROP ZONES
    // =====================================================

    public void HighlightDropZones(
        Level7DropZone.PartType partType)
    {
        ResetDropZoneHighlights();

        if (levelFinished)
            return;

        if (dropZones == null)
            return;

        foreach (Level7DropZone zone in dropZones)
        {
            if (zone == null)
                continue;

            if (zone.partType != partType)
                continue;

            if (!zone.IsAvailable())
                continue;

            if (!CanPlacePartType(zone.partType))
                continue;

            zone.Highlight();
        }
    }

    // =====================================================
    // RESET HIGHLIGHTS
    // =====================================================

    public void ResetDropZoneHighlights()
    {
        if (dropZones == null)
            return;

        foreach (Level7DropZone zone in dropZones)
        {
            if (zone == null)
                continue;

            zone.ResetColor();
        }
    }

    // =====================================================
    // ENABLE ONLY CURRENT STEP DROP ZONES
    // =====================================================

    private void UpdateActiveDropZones()
    {
        if (dropZones == null)
            return;

        foreach (Level7DropZone zone in dropZones)
        {
            if (zone == null)
                continue;

            bool shouldBeActive =
                !zone.occupied &&
                CanPlacePartType(zone.partType);

            zone.gameObject.SetActive(shouldBeActive);

            zone.ResetColor();
        }
    }

    // =====================================================
    // PART PLACED CORRECTLY
    // =====================================================

    public void PartPlacedCorrectly(
        Level7DragPart part,
        Level7DropZone zone)
    {
    if (levelFinished)
        return;

    if (part == null || zone == null)
        return;

    if (part.partType != zone.partType)
        return;

    if (!CanPlacePartType(part.partType))
        return;

    AssemblyStep matchingStep =
        FindAssemblyStep(part.partType);

    if (matchingStep == null)
        return;

    // Count the successful placement.
    matchingStep.completedAmount++;

    // =================================================
    // PSU SPECIAL CASE
    // =================================================

    if (part.partType ==
        Level7DropZone.PartType.PSU)
    {
        if (backCaseScreen != null)
            backCaseScreen.SetActive(false);

        if (frontCaseScreen != null)
            frontCaseScreen.SetActive(true);
    }

    // =================================================
    // DISABLE USED DROP ZONE
    // =================================================

    zone.SetOccupied(true);
    zone.gameObject.SetActive(false);

    // =================================================
    // CURRENT STEP FINISHED
    // =================================================

    ResetDropZoneHighlights();
    UpdateActiveDropZones();

    if (AreAllAssemblyStepsComplete())
    {
        ShowFinishButton();
    }
    }

    private bool CanPlacePartType(
        Level7DropZone.PartType partType)
    {
        if (!HasRequiredPartsRemaining(partType))
            return false;

        if (!IsPartTypePlaced(
                Level7DropZone.PartType.PSU))
        {
            return partType ==
                Level7DropZone.PartType.PSU;
        }

        if (!IsPartTypePlaced(
                Level7DropZone.PartType.Motherboard))
        {
            return partType ==
                Level7DropZone.PartType.Motherboard;
        }

        if (partType == Level7DropZone.PartType.Cover)
        {
            return AreAllSSDsPlaced();
        }

        if (partType == Level7DropZone.PartType.CPUCooler)
        {
            return IsPartTypePlaced(
                Level7DropZone.PartType.CPU);
        }

        return true;
    }

    private bool IsPartTypePlaced(
        Level7DropZone.PartType partType)
    {
        int requiredAmount = 0;
        int completedAmount = 0;

        foreach (AssemblyStep step in assemblySteps)
        {
            if (step == null ||
                step.partType != partType)
                continue;

            requiredAmount += step.requiredAmount;
            completedAmount += step.completedAmount;
        }

        return requiredAmount > 0 &&
               completedAmount >= requiredAmount;
    }

    private bool HasRequiredPartsRemaining(
        Level7DropZone.PartType partType)
    {
        foreach (AssemblyStep step in assemblySteps)
        {
            if (step != null &&
                step.partType == partType &&
                step.completedAmount < step.requiredAmount)
            {
                return true;
            }
        }

        return false;
    }

    private bool AreAllSSDsPlaced()
    {
        return IsPartTypePlaced(
            Level7DropZone.PartType.SSD
        );
    }

    private AssemblyStep FindAssemblyStep(
        Level7DropZone.PartType partType)
    {
        foreach (AssemblyStep step in assemblySteps)
        {
            if (step != null &&
                step.partType == partType &&
                step.completedAmount < step.requiredAmount)
            {
                return step;
            }
        }

        return null;
    }

    private bool AreAllAssemblyStepsComplete()
    {
        foreach (AssemblyStep step in assemblySteps)
        {
            if (step == null ||
                step.completedAmount < step.requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    // =====================================================
    // FINISH BUTTON
    // =====================================================

    private void ShowFinishButton()
    {
        if (finishButton != null)
            finishButton.gameObject.SetActive(true);
    }

    public void FinishButtonPressed()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        ResetDropZoneHighlights();

        // Disable every remaining drop zone.
        if (dropZones != null)
        {
            foreach (Level7DropZone zone in dropZones)
            {
                if (zone != null)
                    zone.gameObject.SetActive(false);
            }
        }

        if (finishButton != null)
            finishButton.gameObject.SetActive(false);

        if (levelCompletionScreen != null)
            levelCompletionScreen.SetActive(true);
    }

    // =====================================================
    // AUDIO
    // =====================================================

    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplyMusicVolume(bgmSource);
        AudioManager.Instance.ApplySFXVolume(sfxSource);
    }

    private void PlayLevelMusic()
    {
        if (bgmSource == null || level7BGMusic == null)
            return;

        if (bgmSource.clip == level7BGMusic &&
            bgmSource.isPlaying)
            return;

        bgmSource.clip = level7BGMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void StopLevelMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayCorrectSFX()
    {
        if (sfxSource == null || correctSFX == null)
            return;

        sfxSource.PlayOneShot(correctSFX);
    }

    public void PlayWrongSFX()
    {
        if (sfxSource == null || wrongSFX == null)
            return;

        sfxSource.PlayOneShot(wrongSFX);
    }

    // =====================================================
    // EXIT LEVEL
    // =====================================================

    public async void ExitLevel()
    {
        StopLevelMusic();

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMainMenuMusic();

        await SaveLevelCompletion();

        SceneManager.LoadScene("SelectLevelScene");
    }

    // =====================================================
    // FIREBASE SAVE + XP
    // =====================================================

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
                db.Collection("users")
                  .Document(user.UserId);

            DocumentReference levelRef =
                userRef.Collection("progress")
                       .Document("level7");

            DocumentSnapshot levelSnapshot =
                await levelRef.GetSnapshotAsync();

            // Do not award XP twice.
            if (levelSnapshot.Exists &&
                levelSnapshot.TryGetValue(
                    "completed",
                    out bool completed) &&
                completed)
            {
                Debug.Log("Level 7 already completed.");
                return;
            }

            await levelRef.SetAsync(
                new Dictionary<string, object>()
                {
                    {
                        "completed",
                        true
                    },
                    {
                        "completedAt",
                        FieldValue.ServerTimestamp
                    }
                },
                SetOptions.MergeAll
            );

            DocumentSnapshot userSnapshot =
                await userRef.GetSnapshotAsync();

            int currentXP = 0;

            if (userSnapshot.Exists &&
                userSnapshot.TryGetValue(
                    "xp",
                    out int xp))
            {
                currentXP = xp;
            }

            await userRef.UpdateAsync(
                new Dictionary<string, object>()
                {
                    {
                        "xp",
                        currentXP + 1000
                    }
                }
            );

            Debug.Log(
                "Level 7 progress saved successfully. " +
                "+1000 XP."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Level 7 progress save error: " +
                e.Message
            );
        }
    }
}