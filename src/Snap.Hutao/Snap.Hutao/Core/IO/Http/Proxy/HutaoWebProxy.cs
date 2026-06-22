// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Service;
using System.Net;
using System.Net.Http;

namespace Snap.Hutao.Core.IO.Http.Proxy;

internal sealed class HutaoWebProxy : IWebProxy
{
    private readonly AppOptions appOptions;
    private readonly HttpProxyUsingSystemProxy systemProxy;

    public HutaoWebProxy(AppOptions appOptions, HttpProxyUsingSystemProxy systemProxy)
    {
        this.appOptions = appOptions;
        this.systemProxy = systemProxy;
    }

    public ICredentials? Credentials
    {
        get => InnerProxy.Credentials;
        set => InnerProxy.Credentials = value;
    }

    public string DisplayProxyUri
    {
        get
        {
            if (!appOptions.ProxyEnabled.Value)
            {
                return systemProxy.DisplayProxyUri;
            }

            return appOptions.ProxyType.Value switch
            {
                ProxyType.SystemProxy => systemProxy.DisplayProxyUri,
                ProxyType.Http => string.IsNullOrEmpty(appOptions.ProxyAddress.Value) 
                    ? systemProxy.DisplayProxyUri 
                    : $"http://{appOptions.ProxyAddress.Value}:{appOptions.ProxyPort.Value}",
                ProxyType.Socks5 => string.IsNullOrEmpty(appOptions.ProxyAddress.Value)
                    ? systemProxy.DisplayProxyUri
                    : $"socks5://{appOptions.ProxyAddress.Value}:{appOptions.ProxyPort.Value}",
                ProxyType.None => systemProxy.DisplayProxyUri,
                _ => systemProxy.DisplayProxyUri,
            };
        }
    }

    private IWebProxy InnerProxy
    {
        get
        {
            if (!appOptions.ProxyEnabled.Value)
            {
                return systemProxy;
            }

            return appOptions.ProxyType.Value switch
            {
                ProxyType.SystemProxy => systemProxy,
                ProxyType.Http => CreateHttpProxy(),
                ProxyType.Socks5 => CreateSocks5Proxy(),
                ProxyType.None => systemProxy,
                _ => systemProxy,
            };
        }
    }

    public Uri? GetProxy(Uri destination)
    {
        return InnerProxy.GetProxy(destination);
    }

    public bool IsBypassed(Uri host)
    {
        return InnerProxy.IsBypassed(host);
    }

    private IWebProxy CreateHttpProxy()
    {
        string address = appOptions.ProxyAddress.Value;
        int port = appOptions.ProxyPort.Value;

        if (string.IsNullOrEmpty(address))
        {
            return systemProxy;
        }

        WebProxy webProxy = new()
        {
            Address = new Uri($"http://{address}:{port}"),
        };

        return webProxy;
    }

    private IWebProxy CreateSocks5Proxy()
    {
        string address = appOptions.ProxyAddress.Value;
        int port = appOptions.ProxyPort.Value;

        if (string.IsNullOrEmpty(address))
        {
            return systemProxy;
        }

        return new Socks5WebProxy(address, port);
    }
}

internal sealed class Socks5WebProxy : IWebProxy
{
    private readonly string address;
    private readonly int port;

    public Socks5WebProxy(string address, int port)
    {
        this.address = address;
        this.port = port;
    }

    public ICredentials? Credentials { get; set; }

    public Uri? GetProxy(Uri destination)
    {
        return new Uri($"socks5://{address}:{port}");
    }

    public bool IsBypassed(Uri host)
    {
        return false;
    }
}