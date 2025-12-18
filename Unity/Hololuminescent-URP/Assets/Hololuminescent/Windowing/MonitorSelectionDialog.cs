using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

/// <summary>
/// Startup dialog for monitor selection.
/// Place this on a Canvas in your startup/splash scene.
/// </summary>
public class MonitorSelectionDialog : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool showOnStartup = true;
    [SerializeField] private bool portraitOnlyDefault = true;
    [SerializeField] private bool goFullscreen = true;

    [Header("Keyboard Shortcut")]
    [SerializeField] private Key reopenModifier = Key.LeftCtrl;
    [SerializeField] private Key reopenKey = Key.E;

    [Header("Auto-Select Settings")]
    [SerializeField] private bool autoSelectIfSingleMonitor = true;
    [SerializeField] private float autoSelectDelay = 0.5f;

    [Header("Remember Selection")]
    [SerializeField] private bool rememberLastSelection = true;
    [SerializeField] private string prefsKey = "SelectedMonitorIndex";

    [Header("Events")]
    public UnityEvent<MonitorDetector.MonitorInfo> OnMonitorSelected;
    public UnityEvent OnDialogShown;
    public UnityEvent OnDialogHidden;

    // UI References - assign in inspector or let the script create them
    [Header("UI References (Optional - auto-created if null)")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject dimmerPanel;
    [SerializeField] private Transform monitorListContainer;
    [SerializeField] private GameObject monitorButtonPrefab;
    [SerializeField] private UnityEngine.UI.Toggle portraitFilterToggle;
    [SerializeField] private UnityEngine.UI.Button refreshButton;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private List<MonitorDetector.MonitorInfo> currentMonitors;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private bool isPortraitOnly;
    private MonitorDetector.MonitorInfo selectedMonitor;
    private bool isDialogVisible;

    private void Awake()
    {
        isPortraitOnly = portraitOnlyDefault;

        if (showOnStartup)
        {
            // Check for remembered selection
            if (rememberLastSelection && PlayerPrefs.HasKey(prefsKey))
            {
                int savedIndex = PlayerPrefs.GetInt(prefsKey);
                var monitors = MonitorDetector.GetAllMonitors();

                if (savedIndex >= 0 && savedIndex < monitors.Count)
                {
                    // Auto-apply saved selection without showing dialog
                    ApplyMonitorSelectionSilent(monitors[savedIndex]);
                    return;
                }
            }

            ShowDialog();
        }
        else
        {
            HideDialog();
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check for Ctrl+E (or configured shortcut) to toggle dialog
        if (keyboard[reopenModifier].isPressed && keyboard[reopenKey].wasPressedThisFrame)
        {
            if (isDialogVisible)
            {
                HideDialog();
            }
            else
            {
                ShowDialog();
            }
        }

        // Also allow Escape to close
        if (isDialogVisible && keyboard[Key.Escape].wasPressedThisFrame)
        {
            HideDialog();
        }
    }

    /// <summary>
    /// Shows the monitor selection dialog.
    /// </summary>
    public void ShowDialog()
    {
        isDialogVisible = true;

        if (dimmerPanel != null)
            dimmerPanel.SetActive(true);

        if (dialogPanel != null)
            dialogPanel.SetActive(true);

        RefreshMonitorList();
        OnDialogShown?.Invoke();

        // Auto-select if only one monitor matches criteria
        if (autoSelectIfSingleMonitor && currentMonitors != null && currentMonitors.Count == 1)
        {
            if (statusText != null)
                statusText.text = $"Auto-selecting {currentMonitors[0].DisplayName}...";

            Invoke(nameof(AutoSelectSingleMonitor), autoSelectDelay);
        }
    }

    /// <summary>
    /// Hides the dialog without making a selection.
    /// </summary>
    public void HideDialog()
    {
        isDialogVisible = false;

        // Cancel any pending auto-select
        CancelInvoke(nameof(AutoSelectSingleMonitor));

        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        if (dimmerPanel != null)
            dimmerPanel.SetActive(false);

        OnDialogHidden?.Invoke();
    }

    /// <summary>
    /// Refreshes the list of available monitors.
    /// </summary>
    public void RefreshMonitorList()
    {
        // Clear existing buttons
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        spawnedButtons.Clear();

        // Get monitors based on filter
        MonitorDetector.ClearCache();
        currentMonitors = isPortraitOnly
            ? MonitorDetector.GetPortraitMonitors(true)
            : MonitorDetector.GetAllMonitors(true);

        // Update status
        if (statusText != null)
        {
            if (currentMonitors.Count == 0)
            {
                statusText.text = isPortraitOnly
                    ? "No portrait monitors detected. Try disabling the filter."
                    : "No monitors detected.";
            }
            else
            {
                statusText.text = $"Found {currentMonitors.Count} monitor(s) • Ctrl+E to reopen";
            }
        }

        // Create buttons for each monitor
        if (monitorListContainer != null && monitorButtonPrefab != null)
        {
            Debug.Log($"MonitorSelectionDialog: Creating {currentMonitors.Count} monitor buttons");

            foreach (var monitor in currentMonitors)
            {
                var btnObj = Instantiate(monitorButtonPrefab, monitorListContainer);
                btnObj.SetActive(true); // Prefab may be inactive - activate it
                spawnedButtons.Add(btnObj);

                // Configure the button
                var btnComponent = btnObj.GetComponent<UnityEngine.UI.Button>();
                var textComponent = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true); // include inactive

                if (textComponent != null)
                {
                    textComponent.text = monitor.DisplayName;
                }
                else
                {
                    Debug.LogWarning("MonitorSelectionDialog: Button prefab missing TextMeshProUGUI");
                }

                if (btnComponent != null)
                {
                    var capturedMonitor = monitor; // Capture for closure
                    btnComponent.onClick.AddListener(() => SelectMonitor(capturedMonitor));
                }
            }

            // Force layout rebuild so ScrollRect knows the content size
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(monitorListContainer.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogWarning($"MonitorSelectionDialog: Missing references - container: {monitorListContainer != null}, prefab: {monitorButtonPrefab != null}");
        }
    }

    /// <summary>
    /// Toggle portrait-only filter.
    /// </summary>
    public void SetPortraitFilter(bool portraitOnly)
    {
        isPortraitOnly = portraitOnly;
        RefreshMonitorList();
    }

    /// <summary>
    /// Called when a monitor button is clicked.
    /// </summary>
    public void SelectMonitor(MonitorDetector.MonitorInfo monitor)
    {
        selectedMonitor = monitor;

        if (rememberLastSelection)
        {
            PlayerPrefs.SetInt(prefsKey, monitor.Index);
            PlayerPrefs.Save();
        }

        ApplyMonitorSelection(monitor);
    }

    /// <summary>
    /// Select monitor by index (for external calls).
    /// </summary>
    public void SelectMonitorByIndex(int index)
    {
        var monitors = MonitorDetector.GetAllMonitors();
        if (index >= 0 && index < monitors.Count)
        {
            SelectMonitor(monitors[index]);
        }
    }

    /// <summary>
    /// Clears the remembered monitor selection.
    /// </summary>
    public void ClearRememberedSelection()
    {
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Returns true if the dialog is currently visible.
    /// </summary>
    public bool IsVisible => isDialogVisible;

    private void ApplyMonitorSelection(MonitorDetector.MonitorInfo monitor)
    {
        Debug.Log($"MonitorSelectionDialog: Selected {monitor.DisplayName}");

        // Move window to selected monitor
        MonitorDetector.MoveWindowToMonitor(monitor, goFullscreen);

        // Invoke event
        OnMonitorSelected?.Invoke(monitor);

        // Hide dialog (includes dimmer)
        HideDialog();
    }

    /// <summary>
    /// Apply monitor selection without showing/hiding dialog (for remembered selections)
    /// </summary>
    private void ApplyMonitorSelectionSilent(MonitorDetector.MonitorInfo monitor)
    {
        Debug.Log($"MonitorSelectionDialog: Auto-applying remembered selection: {monitor.DisplayName}");

        // Move window to selected monitor
        MonitorDetector.MoveWindowToMonitor(monitor, goFullscreen);

        // Invoke event
        OnMonitorSelected?.Invoke(monitor);

        // Make sure UI is hidden
        HideDialog();
    }

    private void AutoSelectSingleMonitor()
    {
        if (currentMonitors != null && currentMonitors.Count == 1)
        {
            SelectMonitor(currentMonitors[0]);
        }
    }

    // Toggle callback for UI
    public void OnPortraitToggleChanged(bool value)
    {
        SetPortraitFilter(value);
    }
}
