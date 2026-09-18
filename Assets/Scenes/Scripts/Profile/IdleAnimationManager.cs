using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class IdleAnimationManager : MonoBehaviour
{
    [Header("Idle Animation GameObjects")]
    public GameObject kiraIdle;
    public GameObject ottoIdle;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private async void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Hide both until profile loads
        if (kiraIdle != null) kiraIdle.SetActive(false);
        if (ottoIdle != null) ottoIdle.SetActive(false);

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.LogWarning("No user logged in.");
            return;
        }

        try
        {
            DocumentSnapshot snapshot = await db
                .Collection("users")
                .Document(user.UserId)
                .GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning("User document not found.");
                return;
            }

            if (snapshot.TryGetValue("profileImage", out string profileImage))
            {
                SwitchIdle(profileImage);
            }
            else
            {
                Debug.LogWarning("profileImage field not found.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load profile image: " + e.Message);
        }
    }

    private void SwitchIdle(string profileImage)
{
    if (string.IsNullOrEmpty(profileImage))
        profileImage = "Otto";

    // Ignore uppercase/lowercase differences.
    profileImage = profileImage.ToLower();

    if (kiraIdle != null)
        kiraIdle.SetActive(profileImage == "kira");

    if (ottoIdle != null)
        ottoIdle.SetActive(profileImage == "otto");
}
}