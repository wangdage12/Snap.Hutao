// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Windowing;
using Snap.Hutao.Core;
using Snap.Hutao.Core.Property;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Model;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Service.Game.FileSystem;
using Snap.Hutao.Service.Game.PathAbstraction;
using Snap.Hutao.Win32;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Snap.Hutao.Service.Game;

[Service(ServiceLifetime.Singleton)]
internal sealed partial class LaunchOptions : DbStoreOptions, IRestrictedGamePathAccess
{
    private const int WindowsPrimaryMonitorValue = 0;

    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial LaunchOptions(IServiceProvider serviceProvider);

    [field: MaybeNull]
    public static IObservableProperty<bool> IsGameRunning { get => field ??= GameLifeCycle.IsGameRunningProperty; }

    [field: MaybeNull]
    public static IReadOnlyObservableProperty<bool> CanKillGameProcess { get => field ??= Property.Observe(IsGameRunning, value => HutaoRuntime.IsProcessElevated && value); }

    public AsyncReaderWriterLock GamePathLock { get; } = new();

    [field: MaybeNull]
    public IObservableProperty<GamePathEntry?> GamePathEntry { get => field ??= CreateProperty(SettingKeys.LaunchGamePath, string.Empty).AsNullableSelection(GamePathEntries.Value, static entry => entry?.Path ?? string.Empty, StringComparer.OrdinalIgnoreCase).Debug("GamePathEntry"); }

    [field: MaybeNull]
    public IObservableProperty<ImmutableArray<GamePathEntry>> GamePathEntries { get => field ??= CreatePropertyForStructUsingJson(SettingKeys.LaunchGamePathEntries, ImmutableArray<GamePathEntry>.Empty); }

