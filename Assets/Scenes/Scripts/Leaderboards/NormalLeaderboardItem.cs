using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

public class NormalLeaderboardItem : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text rankTMP;
    public Image pfpImage;
    public TMP_Text usernameTMP;
    public TMP_Text xpTMP;

    [Header("Portrait Sprites")]
    public Sprite ottoSprite;
    public Sprite kiraSprite;

    public void Setup(LeaderboardPlayer player, int rank)
    {
        rankTMP.text = rank.ToString();
        usernameTMP.text = player.username;
        xpTMP.text = player.xp.ToString("N0") + " XP";

        pfpImage.sprite = player.profileImage == "kira"
            ? kiraSprite
            : ottoSprite;

        string currentUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        if (player.userId == currentUID)
        {
            usernameTMP.color = new Color32(255, 215, 0, 255); // Gold
            usernameTMP.text += " (You)";
        }
        else
        {
            usernameTMP.color = Color.white;
        }
    }
}