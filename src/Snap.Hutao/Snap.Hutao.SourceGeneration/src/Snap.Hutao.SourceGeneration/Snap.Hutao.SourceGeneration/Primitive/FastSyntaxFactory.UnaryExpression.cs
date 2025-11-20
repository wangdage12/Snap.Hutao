// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static partial class FastSyntaxFactory
{
    public static PostfixUnaryExpressionSyntax SuppressNullableWarningExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, operand);
    }

    public static PrefixUnaryExpressionSyntax AddressOfExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.AddressOfExpression, operand);
    }

    public static PrefixUnaryExpressionSyntax LogicalNotExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, operand);
    }

    public static PrefixUnaryExpressionSyntax PointerIndirectionExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression, operand);
    }

    public static PrefixUnaryExpressionSyntax PreDecrementExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreDecrementExpression, operand);
    }

    public static PrefixUnaryExpressionSyntax PreIncrementExpression(ExpressionSyntax operand)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, operand);
    }
}