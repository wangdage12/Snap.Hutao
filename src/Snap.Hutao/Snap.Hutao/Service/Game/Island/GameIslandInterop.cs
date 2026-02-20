// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Factory.Process;
using Snap.Hutao.Service.Game.Launching.Context;
using Snap.Hutao.Win32.Foundation;
using System.IO;
using System.IO.MemoryMappedFiles;
using Windows.Devices.Input;

namespace Snap.Hutao.Service.Game.Island;

internal sealed class GameIslandInterop : IGameIslandInterop
{
    private const string IslandEnvironmentName = "4F3E8543-40F7-4808-82DC-21E48A6037A7";
    private const string IslandLibraryName = "nvd3dump.dll";

    private static ReadOnlySpan<uint> EnvironmentReservedSignature
    {
        get
        {
            return
            [
                0x01560EC0, 0x15B73330, 0x0106A3C0, 0x0106A3B0, 0x0C835B70,
                0x0608D620, 0x00406330, 0x071A6EE0, 0x0E47E1B0, 0x0E4851E0,
                0x0FEAFC10, 0x069EA500, 0x09199950, 0x0A98F410, 0x01063C50,
                0x01063450, 0x0FA87490, 0x1084E9E0, 0x105C2C10,
            ];
        }
    }

    private readonly bool resume;

    private string? islandPath;

    public GameIslandInterop(bool resume)
    {
        this.resume = resume;
    }

    public ValueTask BeforeAsync(BeforeLaunchExecutionContext context)
    {
        if (resume)
        {
            return ValueTask.CompletedTask;
        }

        islandPath = Path.Combine(AppContext.BaseDirectory, IslandLibraryName);
        return ValueTask.CompletedTask;
    }

    public async ValueTask WaitForExitAsync(LaunchExecutionContext context, CancellationToken token = default)
    {
        MemoryMappedFile file;
        if (resume)
        {
            try
            {
                file = MemoryMappedFile.OpenExisting(IslandEnvironmentName);
            }
            catch (FileNotFoundException)
            {
                return;
            }
        }
        else
        {
            file = MemoryMappedFile.CreateOrOpen(IslandEnvironmentName, 1024);
        }

        using (file)
        {
            using (MemoryMappedViewAccessor accessor = file.CreateViewAccessor())
            {
                nint handle = accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
                InitializeIslandEnvironment(handle, context.LaunchOptions);

                if (!resume)
                {
                    ArgumentException.ThrowIfNullOrEmpty(islandPath);
                    if (!File.Exists(islandPath))
                    {
                        throw HutaoException.InvalidOperation($"未找到 {IslandLibraryName}，请将该文件放置在程序目录后重试。路径: {islandPath}");
                    }

                    if (context.Process is FullTrustProcess fullTrustProcess)
                    {
                        fullTrustProcess.LoadLibrary(FullTrustLoadLibraryRequest.Create("Island", islandPath));
                        fullTrustProcess.ResumeMainThread();
                    }
                    else
                    {
                        try
                        {
                            DllInjectionUtilities.InjectUsingRemoteThread(islandPath, context.Process.Id);
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.AddBreadcrumb(
                                $"Island DLL injection failed: {ex.Message}",
                                category: "island.injection",
                                level: Sentry.BreadcrumbLevel.Error);

                            throw HutaoException.Throw($"Island DLL 注入失败: {ex.Message}", ex);
                        }
                    }
                }

                await PeriodicUpdateIslandEnvironmentAsync(context, handle, token).ConfigureAwait(false);
            }
        }
    }

    private static unsafe void InitializeIslandEnvironment(nint handle, LaunchOptions options)
    {
        IslandEnvironment* pIslandEnvironment = (IslandEnvironment*)handle;

        EnvironmentReservedSignature.CopyTo(new Span<uint>(pIslandEnvironment->Reserved, 19));

        UpdateIslandEnvironment(handle, options);
    }

    private static unsafe void UpdateIslandEnvironment(nint handle, LaunchOptions options)
    {
        IslandEnvironment* pIslandEnvironment = (IslandEnvironment*)handle;

        pIslandEnvironment->EnableSetFieldOfView = options.IsSetFieldOfViewEnabled.Value;
        pIslandEnvironment->FieldOfView = options.TargetFov.Value;
        pIslandEnvironment->FixLowFovScene = options.FixLowFovScene.Value;
        pIslandEnvironment->DisableFog = options.DisableFog.Value;
        pIslandEnvironment->EnableSetTargetFrameRate = options.IsSetTargetFrameRateEnabled.Value;
        pIslandEnvironment->TargetFrameRate = options.TargetFps.Value;
        pIslandEnvironment->RemoveOpenTeamProgress = options.RemoveOpenTeamProgress.Value;
        pIslandEnvironment->HideQuestBanner = options.HideQuestBanner.Value;
        pIslandEnvironment->DisableEventCameraMove = options.DisableEventCameraMove.Value;
        pIslandEnvironment->DisableShowDamageText = options.DisableShowDamageText.Value;
        pIslandEnvironment->RedirectCombineEntry = options.RedirectCombineEntry.Value;
        pIslandEnvironment->ResinListItemId000106Allowed = options.ResinListItemId000106Allowed.Value;
        pIslandEnvironment->ResinListItemId000201Allowed = options.ResinListItemId000201Allowed.Value;
        pIslandEnvironment->ResinListItemId107009Allowed = options.ResinListItemId107009Allowed.Value;
        pIslandEnvironment->ResinListItemId107012Allowed = options.ResinListItemId107012Allowed.Value;
        pIslandEnvironment->ResinListItemId220007Allowed = options.ResinListItemId220007Allowed.Value;

        pIslandEnvironment->HideUid = BOOL.FALSE;

        if (LocalSetting.Get(SettingKeys.LaunchForceUsingTouchScreen, false))
        {
            pIslandEnvironment->UsingTouchScreen = IsIntegratedTouchPresent();
        }
        else
        {
            pIslandEnvironment->UsingTouchScreen = options.UsingTouchScreen.Value;
        }
    }

    private static bool IsIntegratedTouchPresent()
    {
        IReadOnlyList<PointerDevice> devices = PointerDevice.GetPointerDevices();

        for (int i = 0; i < devices.Count; i++)
        {
            PointerDevice device = devices[i];
            if (device is { PointerDeviceType: PointerDeviceType.Touch, IsIntegrated: true })
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask PeriodicUpdateIslandEnvironmentAsync(LaunchExecutionContext context, nint handle, CancellationToken token)
    {
        using (PeriodicTimer timer = new(TimeSpan.FromMilliseconds(500)))
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (!context.Process.IsRunning)
                {
                    break;
                }

                UpdateIslandEnvironment(handle, context.LaunchOptions);
            }
        }
    }
}
