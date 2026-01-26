using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;

/// <summary>
/// Automatically creates the monitor selection UI at runtime.
/// Attach this to a GameObject in your startup scene - it will create a Canvas and all UI elements.
/// </summary>
public class MonitorSelectionUIBuilder : MonoBehaviour
{
    [Header("Styling")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
    [SerializeField] private Color panelColor = new Color(0.15f, 0.15f, 0.18f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.5f, 0.9f, 1f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color secondaryTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Dialog Settings")]
    [SerializeField] private string dialogTitle = "Select Display";
    [SerializeField] private bool portraitOnlyDefault = true;
    [SerializeField] private bool rememberSelection = true;

    [Header("Layout")]
    [SerializeField] private float dialogWidth = 500f;
    [SerializeField] private float dialogHeight = 400f;
    [SerializeField] private float buttonHeight = 60f;
    [SerializeField] private float spacing = 10f;

    private Canvas canvas;
    private MonitorSelectionDialog dialog;

    private void Start()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        // Ensure EventSystem exists for UI input
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Try new Input System first (Unity 6 default), fall back to legacy
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystemObj);
        }

        // Create Canvas
        var canvasObj = new GameObject("MonitorSelectionCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Render on top

        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create fullscreen dimmer
        var dimmer = CreatePanel(canvasObj.transform, "Dimmer", backgroundColor);
        var dimmerRect = dimmer.GetComponent<RectTransform>();
        dimmerRect.anchorMin = Vector2.zero;
        dimmerRect.anchorMax = Vector2.one;
        dimmerRect.sizeDelta = Vector2.zero;

        // Create dialog panel
        var dialogPanel = CreatePanel(dimmer.transform, "DialogPanel", panelColor);
        var dialogRect = dialogPanel.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(dialogWidth, dialogHeight);
        dialogRect.anchoredPosition = Vector2.zero;

        // Add rounded corners effect (via child image with outline)
        var outline = dialogPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        outline.effectDistance = new Vector2(2, 2);

        // Create title
        var title = CreateText(dialogPanel.transform, "Title", dialogTitle, 28, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(-40, 40);

        // Create subtitle/instructions
        var subtitle = CreateText(dialogPanel.transform, "Subtitle",
            "Choose which display to use for the application", 16, FontStyles.Normal);
        subtitle.color = secondaryTextColor;
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 1);
        subtitleRect.anchorMax = new Vector2(1, 1);
        subtitleRect.pivot = new Vector2(0.5f, 1);
        subtitleRect.anchoredPosition = new Vector2(0, -60);
        subtitleRect.sizeDelta = new Vector2(-40, 30);

        // Create scroll view for monitor list
        var scrollView = CreateScrollView(dialogPanel.transform, "MonitorList");
        var scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20, 100);
        scrollRect.offsetMax = new Vector2(-20, -100);

        // Get the content transform from scroll view
        var content = scrollView.transform.Find("Viewport/Content");

