// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Snap.Hutao.Core.ApplicationModel;

/// <summary>
/// Adapter to handle both packaged and unpackaged app scenarios
/// </summary>
internal static class PackageIdentityAdapter
{
    private static readonly Lazy<bool> LazyHasPackageIdentity = new(CheckPackageIdentity);
    private static readonly Lazy<string> LazyAppDirectory = new(GetAppDirectoryPath);
    private static readonly Lazy<Version> LazyAppVersion = new(GetAppVersionInternal);
    private static readonly Lazy<string> LazyFamilyName = new(GetFamilyNameInternal);
    private static readonly Lazy<string> LazyPublisherId = new(GetPublisherIdInternal);

    /// <summary>
    /// Check if the app has package identity
    /// </summary>
    public static bool HasPackageIdentity => LazyHasPackageIdentity.Value;

    /// <summary>
    /// Get application installation directory
    /// </summary>
    public static string AppDirectory => LazyAppDirectory.Value;

    /// <summary>
    /// Get application version
    /// </summary>
    public static Version AppVersion => LazyAppVersion.Value;

    /// <summary>
    /// Get package family name (or fallback for unpackaged)
    /// </summary>
    public static string FamilyName => LazyFamilyName.Value;

    /// <summary>
    /// Get publisher ID (or fallback for unpackaged)
    /// </summary>
    public static string PublisherId => LazyPublisherId.Value;

    private static bool CheckPackageIdentity()
    {
        try
        {
            // Try to access Package.Current - if it throws, we don't have package identity
            _ = Windows.ApplicationModel.Package.Current.Id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetAppDirectoryPath()
    {
        if (HasPackageIdentity)
        {
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        }

        // Unpackaged: use the exe directory
        string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
        ArgumentException.ThrowIfNullOrEmpty(exePath);
        string? directory = Path.GetDirectoryName(exePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        return directory;
    }

    private static Version GetAppVersionInternal()
    {
        if (HasPackageIdentity)
        {
            return Windows.ApplicationModel.Package.Current.Id.Version.ToVersion();
        }

        // Unpackaged: use assembly version
        Assembly assembly = Assembly.GetExecutingAssembly();
        Version? version = assembly.GetName().Version;
        return version ?? new Version(1, 0, 0, 0);
    }

    private static string GetFamilyNameInternal()
    {
        if (HasPackageIdentity)
        {
            return Windows.ApplicationModel.Package.Current.Id.FamilyName;
        }

        // Unpackaged: use a deterministic fallback
        return "Snap.Hutao.Unpackaged";
    }

    private static string GetPublisherIdInternal()
    {
        if (HasPackageIdentity)
        {
            return Windows.ApplicationModel.Package.Current.Id.PublisherId;
        }

        // Unpackaged: use a fallback
        return "CN=DGPStudio";
    }
}
