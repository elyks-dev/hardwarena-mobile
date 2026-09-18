using UnityEngine;

public class LevelPopupManager : MonoBehaviour
{
    [Header("Level Popups (1-9)")]
    [SerializeField] private GameObject[] levelPopups;

    private void Start()
    {
        HideAllPopups();
    }

    // -------------------------------------------------
    // Hide every popup
    // -------------------------------------------------
    public void HideAllPopups()
    {
        foreach (GameObject popup in levelPopups)
        {
            if (popup != null)
                popup.SetActive(false);
        }
    }

    // -------------------------------------------------
    // Open popup by level number (1-9)
    // -------------------------------------------------
    public void OpenPopup(int levelNumber)
    {
        HideAllPopups();

        int index = levelNumber - 1;

        if (index >= 0 && index < levelPopups.Length)
        {
            levelPopups[index].SetActive(true);
        }
        else
        {
            Debug.LogWarning("Invalid level popup index.");
        }
    }

    // -------------------------------------------------
    // Close popup
    // -------------------------------------------------
    public void ClosePopup()
    {
        HideAllPopups();
    }
}