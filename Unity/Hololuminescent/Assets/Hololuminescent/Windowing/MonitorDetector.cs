using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Runtime monitor detection using Windows API.
/// Works in both Editor and standalone builds.
/// </summary>
public static class MonitorDetector
{
    #region Windows API Structures
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    #endregion

    #region Windows API Imports
    
    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private const int ENUM_CURRENT_SETTINGS = -1;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    #endregion

    #region Monitor Info Class
    
    [Serializable]
    public class MonitorInfo
    {
        public int Index;
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width;
        public int Height;
        public bool IsPrimary;
        public string DeviceName;
        public int Orientation; // 0=Landscape, 1=Portrait(90°), 2=Landscape(180°), 3=Portrait(270°)
        public int RefreshRate;
        public int BitsPerPixel;
        
        public bool IsPortrait => Orientation == 1 || Orientation == 3;
        public float AspectRatio => Height > 0 ? (float)Width / Height : 0;
        
        public string OrientationString
        {
            get
            {
                return Orientation switch
                {
                    0 => "Landscape",
                    1 => "Portrait (90°)",
                    2 => "Landscape (180°)",
                    3 => "Portrait (270°)",
                    _ => "Unknown"
                };
            }
        }

        public string DisplayName => $"Monitor {Index + 1}: {Width}x{Height} ({OrientationString}){(IsPrimary ? " [Primary]" : "")}";
    }

    #endregion

    #region Public API
    
    private static List<MonitorInfo> cachedMonitors;
    private static MonitorEnumDelegate enumCallback;

    /// <summary>
    /// Gets all connected monitors.
    /// </summary>
    public static List<MonitorInfo> GetAllMonitors(bool forceRefresh = false)
    {
        if (cachedMonitors != null && !forceRefresh)
            return cachedMonitors;

        cachedMonitors = new List<MonitorInfo>();
        
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        enumCallback = MonitorEnumCallback;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumCallback, IntPtr.Zero);
#else
        // Fallback for non-Windows platforms using Unity's display API
        for (int i = 0; i < Display.displays.Length; i++)
        {
            var display = Display.displays[i];
            cachedMonitors.Add(new MonitorInfo
            {
                Index = i,
                Width = display.systemWidth,
                Height = display.systemHeight,
                IsPrimary = i == 0,
                DeviceName = $"Display {i}",
                Orientation = display.systemWidth > display.systemHeight ? 0 : 1
            });
        }
#endif
        
        return cachedMonitors;
    }

    /// <summary>
    /// Gets only portrait-oriented monitors.
    /// </summary>
    public static List<MonitorInfo> GetPortraitMonitors(bool forceRefresh = false)
    {
        var all = GetAllMonitors(forceRefresh);
        var portrait = new List<MonitorInfo>();
        
        foreach (var monitor in all)
        {
            if (monitor.IsPortrait)
                portrait.Add(monitor);
        }
        
        return portrait;
    }

    /// <summary>
    /// Moves the application window to the specified monitor.
    /// </summary>
    public static void MoveWindowToMonitor(MonitorInfo monitor, bool fullscreen = true)
    {
        if (monitor == null)
        {
            Debug.LogError("MonitorDetector: Cannot move to null monitor");
            return;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        IntPtr hwnd = GetActiveWindow();
        
        if (fullscreen)
        {
            // Set to windowed first to allow repositioning
            Screen.fullScreenMode = FullScreenMode.Windowed;
            
            // Move window to monitor position
            SetWindowPos(hwnd, IntPtr.Zero, monitor.Left, monitor.Top, 
                monitor.Width, monitor.Height, SWP_NOZORDER | SWP_SHOWWINDOW);
            
            // Set resolution and go fullscreen
            Screen.SetResolution(monitor.Width, monitor.Height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            SetWindowPos(hwnd, IntPtr.Zero, monitor.Left + 50, monitor.Top + 50, 
                monitor.Width - 100, monitor.Height - 100, SWP_NOZORDER | SWP_SHOWWINDOW);
        }
#else
        // Fallback: Use Unity's built-in display activation
        if (monitor.Index < Display.displays.Length)
        {
            Display.displays[monitor.Index].Activate();
            if (fullscreen)
            {
                Screen.SetResolution(monitor.Width, monitor.Height, FullScreenMode.FullScreenWindow);
            }
        }
#endif
        
        Debug.Log($"MonitorDetector: Moved to {monitor.DisplayName}");
    }

    /// <summary>
    /// Clears the monitor cache to force a refresh on next query.
    /// </summary>
    public static void ClearCache()
    {
        cachedMonitors = null;
    }

    #endregion

    #region Private Methods
    
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        MONITORINFOEX mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(mi);

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            var info = new MonitorInfo
            {
                Index = cachedMonitors.Count,
                Left = mi.rcMonitor.Left,
                Top = mi.rcMonitor.Top,
                Right = mi.rcMonitor.Right,
                Bottom = mi.rcMonitor.Bottom,
                Width = mi.rcMonitor.Right - mi.rcMonitor.Left,
                Height = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                IsPrimary = (mi.dwFlags & 1) != 0,
                DeviceName = mi.szDevice
            };

            // Get display settings for orientation and refresh rate
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(dm);

            if (EnumDisplaySettings(mi.szDevice, ENUM_CURRENT_SETTINGS, ref dm))
            {
                info.Orientation = dm.dmDisplayOrientation;
                info.RefreshRate = dm.dmDisplayFrequency;
                info.BitsPerPixel = dm.dmBitsPerPel;
            }

            cachedMonitors.Add(info);
        }

        return true;
    }
#endif

    #endregion
}
