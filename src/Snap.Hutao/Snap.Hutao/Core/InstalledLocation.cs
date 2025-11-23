// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ApplicationModel;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Snap.Hutao.Core;

internal static class InstalledLocation
{
    public static string GetAbsolutePath(string relativePath)
    {
        return Path.Combine(PackageIdentityAdapter.AppDirectory, relativePath);
    }

    public static void CopyFileFromApplicationUri(string url, string path)
    {
        CopyApplicationUriFileCoreAsync(url, path).GetAwaiter().GetResult();

        static async Task CopyApplicationUriFileCoreAsync(string url, string path)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            Uri uri = url.ToUri();
            Stream outputStream;

            if (PackageIdentityAdapter.HasPackageIdentity)
            {
                // Packaged: use StorageFile
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                outputStream = (await file.OpenReadAsync()).AsStreamForRead();
            }
            else
            {
                // Unpackaged: read from file system directly
                // Assume ms-appx:/// points to the app directory
                string localPath = uri.LocalPath.TrimStart('/');
                string fullPath = Path.Combine(PackageIdentityAdapter.AppDirectory, localPath);
                outputStream = File.OpenRead(fullPath);
            }

            using (outputStream)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        FileInfo fileInfo = new(path);
                        FileSecurity fileSecurity = fileInfo.GetAccessControl();
                        SecurityIdentifier? user = WindowsIdentity.GetCurrent().User;

                        if (user is not null)
                        {
                            fileSecurity.AddAccessRule(new(user, FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                            fileInfo.SetAccessControl(fileSecurity);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                using (FileStream inputStream = File.Create(path))
                {
                    await outputStream.CopyToAsync(inputStream).ConfigureAwait(false);
                }
            }
        }
    }
}