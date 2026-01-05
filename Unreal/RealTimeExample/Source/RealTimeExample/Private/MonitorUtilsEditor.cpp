// MonitorUtilsEditor.cpp
#include "MonitorUtilsEditor.h"
#include "MonitorUtils.h"

#if WITH_EDITOR
#include "Editor.h"
#include "Editor/UnrealEdEngine.h"
#include "UnrealEdGlobals.h"
#include "Framework/Application/SlateApplication.h"
#include "Engine/Engine.h"
#include "Engine/GameViewportClient.h"
#include "Widgets/SWindow.h"
#endif

#if WITH_EDITOR
TWeakPtr<SWindow> UMonitorUtilsEditor::PreviewWindow;
#endif

bool UMonitorUtilsEditor::CreatePreviewWindowOnMonitor(int32 MonitorIndex, bool bFullscreen)
{
#if WITH_EDITOR
    if (!GIsEditor || !GUnrealEd)
    {
        UE_LOG(LogTemp, Warning, TEXT("CreatePreviewWindowOnMonitor: Not in editor"));
        return false;
    }

    TArray<FDisplayMonitorInfo> Monitors = UMonitorUtils::GetAllMonitors();
    if (!Monitors.IsValidIndex(MonitorIndex))
    {
        UE_LOG(LogTemp, Warning, TEXT("CreatePreviewWindowOnMonitor: Invalid monitor index %d"), MonitorIndex);
        return false;
    }

    // If PIE is already running, just move the window
    if (GUnrealEd->PlayWorld && GEngine && GEngine->GameViewport)
    {
        TSharedPtr<SWindow> PIEWindow = GEngine->GameViewport->GetWindow();
        if (PIEWindow.IsValid())
        {
            PreviewWindow = PIEWindow;
            MovePreviewWindowToMonitor(MonitorIndex, bFullscreen);
            return true;
        }
    }

    // Store target for after PIE starts
    const FDisplayMonitorInfo TargetMonitor = Monitors[MonitorIndex];

    // Request PIE - use the simple method
    FRequestPlaySessionParams SessionParams;
    SessionParams.WorldType = EPlaySessionWorldType::PlayInEditor;

    GUnrealEd->RequestPlaySession(SessionParams);

    // Move window after PIE initializes
    FTimerHandle TimerHandle;
    GEditor->GetTimerManager()->SetTimer(
        TimerHandle,
        [MonitorIndex, bFullscreen]()
        {
            if (GEngine && GEngine->GameViewport)
            {
                TSharedPtr<SWindow> PIEWindow = GEngine->GameViewport->GetWindow();
                if (PIEWindow.IsValid())
                {
                    PreviewWindow = PIEWindow;
                    MovePreviewWindowToMonitor(MonitorIndex, bFullscreen);
                }
            }
        },
        0.5f,
        false
    );

    return true;

#else
    return false;
#endif
}

void UMonitorUtilsEditor::ClosePreviewWindow()
{
#if WITH_EDITOR
    if (GUnrealEd && GUnrealEd->PlayWorld)
    {
        GUnrealEd->RequestEndPlayMap();
    }
    PreviewWindow.Reset();
#endif
}

bool UMonitorUtilsEditor::IsPreviewWindowOpen()
{
#if WITH_EDITOR
    return PreviewWindow.IsValid() && GUnrealEd && GUnrealEd->PlayWorld != nullptr;
#else
    return false;
#endif
}

bool UMonitorUtilsEditor::MovePreviewWindowToMonitor(int32 MonitorIndex, bool bFullscreen)
{
#if WITH_EDITOR
    TArray<FDisplayMonitorInfo> Monitors = UMonitorUtils::GetAllMonitors();
    if (!Monitors.IsValidIndex(MonitorIndex))
    {
        UE_LOG(LogTemp, Warning, TEXT("MovePreviewWindowToMonitor: Invalid monitor index %d"), MonitorIndex);
        return false;
    }

    TSharedPtr<SWindow> Window = PreviewWindow.Pin();
    if (!Window.IsValid())
    {
        // Try to get current PIE window
        if (GEngine && GEngine->GameViewport)
        {
            Window = GEngine->GameViewport->GetWindow();
            if (Window.IsValid())
            {
                PreviewWindow = Window;
            }
        }
    }

    if (!Window.IsValid())
    {
        UE_LOG(LogTemp, Warning, TEXT("MovePreviewWindowToMonitor: No valid window"));
        return false;
    }

    const FDisplayMonitorInfo& TargetMonitor = Monitors[MonitorIndex];

    // Go windowed first
    Window->SetWindowMode(EWindowMode::Windowed);

    // Resize and move
    Window->Resize(FVector2D(TargetMonitor.Resolution.X, TargetMonitor.Resolution.Y));
    Window->MoveWindowTo(FVector2D(TargetMonitor.Position.X, TargetMonitor.Position.Y));

    if (bFullscreen)
    {
        FTimerHandle TimerHandle;
        GEditor->GetTimerManager()->SetTimer(
            TimerHandle,
            [Window]()
            {
                if (Window.IsValid())
                {
                    Window->SetWindowMode(EWindowMode::WindowedFullscreen);
                }
            },
            0.1f,
            false
        );
    }

    UE_LOG(LogTemp, Log, TEXT("Moved preview to monitor %d (%s)"), TargetMonitor.Index, *TargetMonitor.Name);
    return true;

#else
    return false;
#endif
}