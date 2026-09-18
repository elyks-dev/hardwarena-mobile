using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class HomeProfileManager : MonoBehaviour
{
    [Header("Home Profile Image")]
    public Image homeProfileImage;

    [Header("Portrait Sprites")]
    public Sprite ottoSprite;
    public Sprite kiraSprite;

    private FirebaseFirestore db;
    private bool isDestroyed;

    // ==========================================
    // START
    // ==========================================

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        await LoadProfileImage();
    }

    // ==========================================
    // DESTROY CHECK
    // ==========================================

    private void OnDestroy()
    {
        isDestroyed = true;
    }

    // ==========================================
    // LOAD PROFILE IMAGE FROM FIRESTORE
    // ==========================================

    public async Task LoadProfileImage()
    {
        // Object may have been destroyed before this method starts.
        if (isDestroyed || this == null || homeProfileImage == null)
            return;

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        // Default to Otto if not logged in.
        if (user == null)
        {
            if (isDestroyed || this == null || homeProfileImage == null)
                return;

            homeProfileImage.sprite = ottoSprite;
            return;
        }

        DocumentSnapshot snapshot = await db
            .Collection("users")
            .Document(user.UserId)
            .GetSnapshotAsync();

        // IMPORTANT:
        // The scene/object may have been destroyed while Firestore
        // was loading. Do not touch the UI after destruction.
        if (isDestroyed || this == null || homeProfileImage == null)
            return;

        if (snapshot.Exists &&
            snapshot.TryGetValue("profileImage", out string character))
        {
            ApplyCharacter(character);
        }
        else
        {
            ApplyCharacter("otto");
        }
    }

    // ==========================================
    // APPLY SPRITE
    // ==========================================

    private void ApplyCharacter(string character)
    {
        if (isDestroyed || this == null || homeProfileImage == null)
            return;

        if (character == "kira")
            homeProfileImage.sprite = kiraSprite;
        else
            homeProfileImage.sprite = ottoSprite;
    }
}