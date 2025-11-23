// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Core;
using Snap.Hutao.Core.ApplicationModel;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Factory.Picker;
using Snap.Hutao.Service.Git;
using Snap.Hutao.Service.Notification;
using System.IO;
using Windows.Storage;

namespace Snap.Hutao.ViewModel.Setting;

internal sealed class SettingStorageSetDataFolderOperation
{
    public required IFileSystemPickerInteraction FileSystemPickerInteraction { private get; init; }

    public required IContentDialogFactory ContentDialogFactory { private get; init; }

    public required IMessenger Messenger { get; init; }

    internal async ValueTask<bool> TryExecuteAsync()
    {
        if (!FileSystemPickerInteraction.PickFolder().TryGetValue(out string? newFolderPath))
        {
            return false;
        }

        string oldFolderPath = HutaoRuntime.DataDirectory;
        if (UrlPath.IsEqualOrSubdirectory(oldFolderPath, newFolderPath))
        {
            return false;
        }

        if (Path.GetDirectoryName(newFolderPath) is null)
        {
            await ContentDialogFactory.CreateForConfirmAsync(
                    SH.ViewModelSettingStorageSetDataFolderTitle,
                    SH.ViewModelSettingStorageSetDataFolderDescription2)
                .ConfigureAwait(false);

            return false;
        }

        Directory.CreateDirectory(newFolderPath);
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateDirectories(newFolderPath);
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }

        if (entries.Any())
        {
            ContentDialogResult result = await ContentDialogFactory.CreateForConfirmCancelAsync(
                    SH.ViewModelSettingStorageSetDataFolderTitle,
                    SH.FormatViewModelSettingStorageSetDataFolderDescription3(newFolderPath))
                .ConfigureAwait(false);

            if (result is not ContentDialogResult.Primary)
            {
                return false;
            }
        }

        try
        {
            Directory.SetReadOnly(oldFolderPath, false);

            if (PackageIdentityAdapter.HasPackageIdentity)
            {
                // Packaged: use StorageFolder API
                StorageFolder oldFolder = await StorageFolder.GetFolderFromPathAsync(oldFolderPath);
                await oldFolder.CopyAsync(newFolderPath).ConfigureAwait(false);
            }
            else
            {
                // Unpackaged: use standard file I/O
                await CopyDirectoryAsync(oldFolderPath, newFolderPath).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Messenger.Send(InfoBarMessage.Error(ex));
            return false;
        }

        LocalSetting.Set(SettingKeys.PreviousDataDirectoryToDelete, oldFolderPath);
        LocalSetting.Set(SettingKeys.DataDirectory, newFolderPath);
        return true;
    }

    private static async ValueTask CopyDirectoryAsync(string sourceDir, string destDir)
    {
        await Task.Run(() =>
        {
            // Create all directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            // Copy all files
            foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(sourceDir, destDir), true);
            }
        }).ConfigureAwait(false);
    }
}