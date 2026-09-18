using TMPro;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class BioManager : MonoBehaviour
{
    [Header("Bio")]
    [SerializeField] private TMP_InputField bioInput;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private bool isLoading = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Connect InputField events.
        bioInput.onSelect.AddListener(OnBioSelected);
        bioInput.onDeselect.AddListener(OnBioDeselected);

        LoadBio();
    }

    // =========================================================
    // LOAD BIO
    // =========================================================

    void LoadBio()
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
            bioInput.text = "";
            return;
        }

        isLoading = true;

        db.Collection("users")
            .Document(currentUser.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                isLoading = false;

                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "Failed to load bio: " +
                        task.Exception
                    );

                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists &&
                    snapshot.ContainsField("bio"))
                {
                    bioInput.text =
                        snapshot.GetValue<string>("bio");
                }
                else
                {
                    bioInput.text = "";
                }
            });
    }

    // =========================================================
    // BIO SELECTED
    // =========================================================

    void OnBioSelected(string value)
    {
        if (isLoading)
            return;

        Debug.Log("✏️ Bio editing started.");
    }

    // =========================================================
    // BIO DESELECTED
    // =========================================================

    void OnBioDeselected(string value)
    {
        if (isLoading)
            return;

        SaveBio();
    }

    // =========================================================
    // SAVE BIO
    // =========================================================

    void SaveBio()
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null)
            return;

        string bio =
            bioInput.text.Trim();

        db.Collection("users")
            .Document(currentUser.UserId)
            .UpdateAsync("bio", bio)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "Failed to save bio: " +
                        task.Exception
                    );

                    return;
                }

                Debug.Log("✅ Bio saved!");
            });
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    void OnDestroy()
    {
        if (bioInput != null)
        {
            bioInput.onSelect.RemoveListener(OnBioSelected);
            bioInput.onDeselect.RemoveListener(OnBioDeselected);
        }
    }
}