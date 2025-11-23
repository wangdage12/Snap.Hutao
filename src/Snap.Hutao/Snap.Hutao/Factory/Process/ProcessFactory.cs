// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;
using Snap.Hutao.Win32;
using Snap.Hutao.Win32.Foundation;
using System.IO;

namespace Snap.Hutao.Factory.Process;

internal sealed class ProcessFactory
{
    public static void KillCurrent()
    {
        global::System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    public static bool TryGetById(int processId, [NotNullWhen(true)] out IProcess? process)
    {
        try
        {
            process = new DiagnosticsProcess(global::System.Diagnostics.Process.GetProcessById(processId));
            return true;
        }
        catch (Exception ex)
        {
            // Process with an Id of $id$ is not running.
            if (ex is not ArgumentException)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        process = null;
        return false;
    }

    public static bool IsRunning(ReadOnlySpan<string> processNames, [NotNullWhen(true)] out IProcess? runningProcess)
    {
        int currentSessionId = global::System.Diagnostics.Process.GetCurrentProcess().SessionId;
        global::System.Diagnostics.Process[] processes = global::System.Diagnostics.Process.GetProcesses();

        // GetProcesses once and manually loop is O(n)
        foreach (global::System.Diagnostics.Process process in processes)
        {
            try
            {
                if (process.SessionId != currentSessionId)
                {
                    continue;
                }

                if (!process.ProcessName.EqualsAny(processNames, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Force access handle to check whether process has exited
                try
                {
                    _ = process.Handle;
                }
                catch (Exception ex)
                {
                    // Assume it's running if access is denied.
                    if (!HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_ACCESS_DENIED))
                    {
                        throw;
                    }
                }

                runningProcess = new DiagnosticsProcess(process);
                return true;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    // 拒绝访问。
                    case Win32Exception we:
                        if (we.NativeErrorCode is (int)WIN32_ERROR.ERROR_ACCESS_DENIED)
                        {
                            runningProcess = new DiagnosticsProcess(process);
                            return true;
                        }

                        break;

                    // Cannot process request because the process ($id$) has exited.
                    case InvalidOperationException ioe:
                        runningProcess = default;
                        return false;
                }

                SentrySdk.CaptureException(ex);
                break;
            }
        }

        runningProcess = default;
        return false;
    }

    public static IProcess CreateUsingShellExecuteRunAs(string arguments, string fileName, string workingDirectory)
    {
        global::System.Diagnostics.Process process = new()
        {
            StartInfo = new()
            {
                Arguments = arguments,
                FileName = fileName,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = workingDirectory,
            },
        };

        return new DiagnosticsProcess(process);
    }

    public static unsafe IProcess CreateSuspended(string arguments, string fileName, string workingDirectory)
    {
        fixed (char* pArguments = arguments)
        {
            fixed (char* pFileName = fileName)
            {
                fixed (char* pWorkingDirectory = workingDirectory)
                {
                    HutaoNativeProcessStartInfo startInfo = new()
                    {
                        ApplicationName = pFileName,
                        CommandLine = pArguments,
                        InheritHandles = BOOL.FALSE,
                        CreationFlags = Win32.System.Threading.PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
                        CurrentDirectory = pWorkingDirectory,
                    };

                    return new NativeProcess(HutaoNative.Instance.MakeProcess(startInfo));
                }
            }
        }
    }

    public static IProcess CreateUsingFullTrustSuspended(string arguments, string fileName, string workingDirectory)
    {
        string repoDirectory = HutaoRuntime.GetDataRepositoryDirectory();
        string fullTrustFilePath = Path.Combine(repoDirectory, "Snap.ContentDelivery", "Snap.Hutao.FullTrust.exe");

        // Check if FullTrust executable exists - if not, fallback to normal admin mode
        if (!File.Exists(fullTrustFilePath))
        {
            string errorMessage = $"""
                Island 功能需要的 FullTrust 进程文件不存在，将使用普通管理员模式启动游戏。
                预期路径：{fullTrustFilePath}
                
                原因：ContentDelivery 仓库尚未下载或初始化失败（常见于非打包模式首次运行）
                
                Island 功能将不可用，但游戏可以正常启动。
                等待仓库下载完成后可重新尝试使用 Island 功能。
                """;

            // Capture as breadcrumb instead of exception
            SentrySdk.AddBreadcrumb(errorMessage, category: "process.fulltrust", level: Sentry.BreadcrumbLevel.Warning);

            // Fallback to normal admin mode - Island features will not work but game can launch
            return CreateUsingShellExecuteRunAs(arguments, fileName, workingDirectory);
        }

        StartUsingShellExecuteRunAs(fullTrustFilePath);

        FullTrustProcessStartInfoRequest request = new()
        {
            ApplicationName = fileName,
            CommandLine = arguments,
            CreationFlags = Win32.System.Threading.PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
            CurrentDirectory = workingDirectory,
        };

        return new FullTrustProcess(request);
    }

    public static void StartUsingShellExecute(string arguments, string fileName)
    {
        global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            Arguments = arguments,
            FileName = fileName,
            UseShellExecute = true,
        });
    }

    public static void StartUsingShellExecuteRunAs(string fileName)
    {
        global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,
            Verb = "runas",
        });
    }
}