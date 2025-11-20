// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static partial class FastSyntaxFactory
{
    public static BinaryExpressionSyntax AddExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, left, right);
    }

    public static BinaryExpressionSyntax AsExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, left, right);
    }

    public static BinaryExpressionSyntax BitwiseOrExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, left, right);
    }

    public static BinaryExpressionSyntax CoalesceExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, left, right);
    }

    public static BinaryExpressionSyntax EqualsExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, left, right);
    }

    public static BinaryExpressionSyntax LogicalAndExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, left, right);
    }

    public static BinaryExpressionSyntax SubtractExpression(ExpressionSyntax left, ExpressionSyntax right)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, left, right);
    }
}