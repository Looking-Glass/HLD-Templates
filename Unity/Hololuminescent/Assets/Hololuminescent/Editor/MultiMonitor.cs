using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using static MultiMonitorGamePreview;
using static System.Net.Mime.MediaTypeNames;

public class MultiMonitorGamePreview : EditorWindow
{
    private int targetMonitor = 0;
    private static EditorWindow gameViewWindow;
    private List<MonitorInfo> monitors = new List<MonitorInfo>();
    private List<MonitorInfo> filteredMonitors = new List<MonitorInfo>();
    private Vector2 scrollPosition;
    private bool showDetailedInfo = true;
    private bool filterPortraitOnly = true;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public class MonitorInfo
    {
        public int Index;
        public RECT Bounds;
        public RECT WorkArea;
        public bool IsPrimary;
        public string DeviceName;
        public string DeviceString;
        public string DeviceID;
        public int Width;
        public int Height;
        public int WorkWidth;
        public int WorkHeight;
        public int BitsPerPixel;
        public int RefreshRate;
        public int Orientation;
        public float AspectRatio;
        public float DiagonalInches;
        public int DPI;
        public bool IsAttached;
        public bool IsActive;
    }

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int LOGPIXELSX = 88;
    private const int LOGPIXELSY = 90;
    private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
    private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;
    private const uint DISPLAY_DEVICE_ACTIVE = 0x00000001;

    [MenuItem("Window/Multi-Monitor Game Preview")]
    public static void ShowWindow()
    {
        GetWindow<MultiMonitorGamePreview>("Monitor Preview");
    }

    private void OnEnable()
    {
        RefreshMonitors();
    }

    private void RefreshMonitors()
    {
        monitors.Clear();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        filteredMonitors.Clear();

        if (filterPortraitOnly)
        {
            // Portrait orientations are 1 (90°) and 3 (270°)
            foreach (var monitor in monitors)
            {
                if (monitor.Orientation == 1 || monitor.Orientation == 3)
                {
                    filteredMonitors.Add(monitor);
                }
            }
        }
        else
        {
            filteredMonitors.AddRange(monitors);
        }
    }

    private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        MONITORINFOEX mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(mi);

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            MonitorInfo info = new MonitorInfo
            {
                Index = monitors.Count,
                Bounds = mi.rcMonitor,
                WorkArea = mi.rcWork,
                IsPrimary = (mi.dwFlags & 1) != 0,
                DeviceName = mi.szDevice,
                Width = mi.rcMonitor.Right - mi.rcMonitor.Left,
                Height = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                WorkWidth = mi.rcWork.Right - mi.rcWork.Left,
                WorkHeight = mi.rcWork.Bottom - mi.rcWork.Top
            };

