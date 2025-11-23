// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Core;
using Snap.Hutao.Core.Logging;
using Snap.Hutao.Core.Security.Principal;
using Snap.Hutao.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using WinRT;

[assembly: DisableRuntimeMarshalling]

namespace Snap.Hutao;

[SuppressMessage("", "SH001")]
public static partial class Bootstrap
{
    private const string LockName = "SNAP_HUTAO_BOOTSTRAP_LOCK";
    private static readonly ApplicationInitializationCallback AppInitializationCallback = InitializeApp;
    private static Mutex? mutex;

    internal static void UseNamedPipeRedirection()
    {
        Debug.Assert(mutex is not null);
        DisposableMarshal.DisposeAndClear(ref mutex);
    }

    [STAThread]
    private static void Main(string[] args)
    {
        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Starting...");
        #endif

        if (Mutex.TryOpenExisting(LockName, out _))
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Another instance is running");
            #endif
            return;
        }

        try
        {
            MutexSecurity mutexSecurity = new();
            mutexSecurity.AddAccessRule(new(SecurityIdentifiers.Everyone, MutexRights.FullControl, AccessControlType.Allow));
            mutex = MutexAcl.Create(true, LockName, out bool created, mutexSecurity);
            Debug.Assert(created);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Mutex created");
            #endif
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] WaitHandleCannotBeOpenedException");
            #endif
            return;
        }

        // Although we 'using' mutex there, the actual disposal is done in AppActivation
        // The using is just to ensure we dispose the mutex when the application exits
        using (mutex)
        {
            if (!OSPlatformSupported())
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] OS not supported");
                #endif
                return;
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Setting environment variables");
            #endif

            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXARRAYSPERPARTITION", "128");
            AppContext.SetData("MVVMTOOLKIT_ENABLE_INOTIFYPROPERTYCHANGING_SUPPORT", false);

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Initializing COM wrappers");
            #endif

            ComWrappersSupport.InitializeComWrappers();

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Initializing DI container");
            #endif

            // By adding the using statement, we can dispose the injected services when closing
            using (ServiceProvider serviceProvider = DependencyInjection.Initialize())
            {
                Thread.CurrentThread.Name = "Snap Hutao Application Main Thread";

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Calling Application.Start()");
                #endif

                // If you hit a COMException REGDB_E_CLASSNOTREG (0x80040154) during debugging
                // You can delete bin and obj folder and then rebuild.
                // In a Desktop app this runs a message pump internally,
                // and does not return until the application shuts down.
                Application.Start(AppInitializationCallback);
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Application.Start() returned");
                #endif

                XamlApplicationLifetime.Exited = true;
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Flushing Sentry");
            #endif

            SentrySdk.Flush();
        }

        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[Bootstrap.Main] Exiting");
        #endif
    }

    private static void InitializeApp(ApplicationInitializationCallbackParams param)
    {
        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[Bootstrap.InitializeApp] Callback invoked");
        #endif

        Gen2GcCallback.Register(() =>
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug("Gen2 GC triggered.", "Runtime"));
            return true;
        });

        IServiceProvider serviceProvider = Ioc.Default;

        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[Bootstrap.InitializeApp] Creating App instance");
        #endif

        // ⚠️ 只创建 App
        // TaskContext 将在第一次被需要时自动创建（延迟初始化）
        _ = serviceProvider.GetRequiredService<App>();
        
        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[Bootstrap.InitializeApp] Initialization complete (TaskContext will be lazily created)");
        #endif
    }

    private static bool OSPlatformSupported()
    {
        if (!HutaoNative.Instance.IsCurrentWindowsVersionSupported())
        {
            const string Message = """
                Snap Hutao 无法在版本低于 10.0.19045.5371 的 Windows 上运行，请更新系统。
                Snap Hutao cannot run on Windows versions earlier than 10.0.19045.5371. Please update your system.
                """;
            HutaoNative.Instance.ShowErrorMessage("Warning | 警告", Message);
            return false;
        }

        return true;
    }
}