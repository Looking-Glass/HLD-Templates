#include "MonitorUtils.h"
#include "Framework/Application/SlateApplication.h"
#include "Engine/Engine.h"
#include "Engine/GameViewportClient.h"
#include "Widgets/SWindow.h"
#include "GameFramework/GameUserSettings.h"

void UMonitorUtils::SetWindowMode(bool bFullscreen)
{
    UGameUserSettings* Settings = GEngine ? GEngine->GetGameUserSettings() : nullptr;
    if (!Settings)
    {
        return;
    }

    if (bFullscreen)
    {
        Settings->SetFullscreenMode(EWindowMode::WindowedFullscreen);
    }
    else
    {
        Settings->SetFullscreenMode(EWindowMode::Windowed);
    }

    Settings->ApplySettings(false);
}

bool UMonitorUtils::IsFullscreen()
{
    UGameUserSettings* Settings = GEngine ? GEngine->GetGameUserSettings() : nullptr;
    if (!Settings)
    {
        return false;
    }

    return Settings->GetFullscreenMode() != EWindowMode::Windowed;
}

void UMonitorUtils::MoveWindowToMonitorAndFullscreen(int32 MonitorIndex)
{
    // First, switch to windowed mode
    SetWindowMode(false);

    // Move to target monitor
    MoveWindowToMonitor(MonitorIndex, true);

    // Switch back to fullscreen after a short delay
    // (Needs a small delay for the window move to complete)
    if (GEngine && GEngine->GameViewport)
    {
        FTimerHandle TimerHandle;
        GEngine->GameViewport->GetWorld()->GetTimerManager().SetTimer(
            TimerHandle,
            []()
            {
                SetWindowMode(true);
            },
            0.1f,
            false
        );
    }
}

TArray<FDisplayMonitorInfo> UMonitorUtils::GetAllMonitors()
{
    TArray<FDisplayMonitorInfo> Monitors;
    FDisplayMetrics DisplayMetrics;
    FSlateApplication::Get().GetDisplayMetrics(DisplayMetrics);

    for (int32 i = 0; i < DisplayMetrics.MonitorInfo.Num(); i++)
    {
        const FMonitorInfo& Info = DisplayMetrics.MonitorInfo[i];
        FDisplayMonitorInfo MonitorData;

        MonitorData.Name = Info.Name.IsEmpty()
            ? FString::Printf(TEXT("Display %d"), i + 1)
            : Info.Name;
        MonitorData.Index = i;
        MonitorData.Position = FIntPoint(Info.DisplayRect.Left, Info.DisplayRect.Top);
        MonitorData.Resolution = FIntPoint(
            Info.DisplayRect.Right - Info.DisplayRect.Left,
            Info.DisplayRect.Bottom - Info.DisplayRect.Top
        );
        MonitorData.NativeResolution = FIntPoint(Info.NativeWidth, Info.NativeHeight);
        MonitorData.bIsPrimary = Info.bIsPrimary;
        MonitorData.DPI = Info.DPI;

        // Determine orientation by comparing native width vs height
        // If native W > H but current W < H, monitor is rotated to portrait
        // If native W > H and current W > H, monitor is landscape
        if (MonitorData.Resolution.X >= MonitorData.Resolution.Y)
        {
            MonitorData.Orientation = EMonitorOrientation::Landscape;
        }
        else
        {
            MonitorData.Orientation = EMonitorOrientation::Portrait;
        }

        Monitors.Add(MonitorData);
    }

    return Monitors;
}

void UMonitorUtils::MoveWindowToMonitor(int32 MonitorIndex, bool bCenterOnMonitor)
{
    TArray<FDisplayMonitorInfo> Monitors = GetAllMonitors();

    if (!Monitors.IsValidIndex(MonitorIndex))
    {
        return;
    }

    const FDisplayMonitorInfo& Target = Monitors[MonitorIndex];

    if (!GEngine || !GEngine->GameViewport)
    {
        return;
    }

    TSharedPtr<SWindow> Window = GEngine->GameViewport->GetWindow();
    if (!Window.IsValid())
    {
        return;
    }

    FVector2D WindowSize = Window->GetSizeInScreen();
    FVector2D NewPosition;

    if (bCenterOnMonitor)
    {
        NewPosition.X = Target.Position.X + (Target.Resolution.X - WindowSize.X) / 2.0;
        NewPosition.Y = Target.Position.Y + (Target.Resolution.Y - WindowSize.Y) / 2.0;
    }
    else
    {
        NewPosition.X = Target.Position.X;
        NewPosition.Y = Target.Position.Y;
    }

    Window->MoveWindowTo(NewPosition);
}

void UMonitorUtils::MoveWindowToPosition(int32 X, int32 Y)
{
    if (!GEngine || !GEngine->GameViewport)
    {
        return;
    }

    TSharedPtr<SWindow> Window = GEngine->GameViewport->GetWindow();
    if (Window.IsValid())
    {
        Window->MoveWindowTo(FVector2D(X, Y));
    }
}

int32 UMonitorUtils::GetCurrentMonitorIndex()
{
    if (!GEngine || !GEngine->GameViewport)
    {
        return 0;
    }

    TSharedPtr<SWindow> Window = GEngine->GameViewport->GetWindow();
    if (!Window.IsValid())
    {
        return 0;
    }

    FVector2D WindowPos = Window->GetPositionInScreen();
    TArray<FDisplayMonitorInfo> Monitors = GetAllMonitors();

    for (const FDisplayMonitorInfo& Monitor : Monitors)
    {
        if (WindowPos.X >= Monitor.Position.X &&
            WindowPos.X < Monitor.Position.X + Monitor.Resolution.X &&
            WindowPos.Y >= Monitor.Position.Y &&
            WindowPos.Y < Monitor.Position.Y + Monitor.Resolution.Y)
        {
            return Monitor.Index;
        }
    }

    return 0;
}