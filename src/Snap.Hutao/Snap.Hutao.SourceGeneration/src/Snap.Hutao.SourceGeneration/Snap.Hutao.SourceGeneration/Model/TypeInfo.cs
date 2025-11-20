// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record TypeInfo
{
    public required string FullyQualifiedName { get; init; }

    public required string FullyQualifiedNameWithoutTypeParameters { get; init; }

    public required string FullyQualifiedMetadataName { get; init; }

    public required string MinimallyQualifiedName { get; init; }

    public required string Name { get; init; }

    public required TypeKind Kind { get; init; }

    public required bool IsRecord { get; init; }

    public required EquatableArray<TypeArgumentInfo> TypeArguments { get; init; }

    public static TypeInfo Create(INamedTypeSymbol symbol)
    {
        return new()
        {
            FullyQualifiedName = symbol.GetFullyQualifiedName(),
            FullyQualifiedNameWithoutTypeParameters = symbol.GetFullyQualifiedNameWithoutTypeParameters(),
            FullyQualifiedMetadataName = symbol.GetFullyQualifiedMetadataName(),
            MinimallyQualifiedName = symbol.GetMinimallyQualifiedName(),
            Name = symbol.Name,
            Kind = symbol.TypeKind,
            IsRecord = symbol.IsRecord,
            TypeArguments = ImmutableArray.CreateRange(symbol.TypeArguments, TypeArgumentInfo.Create),
        };
    }

    public TypeDeclarationSyntax GetSyntax()
    {
        // Create the partial type declaration with the kind.
        // This code produces a class declaration as follows:
        //
        // <TYPE_KIND> <TYPE_NAME>
        // {
        // }
        //
        // Note that specifically for record declarations, we also need to explicitly add the open
        // and close brace tokens, otherwise member declarations will not be formatted correctly.
        return (Kind, IsRecord) switch
        {
            (TypeKind.Struct, false) => StructDeclaration(MinimallyQualifiedName),
            (TypeKind.Struct, true) => RecordDeclaration(RecordKeyword, MinimallyQualifiedName)
                .WithClassOrStructKeyword(StructKeyword)
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
            (TypeKind.Interface, _) => InterfaceDeclaration(MinimallyQualifiedName),
            (TypeKind.Class, true) => RecordDeclaration(RecordKeyword, MinimallyQualifiedName)
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
            _ => ClassDeclaration(MinimallyQualifiedName)
        };
    }

    public TypeSyntax GetTypeSyntax(bool includeTypeArguments = true)
    {
        return includeTypeArguments || TypeArguments.IsEmpty
            ? ParseTypeName(FullyQualifiedName)
            : GenericName(FullyQualifiedNameWithoutTypeParameters).WithTypeArgumentList(FastSyntaxFactory.EmptyTypeArgumentList);
    }
}