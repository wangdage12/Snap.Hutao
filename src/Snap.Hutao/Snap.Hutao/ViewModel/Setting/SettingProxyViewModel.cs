// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Core;
using Snap.Hutao.Core.IO.Http.Proxy;
using Snap.Hutao.Model;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Service;
using Snap.Hutao.Service.Notification;
using System.Diagnostics;
using System.Net.Http;

namespace Snap.Hutao.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
internal sealed partial class SettingProxyViewModel : Abstraction.ViewModel
{
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly HutaoWebProxy hutaoWebProxy;

    [GeneratedConstructor]
    public partial SettingProxyViewModel(IServiceProvider serviceProvider);

    public partial AppOptions AppOptions { get; }

    [ObservableProperty]
    public partial string? ProxyTestResult { get; set; }

    [ObservableProperty]
    public partial bool IsTestingProxy { get; set; }

    public NameValue<ProxyType>? SelectedProxyType
    {
        get => field ??= AppOptions.ProxyTypes.Single(t => t.Value == AppOptions.ProxyType.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.ProxyType.Value = value.Value;
            }
        }
    }

    [Command("SaveProxyCommand")]
    private void SaveProxy()
    {
        messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingProxySaveSuccess));
    }

    [Command("TestProxyCommand")]
    private async Task TestProxyAsync()
    {
        IsTestingProxy = true;
        ProxyTestResult = SH.ViewModelSettingProxyTesting;

        await taskContext.SwitchToBackgroundAsync();

        try
        {
            using HttpClient httpClient = new(new SocketsHttpHandler
            {
                Proxy = hutaoWebProxy,
                UseProxy = true,
            });

            httpClient.Timeout = TimeSpan.FromSeconds(10);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await httpClient.GetAsync("https://hut.ao").ConfigureAwait(false);
            stopwatch.Stop();

            await taskContext.SwitchToMainThreadAsync();

            if (response.IsSuccessStatusCode)
            {
                ProxyTestResult = SH.FormatViewModelSettingProxyTestSuccess(stopwatch.ElapsedMilliseconds);
            }
            else
            {
                ProxyTestResult = SH.ViewModelSettingProxyTestFailed;
            }
        }
        catch (Exception)
        {
            await taskContext.SwitchToMainThreadAsync();
            ProxyTestResult = SH.ViewModelSettingProxyTestFailed;
        }
        finally
        {
            IsTestingProxy = false;
        }
    }
}