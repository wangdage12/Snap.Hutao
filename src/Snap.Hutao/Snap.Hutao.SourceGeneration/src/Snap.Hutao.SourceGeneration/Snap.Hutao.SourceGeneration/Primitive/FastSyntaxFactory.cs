// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Primitive;

// Properties and methods are ordered by return type
internal static partial class FastSyntaxFactory
{
    public static SyntaxToken SemicolonToken { get; } = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

    public static ArgumentListSyntax EmptyArgumentList { get; } = SyntaxFactory.ArgumentList();

    public static TypeArgumentListSyntax EmptyTypeArgumentList { get; } = SyntaxFactory.TypeArgumentList();

    public static BlockSyntax EmptyBlock { get; } = SyntaxFactory.Block();

    public static SyntaxTriviaList NullableEnableTriviaList { get; } = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(EnableKeyword, true)));

    public static AccessorListSyntax GetAndSetAccessorList { get; } = SyntaxFactory.AccessorList(SyntaxFactory.List(
    [
        GetAccessorDeclaration(),
        SetAccessorDeclaration()
    ]));

    public static AccessorDeclarationSyntax GetAccessorDeclaration()
    {
        return SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken);
    }

    public static AccessorDeclarationSyntax SetAccessorDeclaration()
    {
        return SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken);
    }

    public static AssignmentExpressionSyntax CoalesceAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.AssignmentExpression(SyntaxKind.CoalesceAssignmentExpression, left, right);
    }

    public static AssignmentExpressionSyntax SimpleAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right);
    }

    public static ConstructorDeclarationSyntax WithEmptyBlockBody(this ConstructorDeclarationSyntax constructor)
    {
        return constructor.WithBody(EmptyBlock);
    }

    public static ConstructorInitializerSyntax BaseConstructorInitializer(ArgumentListSyntax? argumentList = null)
    {
        return SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, argumentList);
    }

    public static ConversionOperatorDeclarationSyntax ImplicitConversionOperatorDeclaration(TypeSyntax type)
    {
        return SyntaxFactory.ConversionOperatorDeclaration(ImplicitKeyword, type);
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(string qualifiedName)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(qualifiedName));
    }

    public static FileScopedNamespaceDeclarationSyntax FileScopedNamespaceDeclaration(INamespaceSymbol symbol)
    {
        return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(symbol.ToDisplayString()));
    }

    public static InvocationExpressionSyntax WithEmptyArgumentList(this InvocationExpressionSyntax expression)
    {
        return expression.WithArgumentList(EmptyArgumentList);
    }

    public static InvocationExpressionSyntax NameOfExpression(ExpressionSyntax argument)
    {
        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))));
    }

    public static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, SimpleNameSyntax name)
    {
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, name);
    }

    public static ObjectCreationExpressionSyntax WithEmptyArgumentList(this ObjectCreationExpressionSyntax expression)
    {
        return expression.WithArgumentList(EmptyArgumentList);
    }

    public static ParameterSyntax Parameter(TypeSyntax type, SyntaxToken name)
    {
        return SyntaxFactory.Parameter(name).WithType(type);
    }

    public static TypeDeclarationSyntax PartialTypeDeclaration(INamedTypeSymbol typeSymbol)
    {
        string typeName = typeSymbol.Name;

        TypeDeclarationSyntax typeDeclaration = (typeSymbol.TypeKind, typeSymbol.IsRecord) switch
        {
            (TypeKind.Class, false) => SyntaxFactory.ClassDeclaration(typeName),
            (TypeKind.Class, true) => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordDeclaration, RecordKeyword, typeName)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
            (TypeKind.Struct, false) => SyntaxFactory.StructDeclaration(typeName),
            (TypeKind.Struct, true) => SyntaxFactory.RecordDeclaration(SyntaxKind.RecordStructDeclaration, RecordKeyword, typeName)
                .WithClassOrStructKeyword(StructKeyword)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
            (TypeKind.Interface, _) => SyntaxFactory.InterfaceDeclaration(typeName),
            _ => throw new InvalidOperationException("Unsupported type kind for partial declaration: " + typeSymbol.TypeKind)
        };

        if (typeSymbol.IsGenericType)
        {
            typeDeclaration = typeDeclaration.WithTypeParameterList(SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(
                ImmutableArray.CreateRange(typeSymbol.TypeParameters, static tParam => SyntaxFactory.TypeParameter(tParam.Name)))));
        }

        return typeDeclaration.WithModifiers(PartialTokenList);
    }

    public static UsingDirectiveSyntax UsingDirective(string qualifiedName)
    {
        return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(qualifiedName));
    }
}