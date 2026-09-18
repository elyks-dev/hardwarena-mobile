using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " does not have a Button component."
            );

            return;
        }

        button.onClick.AddListener(
            PlayClickSound
        );
    }

    void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                PlayClickSound
            );
        }
    }
}