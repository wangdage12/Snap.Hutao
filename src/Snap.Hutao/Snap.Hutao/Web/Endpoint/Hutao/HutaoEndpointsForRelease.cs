// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

[Service(ServiceLifetime.Singleton, typeof(IHutaoEndpoints), Key = HutaoEndpointsKind.Release)]
internal sealed class HutaoEndpointsForRelease : IHutaoEndpoints
{
    string IHomaRootAccess.Root { get => "http://server.wdg.cloudns.ch:5222"; }

    string IInfrastructureRootAccess.Root { get => "http://server.wdg.cloudns.ch:5222"; }

    string IInfrastructureRawRootAccess.RawRoot { get => "http://server.wdg.cloudns.ch:5222"; }
}