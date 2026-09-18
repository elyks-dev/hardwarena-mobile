using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UniversalAccessibilityManager : MonoBehaviour
{
    private AccessibilityHierarchy hierarchy;
    private readonly List<AccessibilityNode> nodes = new();

    private bool isDestroyed;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        BuildIfNeeded();
    }

    private void OnDestroy()
    {
        isDestroyed = true;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        ClearHierarchy();
    }

    // =========================================================
    // SCENE
    // =========================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isDestroyed)
            return;

        BuildIfNeeded();
    }

    // =========================================================
    // BUILD ONLY WHEN NEEDED
    // =========================================================

    private void BuildIfNeeded()
    {
#if UNITY_EDITOR

        BuildHierarchy();

#elif UNITY_ANDROID || UNITY_IOS

        if (!AssistiveSupport.isScreenReaderEnabled)
        {
            ClearHierarchy();
            return;
        }

        BuildHierarchy();

#endif
    }

    // =========================================================
    // BUILD HIERARCHY
    // =========================================================

    private void BuildHierarchy()
    {
        if (isDestroyed)
            return;

        ClearHierarchy();

        hierarchy = new AccessibilityHierarchy();

        // Only collect the 3 useful UI types.
        CreateInputFields();
        CreateButtons();
        CreateTexts();

        AssistiveSupport.activeHierarchy = hierarchy;

        NotifyScreenChanged();
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearHierarchy()
    {
        if (AssistiveSupport.activeHierarchy == hierarchy)
        {
            AssistiveSupport.activeHierarchy = null;
        }

        if (hierarchy != null)
        {
            hierarchy.Clear();
            hierarchy = null;
        }

        nodes.Clear();
    }

    // =========================================================
    // INPUT FIELDS
    // =========================================================

    private void CreateInputFields()
    {
        TMP_InputField[] fields =
            FindObjectsByType<TMP_InputField>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (TMP_InputField input in fields)
        {
            if (input == null)
                continue;

            if (!input.isActiveAndEnabled)
                continue;

            if (!input.interactable)
                continue;

            string label = GetInputLabel(input);

            AccessibilityNode node =
                hierarchy.AddNode(label);

            if (node == null)
                continue;

            node.role = AccessibilityRole.SearchField;
            node.value = input.text;

            TMP_InputField captured = input;

            node.frameGetter = () =>
            {
                if (captured == null ||
                    !captured.isActiveAndEnabled)
                {
                    return Rect.zero;
                }

                return GetScreenRect(
                    captured.GetComponent<RectTransform>()
                );
            };

            nodes.Add(node);
        }
    }

    // =========================================================
    // BUTTONS
    // =========================================================

    private void CreateButtons()
    {
        Button[] buttons =
            FindObjectsByType<Button>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (!button.isActiveAndEnabled)
                continue;

            if (!button.interactable)
                continue;

            string label = GetButtonLabel(button);

            if (string.IsNullOrWhiteSpace(label))
                continue;

            AccessibilityNode node =
                hierarchy.AddNode(label);

            if (node == null)
                continue;

            node.role = AccessibilityRole.Button;

            Button captured = button;

            node.frameGetter = () =>
            {
                if (captured == null ||
                    !captured.isActiveAndEnabled)
                {
                    return Rect.zero;
                }

                return GetScreenRect(
                    captured.GetComponent<RectTransform>()
                );
            };

            node.invoked += () =>
            {
                if (captured == null)
                    return false;

                if (!captured.isActiveAndEnabled)
                    return false;

                if (!captured.interactable)
                    return false;

                captured.onClick.Invoke();

                return true;
            };

            nodes.Add(node);
        }
    }

    // =========================================================
    // TEXT
    // =========================================================

    private void CreateTexts()
    {
        TMP_Text[] texts =
            FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            if (!text.isActiveAndEnabled)
                continue;

            if (string.IsNullOrWhiteSpace(text.text))
                continue;

            // Don't duplicate button text.
            if (text.GetComponentInParent<Button>() != null)
                continue;

            // Don't duplicate input-field text.
            if (text.GetComponentInParent<TMP_InputField>() != null)
                continue;

            string label = CleanText(text.text);

            if (string.IsNullOrWhiteSpace(label))
                continue;

            AccessibilityNode node =
                hierarchy.AddNode(label);

            if (node == null)
                continue;

            node.role = AccessibilityRole.StaticText;
            node.value = label;

            TMP_Text captured = text;

            node.frameGetter = () =>
            {
                if (captured == null ||
                    !captured.isActiveAndEnabled)
                {
                    return Rect.zero;
                }

                return GetScreenRect(
                    captured.rectTransform
                );
            };

            nodes.Add(node);
        }
    }

    // =========================================================
    // INPUT LABEL
    // =========================================================

    private string GetInputLabel(TMP_InputField input)
    {
        string name =
            CleanText(input.gameObject.name);

        string lower =
            name.ToLowerInvariant();

        if (lower.Contains("email"))
            return "Email";

        if (lower.Contains("password") ||
            lower.Contains("pass word"))
            return "Password";

        if (lower.Contains("username") ||
            lower.Contains("user name"))
            return "Username";

        if (lower.Contains("phone") ||
            lower.Contains("mobile"))
            return "Phone Number";

        if (input.placeholder != null)
        {
            TMP_Text placeholder =
                input.placeholder.GetComponent<TMP_Text>();

            if (placeholder != null &&
                !string.IsNullOrWhiteSpace(placeholder.text))
            {
                return CleanText(placeholder.text);
            }
        }

        return string.IsNullOrWhiteSpace(name)
            ? "Input Field"
            : name;
    }

    // =========================================================
    // BUTTON LABEL
    // =========================================================

    private string GetButtonLabel(Button button)
    {
        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>();

        if (text != null &&
            !string.IsNullOrWhiteSpace(text.text))
        {
            return CleanText(text.text);
        }

        return CleanText(button.gameObject.name);
    }

    // =========================================================
    // TEXT CLEANUP
    // =========================================================

    private string CleanText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string result =
            value.Trim()
                 .Replace("_", " ")
                 .Replace("-", " ");

        while (result.Contains("  "))
            result = result.Replace("  ", " ");

        string lower =
            result.ToLowerInvariant();

        if (lower.EndsWith(" btn"))
            result = result[..^4].Trim();

        if (lower.EndsWith(" button"))
            result = result[..^7].Trim();

        if (lower.EndsWith(" text area"))
            result = result[..^9].Trim();

        if (lower.EndsWith(" input field"))
            result = result[..^11].Trim();

        if (lower == "new text")
            return string.Empty;

        if (lower == "tmp text")
            return string.Empty;

        return result;
    }

    // =========================================================
    // SCREEN RECT
    // =========================================================

    private Rect GetScreenRect(RectTransform rect)
    {
        if (rect == null)
            return Rect.zero;

        Canvas canvas =
            rect.GetComponentInParent<Canvas>();

        Camera camera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera;
        }

        Vector3[] corners = new Vector3[4];

        rect.GetWorldCorners(corners);

        Vector2 bottomLeft =
            RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[0]
            );

        Vector2 topRight =
            RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[2]
            );

        return new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y
        );
    }

    // =========================================================
    // NOTIFY
    // =========================================================

    private void NotifyScreenChanged()
    {
        if (AssistiveSupport.notificationDispatcher != null)
        {
            AssistiveSupport.notificationDispatcher
                .SendScreenChanged(null);
        }
    }
}