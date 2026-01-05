// MonitorUtilsEditor.h
#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "MonitorUtilsEditor.generated.h"

/**
 * Editor-only monitor utilities for previewing on external displays.
 * This class is only available in editor builds.
 */
UCLASS()
class UMonitorUtilsEditor : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:
    /**
     * Creates a standalone PIE window on the specified monitor (Editor only).
     * @param MonitorIndex - Target monitor index
     * @param bFullscreen - Whether the preview window should be borderless fullscreen
     * @return True if the PIE session was successfully requested
     */
    UFUNCTION(BlueprintCallable, Category = "Monitor|Editor", meta = (DevelopmentOnly))
    static bool CreatePreviewWindowOnMonitor(int32 MonitorIndex, bool bFullscreen = true);

    /**
     * Ends any active PIE session that was started for preview.
     */
    UFUNCTION(BlueprintCallable, Category = "Monitor|Editor", meta = (DevelopmentOnly))
    static void ClosePreviewWindow();

    /**
     * Checks if a preview PIE session is currently running.
     */
    UFUNCTION(BlueprintCallable, Category = "Monitor|Editor", meta = (DevelopmentOnly))
    static bool IsPreviewWindowOpen();

    /**
     * Moves an existing PIE window to a different monitor.
     * @param MonitorIndex - Target monitor index
     * @param bFullscreen - Whether to use borderless fullscreen
     * @return True if the window was moved successfully
     */
    UFUNCTION(BlueprintCallable, Category = "Monitor|Editor", meta = (DevelopmentOnly))
    static bool MovePreviewWindowToMonitor(int32 MonitorIndex, bool bFullscreen = true);

private:
    static TWeakPtr<SWindow> PreviewWindow;
};