    [field: MaybeNull]
    public IObservableProperty<bool> UsingHoyolabAccount { get => field ??= CreateProperty(SettingKeys.LaunchUsingHoyolabAccount, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> AreCommandLineArgumentsEnabled { get => field ??= CreateProperty(SettingKeys.LaunchAreCommandLineArgumentsEnabled, true).AlsoSetFalseWhenFalse(UsingHoyolabAccount); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsFullScreen { get => field ??= CreateProperty(SettingKeys.LaunchIsFullScreen, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsBorderless { get => field ??= CreateProperty(SettingKeys.LaunchIsBorderless, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsExclusive { get => field ??= CreateProperty(SettingKeys.LaunchIsExclusive, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenWidthEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsScreenWidthEnabled, true).WithValueChangedCallback(OnManualResolutionControlStateChanged, this); }

    [field: MaybeNull]
    public IObservableProperty<int> ScreenWidth { get => field ??= CreateProperty(SettingKeys.LaunchScreenWidth, DisplayArea.Primary.OuterBounds.Width); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenHeightEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsScreenHeightEnabled, true).WithValueChangedCallback(OnManualResolutionControlStateChanged, this); }

    [field: MaybeNull]
    public IObservableProperty<int> ScreenHeight { get => field ??= CreateProperty(SettingKeys.LaunchScreenHeight, DisplayArea.Primary.OuterBounds.Height); }

    [field: MaybeNull]
    public IObservableProperty<bool> UseCurrentMonitorResolution { get => field ??= CreateProperty(SettingKeys.LaunchUseCurrentMonitorResolution, false).WithValueChangedCallback(OnManualResolutionControlStateChanged, this); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsAspectRatioSelectionEnabled { get => field ??= Property.CreateObservable(!UseCurrentMonitorResolution.Value); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenWidthInputEnabled { get => field ??= Property.CreateObservable(!UseCurrentMonitorResolution.Value && IsScreenWidthEnabled.Value); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenWidthToggleEnabled { get => field ??= Property.CreateObservable(!UseCurrentMonitorResolution.Value); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenHeightInputEnabled { get => field ??= Property.CreateObservable(!UseCurrentMonitorResolution.Value && IsScreenHeightEnabled.Value); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsScreenHeightToggleEnabled { get => field ??= Property.CreateObservable(!UseCurrentMonitorResolution.Value); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsMonitorEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsMonitorEnabled, true); }

    public ImmutableArray<NameValue<int>> Monitors { get; } = InitializeMonitors();

    [field: MaybeNull]
    public IObservableProperty<NameValue<int>?> Monitor { get => field ??= CreateProperty(SettingKeys.LaunchMonitor, 1).AsNameValue(Monitors); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsPlatformTypeEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsPlatformTypeEnabled, false); }

    public ImmutableArray<NameValue<PlatformType>> PlatformTypes { get; } = ImmutableCollectionsNameValue.FromEnum<PlatformType>();

    [field: MaybeNull]
    public IObservableProperty<PlatformType> PlatformType { get => field ??= CreateProperty(SettingKeys.LaunchPlatformType, Model.Intrinsic.PlatformType.PC); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsWindowsHDREnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsWindowsHDREnabled, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> UsingStarwardPlayTimeStatistics { get => field ??= CreateProperty(SettingKeys.LaunchUsingStarwardPlayTimeStatistics, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> UsingBetterGenshinImpactAutomation { get => field ??= CreateProperty(SettingKeys.LaunchUsingBetterGenshinImpactAutomation, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsIslandEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsIslandEnabled, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsSetFieldOfViewEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsSetFieldOfViewEnabled, true); }

    [field: MaybeNull]
    public IObservableProperty<float> TargetFov { get => field ??= CreateProperty(SettingKeys.LaunchTargetFov, 45f); }

    [field: MaybeNull]
    public IObservableProperty<bool> FixLowFovScene { get => field ??= CreateProperty(SettingKeys.LaunchFixLowFovScene, true); }

    [field: MaybeNull]
    public IObservableProperty<bool> DisableFog { get => field ??= CreateProperty(SettingKeys.LaunchDisableFogRendering, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsSetTargetFrameRateEnabled { get => field ??= CreateProperty(SettingKeys.LaunchIsSetTargetFrameRateEnabled, true); }

    [field: MaybeNull]
    public IObservableProperty<int> TargetFps { get => field ??= CreateProperty(SettingKeys.LaunchTargetFps, InitializeTargetFpsWithScreenFps).WithValueChangedCallback(OnTargetFpsChanged); }

    [field: MaybeNull]
    public IObservableProperty<bool> RemoveOpenTeamProgress { get => field ??= CreateProperty(SettingKeys.LaunchRemoveOpenTeamProgress, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> HideQuestBanner { get => field ??= CreateProperty(SettingKeys.LaunchHideQuestBanner, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> DisableEventCameraMove { get => field ??= CreateProperty(SettingKeys.LaunchDisableEventCameraMove, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> DisableShowDamageText { get => field ??= CreateProperty(SettingKeys.LaunchDisableShowDamageText, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> UsingTouchScreen { get => field ??= CreateProperty(SettingKeys.LaunchUsingTouchScreen, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> RedirectCombineEntry { get => field ??= CreateProperty(SettingKeys.LaunchRedirectCombineEntry, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> ResinListItemId000106Allowed { get => field ??= CreateProperty(SettingKeys.LaunchResinListItemId000106Allowed, true); }

    [field: MaybeNull]
    public IObservableProperty<bool> ResinListItemId000201Allowed { get => field ??= CreateProperty(SettingKeys.LaunchResinListItemId000201Allowed, true); }

    [field: MaybeNull]
    public IObservableProperty<bool> ResinListItemId107009Allowed { get => field ??= CreateProperty(SettingKeys.LaunchResinListItemId107009Allowed, true); }

    [field: MaybeNull]
    public IObservableProperty<bool> ResinListItemId107012Allowed { get => field ??= CreateProperty(SettingKeys.LaunchResinListItemId107012Allowed, true); }


    [field: MaybeNull]
    public IObservableProperty<bool> ResinListItemId220007Allowed { get => field ??= CreateProperty(SettingKeys.LaunchResinListItemId220007Allowed, true); }

    [field: MaybeNull]
    public IObservableProperty<ImmutableArray<AspectRatio>> AspectRatios { get => field ??= CreatePropertyForStructUsingJson(SettingKeys.LaunchAspectRatios, ImmutableArray<AspectRatio>.Empty); }

    public AspectRatio? SelectedAspectRatio
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                (ScreenWidth.Value, ScreenHeight.Value) = ((int)value.Width, (int)value.Height);
            }
        }
    }

    [field: MaybeNull]
    public IObservableProperty<bool> UsingOverlay { get => field ??= CreateProperty(SettingKeys.LaunchUsingOverlay, false); }

    public (int Width, int Height) GetLaunchScreenResolution()
    {
        if (!UseCurrentMonitorResolution.Value)
        {
            return (ScreenWidth.Value, ScreenHeight.Value);
        }

        DisplayArea displayArea = ResolveLaunchDisplayArea();
        return (displayArea.OuterBounds.Width, displayArea.OuterBounds.Height);
    }

    public int GetLaunchMonitorValue()
    {
        if (Monitor.Value?.Value is not WindowsPrimaryMonitorValue)
        {
            return Monitor.Value?.Value ?? 1;
        }

        try
        {
            IReadOnlyList<DisplayArea> displayAreas = DisplayArea.FindAll();
            ulong primaryDisplayId = DisplayArea.Primary.DisplayId.Value;
            for (int i = 0; i < displayAreas.Count; i++)
            {
                if (displayAreas[i].DisplayId.Value == primaryDisplayId)
                {
                    return i + 1;
                }
            }
        }
        catch
        {
        }

        return 1;
    }

    private static int InitializeTargetFpsWithScreenFps()
    {
        return HutaoNative.Instance.MakeDeviceCapabilities().GetPrimaryScreenVerticalRefreshRate();
    }

    private static void OnManualResolutionControlStateChanged(bool _, LaunchOptions options)
    {
        options.RefreshManualResolutionControlStates();
    }

    private void RefreshManualResolutionControlStates()
    {
        bool canEditManualResolution = !UseCurrentMonitorResolution.Value;
        IsAspectRatioSelectionEnabled.Value = canEditManualResolution;
        IsScreenWidthInputEnabled.Value = canEditManualResolution && IsScreenWidthEnabled.Value;
        IsScreenWidthToggleEnabled.Value = canEditManualResolution;
        IsScreenHeightInputEnabled.Value = canEditManualResolution && IsScreenHeightEnabled.Value;
        IsScreenHeightToggleEnabled.Value = canEditManualResolution;
    }

    private DisplayArea ResolveLaunchDisplayArea()
    {
        if (!IsMonitorEnabled.Value)
        {
            return DisplayArea.Primary;
        }

        try
        {
            IReadOnlyList<DisplayArea> displayAreas = DisplayArea.FindAll();
            int selectedIndex = Math.Max(GetLaunchMonitorValue() - 1, 0);
            if ((uint)selectedIndex < (uint)displayAreas.Count)
            {
                return displayAreas[selectedIndex];
            }
        }
        catch
        {
        }

        return DisplayArea.Primary;
    }

    private static void OnTargetFpsChanged(int newFps)
    {
        // 异步更新配置文件，避免阻塞UI线程
        Task.Run(async () =>
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "fps_config.ini");
                

                if (File.Exists(configPath))
                {
                    string[] lines = await File.ReadAllLinesAsync(configPath).ConfigureAwait(false);
                    bool needsUpdate = true;
                    

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("FPS="))
                        {
                            int configFps = int.Parse(line.Substring(4));
                            if (configFps == newFps)
                            {
                                needsUpdate = false;
                            }
                            break;
                        }
                    }
                    
                    // 更新配置文件
                    if (needsUpdate)
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].StartsWith("FPS="))
                            {
                                lines[i] = $"FPS={newFps}";
                                break;
                            }
                        }
                        

                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                await File.WriteAllLinesAsync(configPath, lines).ConfigureAwait(false);
                                SentrySdk.AddBreadcrumb(
                                    $"Updated fps_config.ini with new FPS: {newFps}",
                                    category: "fps.unlocker",
                                    level: Sentry.BreadcrumbLevel.Info);
                                break;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                if (i == 2)
                                {
                                    SentrySdk.AddBreadcrumb(
                                        $"无法写入配置文件 {configPath}，请检查权限",
                                        category: "fps.unlocker",
                                        level: Sentry.BreadcrumbLevel.Error);
                                    return;
                                }
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                if (i == 2)
                                {
                                    SentrySdk.AddBreadcrumb(
                                        $"无法写入配置文件 {configPath}，文件可能被占用",
                                        category: "fps.unlocker",
                                        level: Sentry.BreadcrumbLevel.Error);
                                    return;
                                }
                                await Task.Delay(500).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                SentrySdk.AddBreadcrumb(
                    $"Failed to update fps_config.ini: {ex.Message}",
                    category: "fps.unlocker",
                    level: Sentry.BreadcrumbLevel.Warning);
            }
        });
    }

    private static ImmutableArray<NameValue<int>> InitializeMonitors()
    {
        ImmutableArray<NameValue<int>>.Builder monitors = ImmutableArray.CreateBuilder<NameValue<int>>();
        monitors.Add(new(SH.ServiceGameLaunchMonitorWindowsPrimary, WindowsPrimaryMonitorValue));

        try
        {
            // This list can't use foreach
            // https://github.com/microsoft/CsWinRT/issues/747
            IReadOnlyList<DisplayArea> displayAreas = DisplayArea.FindAll();
            for (int i = 0; i < displayAreas.Count; i++)
            {
                DisplayArea displayArea = displayAreas[i];
                int index = i + 1;
                monitors.Add(new($"{displayArea.DisplayId.Value:X8}:{index}", index));
            }
        }
        catch
        {
            monitors.Clear();
        }

        return monitors.ToImmutable();
    }
}
