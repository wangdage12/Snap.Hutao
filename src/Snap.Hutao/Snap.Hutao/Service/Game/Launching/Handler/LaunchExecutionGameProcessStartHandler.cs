// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Service.Game.Launching.Context;
using Snap.Hutao.Service.Git;
using Snap.Hutao.Win32.Foundation;

namespace Snap.Hutao.Service.Game.Launching.Handler;

internal sealed class LaunchExecutionGameProcessStartHandler : AbstractLaunchExecutionHandler
{
    public override async ValueTask BeforeAsync(BeforeLaunchExecutionContext context)
    {
        // Island 注入已改为本地 nvd3dump.dll + 共享内存协议，不再依赖联网同步 ContentDelivery。
        // 保持其他分支原有行为：仅在非提权启动时沿用仓库同步逻辑。
        if (!context.LaunchOptions.IsIslandEnabled.Value && !HutaoRuntime.IsProcessElevated)
        {
            (bool result, _) = await context.ServiceProvider
                .GetRequiredService<IGitRepositoryService>()
                .EnsureRepositoryAsync("Snap.ContentDelivery")
                .ConfigureAwait(false);

            HutaoException.ThrowIfNot(result, SH.ServiceGameLaunchingHandlerGameProcessStartRepositorySyncFailed);
        }
    }

    public override async ValueTask ExecuteAsync(LaunchExecutionContext context)
    {
        try
        {
            // 对于suspended进程（Yae注入模式、Island模式），需要先Start()创建进程，然后ResumeMainThread()恢复主线程
            // 对于正常启动的进程（ShellExecute、DiagnosticsProcess），只调用Start()
            context.Process.Start();

            // 尝试恢复主线程（适用于suspended进程）
            try
            {
                context.Process.ResumeMainThread();
            }
            catch (HutaoException ex) when (ex.Message.Contains("ResumeMainThread is not supported"))
            {
                // ResumeMainThread不支持，说明是正常启动的进程（DiagnosticsProcess），忽略此错误
            }

            await context.TaskContext.SwitchToMainThreadAsync();
            GameLifeCycle.IsGameRunningProperty.Value = true;
        }
        catch (Win32Exception ex)
        {
            if (ex.HResult is HRESULT.E_FAIL)
            {
                return;
            }

            throw;
        }

        context.Progress.Report(new(SH.ServiceGameLaunchPhaseProcessStarted));
    }
}