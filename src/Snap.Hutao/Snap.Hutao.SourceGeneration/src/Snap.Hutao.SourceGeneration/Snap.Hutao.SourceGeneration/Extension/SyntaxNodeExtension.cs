// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class SyntaxNodeExtension
{
    public static string ToFullStringWithHeader(this SyntaxNode node)
    {
        return $"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.
            
            #pragma warning disable CS1591
            #pragma warning disable SA1003, SA1009, SA1010, SA1013, SA1027, SA1028
            #pragma warning disable SA1101, SA1106, SA1117, SA1122, SA1128
            #pragma warning disable SA1201, SA1202, SA1205
            #pragma warning disable SA1413
            #pragma warning disable SA1514, SA1516
            #pragma warning disable SA1623, SA1629, SA1649

            {node.ToFullString()}
            """;
    }
}