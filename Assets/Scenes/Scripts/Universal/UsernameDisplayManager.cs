using TMPro;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class UsernameDisplayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text usernameText;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        LoadUsername();
    }

    // =========================================================
    // LOAD USERNAME
    // =========================================================

    void LoadUsername()
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogWarning("No user is currently signed in.");
            usernameText.text = "";
            return;
        }

        db.Collection("users")
            .Document(currentUser.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "Failed to load username: " +
                        task.Exception
                    );

                    usernameText.text = "User";
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists &&
                    snapshot.ContainsField("username"))
                {
                    string username =
                        snapshot.GetValue<string>("username");

                    usernameText.text = username;

                    Debug.Log(
                        "✅ Username loaded: " +
                        username
                    );
                }
                else
                {
                    usernameText.text = "User";

                    Debug.LogWarning(
                        "Username was not found in Firestore."
                    );
                }
            });
    }
}