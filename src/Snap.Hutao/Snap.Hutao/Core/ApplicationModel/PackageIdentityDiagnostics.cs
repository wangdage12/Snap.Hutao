// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO;

namespace Snap.Hutao.Core.ApplicationModel;

/// <summary>
/// Diagnostic helper for PackageIdentityAdapter
/// </summary>
internal static class PackageIdentityDiagnostics
{
    public static void LogDiagnostics()
    {
        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Hutao",
                "startup_diagnostics.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            using (StreamWriter writer = File.CreateText(logPath))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup Diagnostics");
                writer.WriteLine($"HasPackageIdentity: {PackageIdentityAdapter.HasPackageIdentity}");
                writer.WriteLine($"AppVersion: {PackageIdentityAdapter.AppVersion}");
                writer.WriteLine($"AppDirectory: {PackageIdentityAdapter.AppDirectory}");
                writer.WriteLine($"FamilyName: {PackageIdentityAdapter.FamilyName}");
                writer.WriteLine($"PublisherId: {PackageIdentityAdapter.PublisherId}");
                writer.WriteLine("---");
            }

            Debug.WriteLine($"Diagnostics written to: {logPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write diagnostics: {ex.Message}");
        }
    }
}
