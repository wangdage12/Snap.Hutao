// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record FieldInfo
{
    public required string MinimallyQualifiedName { get; init; }

    public required string FullyQualifiedTypeName { get; init; }

    public required string FullyQualifiedTypeNameWithNullabilityAnnotation { get; init; }

    public required EquatableArray<AttributeInfo> Attributes { get; init; }

    public static FieldInfo Create(IFieldSymbol fieldSymbol)
    {
        return new()
        {
            Attributes = ImmutableArray.CreateRange(fieldSymbol.GetAttributes(), AttributeInfo.Create),
            MinimallyQualifiedName = fieldSymbol.Name,
            FullyQualifiedTypeName = fieldSymbol.Type.GetFullyQualifiedName(),
            FullyQualifiedTypeNameWithNullabilityAnnotation = fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations(),
        };
    }

    public bool TryGetAttributeWithFullyQualifiedMetadataName(string name, [NotNullWhen(true)] out AttributeInfo? attributeInfo)
    {
        foreach (AttributeInfo attribute in Attributes)
        {
            if (string.Equals(attribute.FullyQualifiedMetadataName, name, StringComparison.Ordinal))
            {
                attributeInfo = attribute;
                return true;
            }
        }

        attributeInfo = null;
        return false;
    }
}