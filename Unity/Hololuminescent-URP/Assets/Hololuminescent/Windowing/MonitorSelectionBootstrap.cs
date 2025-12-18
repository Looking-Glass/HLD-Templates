using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Bootstrap the monitor selection system automatically.
/// Add this script to a GameObject in your scene, or use [RuntimeInitializeOnLoadMethod].
/// </summary>
public class MonitorSelectionBootstrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool portraitOnlyDefault = true;
    [SerializeField] private bool rememberSelection = true;
    [SerializeField] private bool skipIfSingleMonitor = false;

    [Header("Styling")]
    [SerializeField] private Color accentColor = new Color(0.2f, 0.4f, 0.8f, 1f);

    private static bool hasInitialized = false;

    private void Awake()
    {
        if (hasInitialized)
        {
            Destroy(gameObject);
            return;
        }

        hasInitialized = true;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        // Check if we should skip
        if (skipIfSingleMonitor)
        {
            var monitors = portraitOnlyDefault
                ? MonitorDetector.GetPortraitMonitors()
                : MonitorDetector.GetAllMonitors();

            if (monitors.Count <= 1)
            {
                if (monitors.Count == 1)
                {
                    MonitorDetector.MoveWindowToMonitor(monitors[0], true);
                }
                // No UI needed - just continue with the current scene
                Destroy(gameObject);
                return;
            }
        }

        // Create the UI builder
        var builder = gameObject.AddComponent<MonitorSelectionUIBuilder>();

        // Configure via reflection
        SetField(builder, "portraitOnlyDefault", portraitOnlyDefault);
        SetField(builder, "rememberSelection", rememberSelection);
        SetField(builder, "buttonColor", accentColor);
    }

    private void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        field?.SetValue(obj, value);
    }

    /// <summary>
    /// Optional: Auto-initialize without needing a GameObject in the scene.
    /// Uncomment the attribute below to enable.
    /// </summary>
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (hasInitialized) return;

        var bootstrapObj = new GameObject("MonitorSelectionBootstrap");
        bootstrapObj.AddComponent<MonitorSelectionBootstrap>();
    }
}
