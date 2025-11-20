// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Model;

internal sealed record HierarchyInfo
{
    public required string FileNameHint { get; init; }

    public required string MetadataName { get; init; }

    public required string Namespace { get; init; }

    public required EquatableArray<TypeInfo> Hierarchy { get; init; }

    public static HierarchyInfo Create(INamedTypeSymbol typeSymbol)
    {
        using ImmutableArrayBuilder<TypeInfo> hierarchy = ImmutableArrayBuilder<TypeInfo>.Rent();

        for (INamedTypeSymbol? parent = typeSymbol; parent is not null; parent = parent.ContainingType)
        {
            hierarchy.Add(TypeInfo.Create(parent));
        }

        return new()
        {
            FileNameHint = typeSymbol.GetFullyQualifiedMetadataName(),
            MetadataName = typeSymbol.MetadataName,
            Namespace = typeSymbol.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
            Hierarchy = hierarchy.ToImmutable(),
        };
    }

    public CompilationUnitSyntax GetCompilationUnit(ImmutableArray<MemberDeclarationSyntax> memberDeclarations, BaseListSyntax? baseList = null)
    {
        // Create the partial type declaration with the given member declarations.
        // This code produces a class declaration as follows:
        //
        // partial <TYPE_KIND> <TYPE_NAME>
        // {
        //     <MEMBERS>
        // }
        TypeDeclarationSyntax typeDeclarationSyntax =
            Hierarchy[0].GetSyntax()
            .AddModifiers(PartialKeyword)
            .AddMembers(memberDeclarations.ToArray());

        // Add the base list, if present
        if (baseList is not null)
        {
            typeDeclarationSyntax = typeDeclarationSyntax.WithBaseList(baseList);
        }

        // Add all parent types in ascending order, if any
        foreach (TypeInfo parentType in Hierarchy.AsSpan()[1..])
        {
            typeDeclarationSyntax =
                parentType.GetSyntax()
                .AddModifiers(PartialKeyword)
                .AddMembers(typeDeclarationSyntax);
        }

        if (Namespace is "")
        {
            // If there is no namespace, attach the pragma directly to the declared type,
            // and skip the namespace declaration. This will produce code as follows:
            //
            // <SYNTAX_TRIVIA>
            // <TYPE_HIERARCHY>
            return CompilationUnit()
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    typeDeclarationSyntax
                        .WithLeadingTrivia(NullableEnableTriviaList)));
        }

        // Create the compilation unit with disabled warnings, target namespace and generated type.
        // This will produce code as follows:
        //
        // namespace <NAMESPACE>;
        // <TYPE_HIERARCHY>
        return CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(Namespace)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(typeDeclarationSyntax
                    .WithLeadingTrivia(NullableEnableTriviaList)))));
    }
}