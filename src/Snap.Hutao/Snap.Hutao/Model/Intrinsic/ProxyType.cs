// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Model.Intrinsic;

[ExtendedEnum]
internal enum ProxyType
{
    [LocalizationKey(nameof(SH.ServiceProxyTypeNone))]
    None = 0,

    [LocalizationKey(nameof(SH.ServiceProxyTypeSystemProxy))]
    SystemProxy = 1,

    [LocalizationKey(nameof(SH.ServiceProxyTypeHttp))]
    Http = 2,

    [LocalizationKey(nameof(SH.ServiceProxyTypeSocks5))]
    Socks5 = 3,
}