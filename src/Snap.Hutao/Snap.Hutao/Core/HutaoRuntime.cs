// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Core.ApplicationModel;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.Hashing;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Service.Git;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Hutao.Core;

internal static class HutaoRuntime
{
    public static Version Version { get; } = PackageIdentityAdapter.AppVersion;

    public static string UserAgent { get; } = $"Snap Hutao/{Version}";

    public static string DataDirectory { get; } = InitializeDataDirectory();

    public static string LocalCacheDirectory { get; } = InitializeLocalCacheDirectory();

    public static string FamilyName { get; } = PackageIdentityAdapter.FamilyName;

    public static string DeviceId { get; } = InitializeDeviceId();

    public static WebView2Version WebView2Version { get; } = InitializeWebView2();

    // ⚠️ 延迟初始化以避免循环依赖
    private static readonly Lazy<bool> LazyIsProcessElevated = new(GetIsProcessElevated);
    
    public static bool IsProcessElevated => LazyIsProcessElevated.Value;

    // Requires main thread
    public static bool IsAppNotificationEnabled { get; } = CheckAppNotificationEnabled();

    public static string? GetDisplayName()
    {
        // AppNameAndVersion
        // AppDevNameAndVersion
        // AppElevatedNameAndVersion
        // AppElevatedDevNameAndVersion
        string name = new StringBuilder()
            .Append("App")
            .AppendIf(IsProcessElevated, "Elevated")
#if DEBUG
            .Append("Dev")
#endif
            .Append("NameAndVersion")
            .ToString();

        Debug.Assert(XamlApplicationLifetime.CultureInfoInitialized);
        string? displayName = SH.GetString(name, Version);
        return displayName is null ? null : string.Intern(displayName);
    }

    public static ValueFile GetDataDirectoryFile(string fileName)
    {
        return string.Intern(Path.Combine(DataDirectory, fileName));
    }

    public static ValueFile GetDataUpdateCacheDirectoryFile(string fileName)
    {
        string directory = Path.Combine(DataDirectory, "UpdateCache");
        Directory.CreateDirectory(directory);
        return string.Intern(Path.Combine(directory, fileName));
    }

    public static ValueDirectory GetDataServerCacheDirectory()
    {
        string directory = Path.Combine(DataDirectory, "ServerCache");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataBackgroundDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Background");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataScreenshotDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Screenshot");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetDataRepositoryDirectory()
    {
        string directory = Path.Combine(DataDirectory, "Repository");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    public static ValueDirectory GetLocalCacheImageCacheDirectory()
    {
        string directory = Path.Combine(LocalCacheDirectory, "ImageCache");
        Directory.CreateDirectory(directory);
        return string.Intern(directory);
    }

    private static bool GetIsProcessElevated()
    {
        // ⚠️ 这里调用 LocalSetting 时，确保 DataDirectory 已经初始化完成
        try
        {
            return LocalSetting.Get(SettingKeys.OverrideElevationRequirement, false) || Environment.IsPrivilegedProcess;
        }
        catch
        {
            // 如果读取失败，使用默认值
            return Environment.IsPrivilegedProcess;
        }
    }

    private static string InitializeLocalCacheDirectory()
    {
        if (PackageIdentityAdapter.HasPackageIdentity)
        {
            return Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
        }

        // Unpackaged: use %LOCALAPPDATA%\Snap.Hutao\Cache
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        const string FolderName
#if IS_ALPHA_BUILD
            = "HutaoAlpha";
#elif IS_CANARY_BUILD
            = "HutaoCanary";
#else
            = "Hutao";
#endif
        string cacheDir = Path.Combine(localAppData, FolderName, "Cache");
        Directory.CreateDirectory(cacheDir);
        return cacheDir;
    }

    private static bool CheckAppNotificationEnabled()
    {
        try
        {
            return AppNotificationManager.Default.Setting is AppNotificationSetting.Enabled;
        }
        catch
        {
            // In unpackaged mode, this might fail - return false
            return false;
        }
    }

    private static string InitializeDataDirectory()
    {
        const string FolderName
#if IS_ALPHA_BUILD
        = "HutaoAlpha";
#elif IS_CANARY_BUILD
        = "HutaoCanary";
#else
        = "Hutao";
#endif

        // ⚠️ 不要在这里调用 LocalSetting - 会导致循环依赖
        // 先确定默认的数据目录位置

        // Check if the old documents path exists
        string myDocumentsHutaoDirectory = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderName));
        if (Directory.Exists(myDocumentsHutaoDirectory))
        {
            return myDocumentsHutaoDirectory;
        }

        // Use LocalApplicationData
        string localApplicationData;
        if (PackageIdentityAdapter.HasPackageIdentity)
        {
            localApplicationData = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            // Unpackaged: use %LOCALAPPDATA%
            localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        
        string defaultPath = Path.GetFullPath(Path.Combine(localApplicationData, FolderName));
        
        // ⚠️ 延迟处理：在第一次使用 LocalSetting 后再检查是否有自定义路径
        // 这里返回默认路径，后续通过 LocalSetting 可能会更新
        try
        {
            Directory.CreateDirectory(defaultPath);
        }
        catch (Exception ex)
        {
            // FileNotFoundException | UnauthorizedAccessException
            HutaoException.InvalidOperation($"Failed to create data folder: {defaultPath}", ex);
        }

        return defaultPath;
    }

    private static string InitializeDeviceId()
    {
        string userName = Environment.UserName;
        object? machineGuid = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography\", "MachineGuid", userName);
        return Hash.ToHexString(HashAlgorithmName.MD5, $"{userName}{machineGuid}");
    }

    private static WebView2Version InitializeWebView2()
    {
        try
        {
            string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return new(version, version, true);
        }
        catch (FileNotFoundException)
        {
            return new(string.Empty, SH.CoreWebView2HelperVersionUndetected, false);
        }
    }
}