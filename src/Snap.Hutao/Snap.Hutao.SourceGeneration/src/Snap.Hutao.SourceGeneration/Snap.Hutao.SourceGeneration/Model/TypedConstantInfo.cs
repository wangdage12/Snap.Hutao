// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Immutable;
using System.Globalization;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Model;

internal abstract record TypedConstantInfo
{
    public static TypedConstantInfo Create(TypedConstant arg)
    {
        if (arg.IsNull)
        {
            return new Null();
        }

        if (arg.Kind == TypedConstantKind.Array)
        {
            string elementTypeName = ((IArrayTypeSymbol)arg.Type!).ElementType.GetFullyQualifiedName();
            ImmutableArray<TypedConstantInfo> items = ImmutableArray.CreateRange(arg.Values, Create);
            return new Array(elementTypeName, items);
        }

        return (arg.Kind, arg.Value) switch
        {
            (TypedConstantKind.Primitive, string text) => new Primitive.String(text),
            (TypedConstantKind.Primitive, bool flag) => new Primitive.Boolean(flag),
            (TypedConstantKind.Primitive, { } value) => value switch
            {
                byte b => new Primitive.Of<byte>(b),
                char c => new Primitive.Of<char>(c),
                double d => new Primitive.Of<double>(d),
                float f => new Primitive.Of<float>(f),
                int i => new Primitive.Of<int>(i),
                long l => new Primitive.Of<long>(l),
                sbyte sb => new Primitive.Of<sbyte>(sb),
                short sh => new Primitive.Of<short>(sh),
                uint ui => new Primitive.Of<uint>(ui),
                ulong ul => new Primitive.Of<ulong>(ul),
                ushort ush => new Primitive.Of<ushort>(ush),
                _ => throw new ArgumentException("Invalid primitive type")
            },
            (TypedConstantKind.Type, ITypeSymbol type) => new Type(type.GetFullyQualifiedName(), type is INamedTypeSymbol { IsUnboundGenericType: true }),
            (TypedConstantKind.Enum, { } value) => new Enum(arg.Type!.GetFullyQualifiedName(), value),
            _ => throw new ArgumentException("Invalid typed constant type"),
        };
    }

    public abstract ExpressionSyntax GetSyntax();

    public sealed record Array : TypedConstantInfo
    {
        public Array(string fullyQualifiedElementTypeName, EquatableArray<TypedConstantInfo> items)
        {
            FullyQualifiedElementTypeName = fullyQualifiedElementTypeName;
            Items = items;
        }

        public string FullyQualifiedElementTypeName { get; }

        public EquatableArray<TypedConstantInfo> Items { get; }

        public override ExpressionSyntax GetSyntax()
        {
            return CollectionExpression(SeparatedList<CollectionElementSyntax>(
                Items.SelectAsArray(static c => ExpressionElement(c.GetSyntax()))));
        }
    }

    public abstract record Primitive : TypedConstantInfo
    {
        public sealed record String : TypedConstantInfo
        {
            public String(string Value)
            {
                this.Value = Value;
            }

            public string Value { get; }

            public override ExpressionSyntax GetSyntax()
            {
                return StringLiteralExpression(Value);
            }
        }

        public sealed record Boolean : TypedConstantInfo
        {
            public Boolean(bool value)
            {
                Value = value;
            }

            public bool Value { get; }

            public override ExpressionSyntax GetSyntax()
            {
                return Value ? TrueLiteralExpression : FalseLiteralExpression;
            }
        }

        public sealed record Of<T> : TypedConstantInfo
            where T : unmanaged, IEquatable<T>
        {
            public Of(T value)
            {
                Value = value;
            }

            public T Value { get; }

            public override ExpressionSyntax GetSyntax()
            {
                return NumericLiteralExpression(Value switch
                {
                    byte b => Literal(b),
                    char c => Literal(c),

                    // For doubles, we need to manually format it and always add the trailing "D" suffix.
                    // This ensures that the correct type is produced if the expression was assigned to
                    // an object (eg. the literal was used in an attribute object parameter/property).
                    double d => Literal($"{d.ToString("R", CultureInfo.InvariantCulture)}D", d),

                    // For floats, Roslyn will automatically add the "F" suffix, so no extra work is needed
                    float f => Literal(f),
                    int i => Literal(i),
                    long l => Literal(l),
                    sbyte sb => Literal(sb),
                    short sh => Literal(sh),
                    uint ui => Literal(ui),
                    ulong ul => Literal(ul),
                    ushort ush => Literal(ush),
                    _ => throw new ArgumentException("Invalid primitive type")
                });
            }
        }
    }

    public sealed record Type : TypedConstantInfo
    {
        public Type(string fullyQualifiedTypeName, bool isUnboundGeneric)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            IsUnboundGeneric = isUnboundGeneric;
        }

        public string FullyQualifiedTypeName { get; }

        public bool IsUnboundGeneric { get; }

        public override ExpressionSyntax GetSyntax()
        {
            return TypeOfExpression(IdentifierName(FullyQualifiedTypeName));
        }
    }

    public sealed record Enum : TypedConstantInfo
    {
        public Enum(string fullyQualifiedTypeName, object value)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            Value = value;
        }

        public string FullyQualifiedTypeName { get; }

        public object Value { get; }

        public override ExpressionSyntax GetSyntax()
        {
            // We let Roslyn parse the value expression, so that it can automatically handle both positive and negative values. This
            // is needed because negative values have a different syntax tree (UnaryMinusExpression holding the numeric expression).
            ExpressionSyntax valueExpression = ParseExpression(Value.ToString());

            // If the value is negative, we have to put parentheses around them (to avoid CS0075 errors)
            if (valueExpression is PrefixUnaryExpressionSyntax unaryExpression && unaryExpression.IsKind(SyntaxKind.UnaryMinusExpression))
            {
                valueExpression = ParenthesizedExpression(valueExpression);
            }

            // Now we can safely return the cast expression for the target enum type (with optional parentheses if needed)
            return CastExpression(IdentifierName(FullyQualifiedTypeName), valueExpression);
        }
    }

    public sealed record Null : TypedConstantInfo
    {
        public override ExpressionSyntax GetSyntax()
        {
            return NullLiteralExpression;
        }
    }
}