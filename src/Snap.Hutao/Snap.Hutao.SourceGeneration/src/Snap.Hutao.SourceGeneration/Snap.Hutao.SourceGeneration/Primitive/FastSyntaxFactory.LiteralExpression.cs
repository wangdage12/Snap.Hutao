// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static partial class FastSyntaxFactory
{
    public static LiteralExpressionSyntax DefaultLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

    public static LiteralExpressionSyntax FalseLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

    public static LiteralExpressionSyntax NullLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

    public static LiteralExpressionSyntax TrueLiteralExpression { get; } = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

    public static LiteralExpressionSyntax LiteralExpression(bool value)
    {
        return value ? TrueLiteralExpression : FalseLiteralExpression;
    }

    public static LiteralExpressionSyntax NumericLiteralExpression(int value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }

    public static LiteralExpressionSyntax NumericLiteralExpression(SyntaxToken literal)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literal);
    }

    public static LiteralExpressionSyntax StringLiteralExpression(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }
}