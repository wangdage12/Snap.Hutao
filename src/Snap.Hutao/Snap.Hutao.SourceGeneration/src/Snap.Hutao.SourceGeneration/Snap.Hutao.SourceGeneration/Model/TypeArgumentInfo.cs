// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record TypeArgumentInfo
{
    public required string MinimallyQualifiedName { get; init; }

    public required string FullyQualifiedTypeName { get; init; }

    public required string FullyQualifiedTypeNameWithNullabilityAnnotations { get; init; }

    public static TypeArgumentInfo Create(ITypeSymbol typeSymbol)
    {
        return new()
        {
            MinimallyQualifiedName = typeSymbol.Name,
            FullyQualifiedTypeName = typeSymbol.GetFullyQualifiedName(),
            FullyQualifiedTypeNameWithNullabilityAnnotations = typeSymbol.GetFullyQualifiedNameWithNullabilityAnnotations(),
        };
    }

    public TypeSyntax GetSyntax()
    {
        return SyntaxFactory.ParseTypeName(FullyQualifiedTypeNameWithNullabilityAnnotations);
    }
}