            // Get additional display device information
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);

            if (EnumDisplayDevices(mi.szDevice, 0, ref dd, 0))
            {
                info.DeviceString = dd.DeviceString;
                info.DeviceID = dd.DeviceID;
                info.IsAttached = (dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0;
                info.IsActive = (dd.StateFlags & DISPLAY_DEVICE_ACTIVE) != 0;
            }

            // Get display settings (resolution, refresh rate, etc.)
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(dm);

            if (EnumDisplaySettings(mi.szDevice, ENUM_CURRENT_SETTINGS, ref dm))
            {
                info.BitsPerPixel = dm.dmBitsPerPel;
                info.RefreshRate = dm.dmDisplayFrequency;
                info.Orientation = dm.dmDisplayOrientation;
            }

            // Calculate aspect ratio
            if (info.Height > 0)
            {
                info.AspectRatio = (float)info.Width / info.Height;
            }

            // Get DPI information
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                info.DPI = GetDeviceCaps(hdc, LOGPIXELSX);
                ReleaseDC(IntPtr.Zero, hdc);

                // Estimate diagonal size in inches (assuming 96 DPI is standard)
                float widthInches = info.Width / (float)info.DPI;
                float heightInches = info.Height / (float)info.DPI;
                info.DiagonalInches = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);
            }

            monitors.Add(info);
        }

        return true;
    }

    private void OnGUI()
    {
        GUILayout.Label("Multi-Monitor Game Preview", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Monitors"))
        {
            RefreshMonitors();
        }

        EditorGUI.BeginChangeCheck();
        filterPortraitOnly = GUILayout.Toggle(filterPortraitOnly, "Portrait Only", GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFilter();
            targetMonitor = 0; // Reset selection when filter changes
        }

        showDetailedInfo = GUILayout.Toggle(showDetailedInfo, "Show Details", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("System Information", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Total Monitors", monitors.Count.ToString());

        if (filterPortraitOnly)
        {
            int portraitCount = filteredMonitors.Count;
            EditorGUILayout.LabelField("Portrait Monitors", portraitCount.ToString());

            if (portraitCount == 0)
            {
                EditorGUILayout.HelpBox("No portrait-oriented monitors detected. Portrait orientations are 90° and 270°.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Filtered Monitors", filteredMonitors.Count.ToString());
        }

        EditorGUILayout.LabelField("Unity Version", UnityEngine.Application.unityVersion);
        EditorGUILayout.LabelField("Platform", UnityEngine.Application.platform.ToString());

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Monitor Details", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < filteredMonitors.Count; i++)
        {
            var mon = filteredMonitors[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header - show both filtered index and original index
            string header = $"Monitor {i}";
            if (filterPortraitOnly)
            {
                header += $" (System Index: {mon.Index})";
            }
            if (mon.IsPrimary) header += " (Primary)";
            if (!mon.IsActive) header += " (Inactive)";

            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            if (showDetailedInfo)
            {
                EditorGUI.indentLevel++;

                // Display Information
                EditorGUILayout.LabelField("Display Name", mon.DeviceString ?? "Unknown");
                EditorGUILayout.LabelField("Device Name", mon.DeviceName ?? "Unknown");

                // Resolution
                EditorGUILayout.LabelField("Resolution", $"{mon.Width} × {mon.Height}");
                EditorGUILayout.LabelField("Aspect Ratio", $"{mon.AspectRatio:F2}:1 ({GetAspectRatioName(mon.AspectRatio)})");

                // Position
                EditorGUILayout.LabelField("Position", $"({mon.Bounds.Left}, {mon.Bounds.Top})");
                EditorGUILayout.LabelField("Bounds", $"L:{mon.Bounds.Left} T:{mon.Bounds.Top} R:{mon.Bounds.Right} B:{mon.Bounds.Bottom}");

                // Work Area (excluding taskbar)
                EditorGUILayout.LabelField("Work Area", $"{mon.WorkWidth} × {mon.WorkHeight}");
                EditorGUILayout.LabelField("Work Bounds", $"L:{mon.WorkArea.Left} T:{mon.WorkArea.Top} R:{mon.WorkArea.Right} B:{mon.WorkArea.Bottom}");

                // Color and Refresh
                EditorGUILayout.LabelField("Color Depth", $"{mon.BitsPerPixel} bit");
                EditorGUILayout.LabelField("Refresh Rate", $"{mon.RefreshRate} Hz");

                // Orientation
                string orientation = mon.Orientation switch
                {
                    0 => "Landscape (0°)",
                    1 => "Portrait (90°)",
                    2 => "Landscape Flipped (180°)",
                    3 => "Portrait Flipped (270°)",
                    _ => "Unknown"
                };
                EditorGUILayout.LabelField("Orientation", orientation);

                // DPI and Physical Size
                EditorGUILayout.LabelField("DPI", mon.DPI.ToString());
                EditorGUILayout.LabelField("Diagonal (est.)", $"{mon.DiagonalInches:F1}\"");

                // Pixel Density
                float ppi = Mathf.Sqrt(mon.Width * mon.Width + mon.Height * mon.Height) / mon.DiagonalInches;
                EditorGUILayout.LabelField("PPI (est.)", $"{ppi:F0}");

                // Status
                EditorGUILayout.LabelField("Status", mon.IsAttached ? "Attached" : "Detached");
                EditorGUILayout.LabelField("Active", mon.IsActive ? "Yes" : "No");

                // Device ID
                if (!string.IsNullOrEmpty(mon.DeviceID))
                {
                    EditorGUILayout.LabelField("Device ID", mon.DeviceID);
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                // Compact view
                string orientationShort = mon.Orientation switch
                {
                    0 => "0°",
                    1 => "90°",
                    2 => "180°",
                    3 => "270°",
                    _ => "?"
                };
                EditorGUILayout.LabelField($"{mon.Width}×{mon.Height} @ {mon.RefreshRate}Hz, {mon.BitsPerPixel}bit, {mon.DPI}DPI, {orientationShort}");
                EditorGUILayout.LabelField($"Position: ({mon.Bounds.Left}, {mon.Bounds.Top}) | {mon.DeviceString}");
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // Control Section
        EditorGUILayout.LabelField("Game View Controls", EditorStyles.boldLabel);

        if (filteredMonitors.Count == 0)
        {
            EditorGUILayout.HelpBox("No monitors available with current filter settings.", MessageType.Warning);
            GUI.enabled = false;
        }

        targetMonitor = EditorGUILayout.IntSlider("Target Monitor", targetMonitor, 0, Mathf.Max(0, filteredMonitors.Count - 1));

        if (targetMonitor < filteredMonitors.Count)
        {
            var targetMon = filteredMonitors[targetMonitor];
            string orientationInfo = targetMon.Orientation switch
            {
                0 => "Landscape (0°)",
                1 => "Portrait (90°)",
                2 => "Landscape Flipped (180°)",
                3 => "Portrait Flipped (270°)",
                _ => "Unknown"
            };

            EditorGUILayout.HelpBox(
                $"Will open on: {targetMon.DeviceString}\n" +
                $"Resolution: {targetMon.Width}×{targetMon.Height}\n" +
                $"Position: ({targetMon.Bounds.Left}, {targetMon.Bounds.Top})\n" +
                $"Orientation: {orientationInfo}",
                MessageType.Info
            );
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Game View Fullscreen"))
        {
            OpenGameViewOnMonitor(targetMonitor);
        }

        if (GUILayout.Button("Close Fullscreen Game View"))
        {
            CloseGameView();
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;
    }

    private string GetAspectRatioName(float ratio)
    {
        if (Mathf.Abs(ratio - 16f / 9f) < 0.01f) return "16:9";
        if (Mathf.Abs(ratio - 16f / 10f) < 0.01f) return "16:10";
        if (Mathf.Abs(ratio - 21f / 9f) < 0.01f) return "21:9";
        if (Mathf.Abs(ratio - 32f / 9f) < 0.01f) return "32:9";
        if (Mathf.Abs(ratio - 4f / 3f) < 0.01f) return "4:3";
        if (Mathf.Abs(ratio - 5f / 4f) < 0.01f) return "5:4";
        if (Mathf.Abs(ratio - 3f / 2f) < 0.01f) return "3:2";
        return "Custom";
    }

    private void OpenGameViewOnMonitor(int monitorIndex)
    {
        if (monitorIndex >= filteredMonitors.Count)
        {
            UnityEngine.Debug.LogError($"Monitor {monitorIndex} not available in filtered list. Only {filteredMonitors.Count} monitors available.");
            return;
        }

        // Get or create Game view
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        gameViewWindow = EditorWindow.GetWindow(gameViewType, false, "Game", false);

        var monitor = filteredMonitors[monitorIndex];

        // Calculate position for target monitor
        Rect monitorRect = new Rect(
            monitor.Bounds.Left,
            monitor.Bounds.Top,
            monitor.Bounds.Right - monitor.Bounds.Left,
            monitor.Bounds.Bottom - monitor.Bounds.Top
        );

        // Use reflection to call ShowPopupWithMode for true fullscreen
        MethodInfo showPopupWithMode = gameViewType.GetMethod("ShowPopupWithMode",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (showPopupWithMode != null)
        {
            // ShowPopupWithMode(int mode, bool giveFocus) - mode 1 is borderless fullscreen
            showPopupWithMode.Invoke(gameViewWindow, new object[] { 1, true });
        }

        // Set position after showing as popup
        gameViewWindow.position = monitorRect;
        gameViewWindow.Focus();

        string orientationInfo = monitor.Orientation switch
        {
            0 => "Landscape 0°",
            1 => "Portrait 90°",
            2 => "Landscape 180°",
            3 => "Portrait 270°",
            _ => "Unknown"
        };

        UnityEngine.Debug.Log($"Game view opened fullscreen on monitor {monitorIndex} (System: {monitor.Index}) - {monitor.DeviceString} ({orientationInfo}) at {monitorRect}");
    }

    private void CloseGameView()
    {
        if (gameViewWindow != null)
        {
            gameViewWindow.Close();
            gameViewWindow = null;
        }
    }
}