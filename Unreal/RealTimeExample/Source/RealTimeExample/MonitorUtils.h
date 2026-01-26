#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "MonitorUtils.generated.h"

UENUM(BlueprintType)
enum class EMonitorOrientation : uint8
{
    Landscape,
    Portrait
};

USTRUCT(BlueprintType)
struct FDisplayMonitorInfo
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    FString Name;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    int32 Index = 0;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    FIntPoint Position = FIntPoint::ZeroValue;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    FIntPoint Resolution = FIntPoint::ZeroValue;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    FIntPoint NativeResolution = FIntPoint::ZeroValue;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    bool bIsPrimary = false;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    EMonitorOrientation Orientation = EMonitorOrientation::Landscape;

    UPROPERTY(BlueprintReadOnly, Category = "Monitor")
    float DPI = 0.0f;
};

UCLASS()
class REALTIMEEXAMPLE_API UMonitorUtils : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static TArray<FDisplayMonitorInfo> GetAllMonitors();

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static void MoveWindowToMonitor(int32 MonitorIndex, bool bCenterOnMonitor = true);

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static void MoveWindowToPosition(int32 X, int32 Y);

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static void SetWindowMode(bool bFullscreen);

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static bool IsFullscreen();

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static void MoveWindowToMonitorAndFullscreen(int32 MonitorIndex);

    UFUNCTION(BlueprintCallable, Category = "Monitor")
    static int32 GetCurrentMonitorIndex();
};