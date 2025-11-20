// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record PropertyInfo
{
    public required string Name { get; init; }

    public required string FullyQualifiedTypeName { get; init; }

    public required string FullyQualifiedTypeNameWithNullabilityAnnotation { get; init; }

    public required bool TypeIsValueType { get; init; }

    public required bool TypeIsReferenceType { get; init; }

    public required EquatableArray<AttributeInfo> Attributes { get; init; }

    public required Accessibility DeclaredAccessibility { get; init; }

    public required Accessibility? GetMethodAccessibility { get; init; }

    public required Accessibility? SetMethodAccessibility { get; init; }

    [MemberNotNullWhen(true, nameof(FullyQualifiedIndexerParameterTypeName), nameof(IndexerParameterTypeIsValueType), nameof(IndexerParameterTypeIsReferenceType))]
    public required bool IsIndexer { get; init; }

    public required string? FullyQualifiedIndexerParameterTypeName { get; init; }

    public required bool? IndexerParameterTypeIsValueType { get; init; }

    public required bool? IndexerParameterTypeIsReferenceType { get; init; }

    public required bool IsStatic { get; init; }

    public static PropertyInfo Create(IPropertySymbol propertySymbol)
    {
        ITypeSymbol? indexerParameterType = propertySymbol.IsIndexer ? propertySymbol.Parameters[0].Type : null;

        return new()
        {
            Attributes = ImmutableArray.CreateRange(propertySymbol.GetAttributes(), AttributeInfo.Create),
            DeclaredAccessibility = propertySymbol.DeclaredAccessibility,
            GetMethodAccessibility = propertySymbol.GetMethod?.DeclaredAccessibility,
            SetMethodAccessibility = propertySymbol.SetMethod?.DeclaredAccessibility,
            Name = propertySymbol.Name,
            FullyQualifiedTypeName = propertySymbol.Type.GetFullyQualifiedName(),
            FullyQualifiedTypeNameWithNullabilityAnnotation = propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations(),
            TypeIsValueType = propertySymbol.Type.IsValueType,
            TypeIsReferenceType = propertySymbol.Type.IsReferenceType,
            IsIndexer = propertySymbol.IsIndexer,
            FullyQualifiedIndexerParameterTypeName = indexerParameterType?.GetFullyQualifiedNameWithNullabilityAnnotations(),
            IndexerParameterTypeIsValueType = indexerParameterType?.IsValueType,
            IndexerParameterTypeIsReferenceType = indexerParameterType?.IsReferenceType,
            IsStatic = propertySymbol.IsStatic,
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