        // Create portrait filter toggle
        var toggleContainer = CreatePanel(dialogPanel.transform, "ToggleContainer", Color.clear);
        var toggleRect = toggleContainer.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0, 0);
        toggleRect.anchorMax = new Vector2(1, 0);
        toggleRect.pivot = new Vector2(0.5f, 0);
        toggleRect.anchoredPosition = new Vector2(0, 55);
        toggleRect.sizeDelta = new Vector2(-40, 35);

        var toggle = CreateToggle(toggleContainer.transform, "PortraitToggle", "Portrait displays only", portraitOnlyDefault);

        // Create bottom buttons container
        var bottomContainer = CreatePanel(dialogPanel.transform, "BottomContainer", Color.clear);
        var bottomRect = bottomContainer.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.anchoredPosition = new Vector2(0, 15);
        bottomRect.sizeDelta = new Vector2(-40, 35);

        var refreshBtn = CreateButton(bottomContainer.transform, "RefreshButton", "↻ Refresh", new Color(0.3f, 0.3f, 0.35f, 1f));
        var refreshRect = refreshBtn.GetComponent<RectTransform>();
        refreshRect.anchorMin = new Vector2(0, 0.5f);
        refreshRect.anchorMax = new Vector2(0, 0.5f);
        refreshRect.pivot = new Vector2(0, 0.5f);
        refreshRect.anchoredPosition = Vector2.zero;
        refreshRect.sizeDelta = new Vector2(120, 35);

        // Create status text
        var status = CreateText(bottomContainer.transform, "Status", "", 14, FontStyles.Normal);
        status.color = secondaryTextColor;
        status.alignment = TextAlignmentOptions.Right;
        var statusRect = status.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.5f);
        statusRect.anchorMax = new Vector2(1, 0.5f);
        statusRect.pivot = new Vector2(1, 0.5f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = new Vector2(-130, 30);

        // Create button prefab (will be instantiated for each monitor)
        var buttonPrefab = CreateMonitorButtonPrefab();

        // Add MonitorSelectionDialog component
        dialog = canvasObj.AddComponent<MonitorSelectionDialog>();

        // Use reflection to set serialized fields (since they're private)
        // IMPORTANT: Set showOnStartup=false because Awake already ran before we set up references
        SetPrivateField(dialog, "showOnStartup", false);
        SetPrivateField(dialog, "dialogPanel", dialogPanel);
        SetPrivateField(dialog, "dimmerPanel", dimmer); // Reference to dimmer for proper hiding
        SetPrivateField(dialog, "monitorListContainer", content);
        SetPrivateField(dialog, "monitorButtonPrefab", buttonPrefab);
        SetPrivateField(dialog, "portraitFilterToggle", toggle.GetComponent<Toggle>());
        SetPrivateField(dialog, "refreshButton", refreshBtn.GetComponent<Button>());
        SetPrivateField(dialog, "titleText", title);
        SetPrivateField(dialog, "statusText", status);
        SetPrivateField(dialog, "portraitOnlyDefault", portraitOnlyDefault);
        SetPrivateField(dialog, "isPortraitOnly", portraitOnlyDefault); // Also set the runtime field
        SetPrivateField(dialog, "rememberLastSelection", rememberSelection);

        // Wire up events
        toggle.GetComponent<Toggle>().onValueChanged.AddListener(dialog.OnPortraitToggleChanged);
        refreshBtn.GetComponent<Button>().onClick.AddListener(dialog.RefreshMonitorList);

        // Don't destroy on load so it persists if scenes change
        DontDestroyOnLoad(canvasObj);

        // Now manually show the dialog (since Awake already ran with null references)
        dialog.ShowDialog();
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        var image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    private GameObject CreateButton(Transform parent, string name, string text, Color bgColor)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var image = btnObj.AddComponent<Image>();
        image.color = bgColor;

        var button = btnObj.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = bgColor * 0.8f;
        button.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btnObj;
    }

    private GameObject CreateToggle(Transform parent, string name, string label, bool defaultValue)
    {
        var toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent, false);

        var toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = Vector2.zero;
        toggleRect.anchorMax = Vector2.one;
        toggleRect.sizeDelta = Vector2.zero;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(24, 24);

        // Checkmark
        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkImage = checkObj.AddComponent<Image>();
        checkImage.color = buttonColor;
        var checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(16, 16);

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 16;
        labelTmp.color = textColor;
        labelTmp.alignment = TextAlignmentOptions.Left;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(34, 0);
        labelRect.offsetMax = Vector2.zero;

        // Toggle component
        var toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = defaultValue;

        return toggleObj;
    }

    private GameObject CreateScrollView(Transform parent, string name)
    {
        // Scroll View
        var scrollViewObj = new GameObject(name);
        scrollViewObj.transform.SetParent(parent, false);

        var scrollViewRect = scrollViewObj.AddComponent<RectTransform>();

        var scrollImage = scrollViewObj.AddComponent<Image>();
        scrollImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        var scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;

        // Viewport - must fill the scroll view and mask content
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollViewObj.transform, false);

        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1);

        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.white;

        var mask = viewport.AddComponent<RectMask2D>(); // RectMask2D is more performant than Mask

        scrollRect.viewport = viewportRect;

        // Content - anchored to top, stretches horizontally, grows vertically
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin = new Vector2(0, contentRect.offsetMin.y);
        contentRect.offsetMax = new Vector2(0, contentRect.offsetMax.y);
        contentRect.sizeDelta = new Vector2(0, 0);

        var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var verticalLayout = content.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = spacing;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        scrollRect.content = contentRect;

        return scrollViewObj;
    }

    private GameObject CreateMonitorButtonPrefab()
    {
        var prefab = new GameObject("MonitorButtonPrefab");
        prefab.SetActive(false); // Keep inactive as template

        var image = prefab.AddComponent<Image>();
        image.color = buttonColor;

        var button = prefab.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonColor * 0.8f;
        colors.selectedColor = buttonColor;
        button.colors = colors;

        var layout = prefab.AddComponent<LayoutElement>();
        layout.minHeight = buttonHeight;
        layout.preferredHeight = buttonHeight;

        // Text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Monitor";
        tmp.fontSize = 18;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        prefab.transform.SetParent(transform, false);

        return prefab;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Could not find field: {fieldName}");
        }
    }
}
