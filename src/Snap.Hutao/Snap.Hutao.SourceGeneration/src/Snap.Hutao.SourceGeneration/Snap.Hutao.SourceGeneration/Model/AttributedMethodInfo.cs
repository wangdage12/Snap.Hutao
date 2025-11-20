// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record AttributedMethodInfo
{
    public required EquatableArray<AttributeInfo> Attributes { get; init; }

    public required MethodInfo Method { get; init; }

    public static AttributedMethodInfo Create((EquatableArray<AttributeInfo> Attributes, MethodInfo Method) tuple)
    {
        return new()
        {
            Attributes = tuple.Attributes,
            Method = tuple.Method,
        };
    }

    public static AttributedMethodInfo Create(EquatableArray<AttributeInfo> attributes, MethodInfo method)
    {
        return new()
        {
            Attributes = attributes,
            Method = method,
        };
    }
}