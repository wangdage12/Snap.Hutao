// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record ParameterInfo
{
    public required string Name { get; init; }

    public required string FullyQualifiedTypeName { get; init; }

    public required string FullyQualifiedTypeMetadataName { get; init; }

    public required string FullyQualifiedTypeNameWithNullabilityAnnotations { get; init; }

    public static ParameterInfo Create(IParameterSymbol parameterSymbol)
    {
        return new()
        {
            Name = parameterSymbol.Name,
            FullyQualifiedTypeName = parameterSymbol.Type.GetFullyQualifiedName(),
            FullyQualifiedTypeMetadataName = parameterSymbol.Type.GetFullyQualifiedMetadataName(),
            FullyQualifiedTypeNameWithNullabilityAnnotations = parameterSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations(),
        };
    }

    public ParameterSyntax GetSyntax()
    {
        return Parameter(ParseTypeName(FullyQualifiedTypeNameWithNullabilityAnnotations), Identifier(Name));
    }
}