// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Identity;

[Generator(LanguageNames.CSharp)]
internal sealed class IdentityGenerator : IIncrementalGenerator
{
    private const string FileName = "IdentityStructs.json";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<IdentityStructMetadata> provider = context.AdditionalTextsProvider
            .Where(Match)
            .SelectMany(ToMetadata);
        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static bool Match(AdditionalText text)
    {
        return Path.GetFileName(text.Path).EndsWith(FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<IdentityStructMetadata> ToMetadata(AdditionalText text, CancellationToken token)
    {
        string identityJson = text.GetText(token)!.ToString();
        return JsonSerializer.Deserialize<ImmutableArray<IdentityStructMetadata>>(identityJson)!;
    }

    private static void GenerateWrapper(SourceProductionContext production, IdentityStructMetadata metadata)
    {
        try
        {
            Generate(production, metadata);
        }
        catch (Exception ex)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", ex.ToString());
        }
    }

    private static void Generate(SourceProductionContext context, IdentityStructMetadata metadata)
    {
        string metadataName = metadata.Name!;
        if (string.IsNullOrEmpty(metadataName))
        {
            return;
        }

        SyntaxTriviaList trivia = ParseLeadingTrivia($"""
            /// <summary>
            /// {metadata.Documentation}
            /// </summary>

            """);

        IdentifierNameSyntax typeName = IdentifierName(metadataName);
        SyntaxToken typeToken = Identifier(metadataName);

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Model.Primitive")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    StructDeclaration(metadataName)
                        .WithAttributeLists(SingletonList(
                            AttributeList(SingletonSeparatedList(
                                    Attribute(NameOfSystemTextJsonSerializationJsonConverter)
                                        .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                                            AttributeArgument(GenerateTypeOfIdentityConverterGenericExpression(metadataName)))))))
                                .WithLeadingTrivia(trivia)))
                        .WithModifiers(InternalReadOnlyPartialTokenList)
                        .WithBaseList(BaseList(SeparatedList<BaseTypeSyntax>(
                        [
                            SimpleBaseType(TypeOfSystemIComparable),                                                                            // System.IComparable
                            SimpleBaseType(GenerateGenericType(NameOfSystem, "IComparable", typeName)),                                         // System.IComparable<T>
                            SimpleBaseType(GenerateGenericType(NameOfSystem, "IEquatable", typeName)),                                          // System.IEquatable<T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IEqualityOperators", typeName, typeName, BoolType)),      // System.Numerics.IEqualityOperators<T, T, bool>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IEqualityOperators", typeName, UIntType, BoolType)),      // System.Numerics.IEqualityOperators<T, uint, bool>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IAdditionOperators", typeName, typeName, typeName)),      // System.Numerics.IAdditionOperators<T, T, T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IAdditionOperators", typeName, UIntType, typeName)),      // System.Numerics.IAdditionOperators<T, uint, T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "ISubtractionOperators", typeName, typeName, typeName)),   // System.Numerics.ISubtractionOperators<T, T, T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "ISubtractionOperators", typeName, UIntType, typeName)),   // System.Numerics.ISubtractionOperators<T, uint, T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IIncrementOperators", typeName)),                         // System.Numerics.IIncrementOperators<T>
                            SimpleBaseType(GenerateGenericType(NameOfSystemNumerics, "IDecrementOperators", typeName))                          // System.Numerics.IDecrementOperators<T>
                        ])))
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            // public readonly uint Value;
                            FieldDeclaration(VariableDeclaration(UIntType)
                                    .WithVariables(SingletonSeparatedList(
                                        VariableDeclarator(Identifier("Value")))))
                                .WithModifiers(PublicReadOnlyTokenList),

                            // public T(uint value)
                            ConstructorDeclaration(typeToken)
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(UIntType, Identifier("value")))))
                                .WithBody(Block(SingletonList(
                                    ExpressionStatement(
                                        SimpleAssignmentExpression(
                                            IdentifierName("Value"),
                                            IdentifierName("value")))))),

                            // public static implicit operator uint(T value)
                            ImplicitConversionOperatorDeclaration(UIntType)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeName, Identifier("value")))))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(SimpleMemberAccessExpression(
                                        IdentifierName("value"),
                                        IdentifierName("Value")))))),

                            // public static implicit operator T(uint value)
                            ImplicitConversionOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(UIntType, Identifier("value")))))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(ImplicitObjectCreationExpression()
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName("value"))))))))),

                            // public override bool Equals(object? obj) -> System.Object
                            MethodDeclaration(BoolType, "Equals")
                                .WithModifiers(PublicOverrideTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(NullableObjectType, Identifier("obj")))))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(LogicalAndExpression(
                                        IsPatternExpression(
                                            IdentifierName("obj"),
                                            DeclarationPattern(typeName, SingleVariableDesignation(Identifier("other")))),
                                        InvocationExpression(IdentifierName("Equals"))
                                            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                                Argument(IdentifierName("other")))))))))),

                            // public override int GetHashCode() -> System.Object
                            MethodDeclaration(IntType, "GetHashCode")
                                .WithModifiers(PublicOverrideTokenList)
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                        IdentifierName("Value"),
                                        IdentifierName("GetHashCode"))))))),

                            // public override string ToString() -> System.Object
                            MethodDeclaration(StringType, "ToString")
                                .WithModifiers(PublicOverrideTokenList)
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                        IdentifierName("Value"),
                                        IdentifierName("ToString"))))))),

                            // public int CompareTo(object? obj) -> System.IComparable
                            MethodDeclaration(IntType, "CompareTo")
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(NullableObjectType, Identifier("obj")))))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    IfStatement(
                                        IsPatternExpression(IdentifierName("obj"), ConstantPattern(NullLiteralExpression)),
                                        Block(SingletonList(ReturnStatement(NumericLiteralExpression(1))))),
                                    IfStatement(
                                        IsPatternExpression(IdentifierName("obj"), UnaryPattern(DeclarationPattern(typeName, SingleVariableDesignation(Identifier("other"))))),
                                        Block(SingletonList(ThrowStatement(ObjectCreationExpression(TypeOfSystemArgumentException)
                                            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                                Argument(StringLiteralExpression($"Object must be of type {metadataName}."))))))))),
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                            IdentifierName("Value"),
                                            IdentifierName("CompareTo")))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(SimpleMemberAccessExpression(
                                                IdentifierName("other"),
                                                IdentifierName("Value"))))))),
                                ]))),

                            // public int CompareTo(T other) -> System.IComparable<T>
                            MethodDeclaration(IntType, "CompareTo")
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeName, Identifier("other")))))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                        IdentifierName("Value"),
                                        IdentifierName("CompareTo")))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(SimpleMemberAccessExpression(
                                                IdentifierName("other"),
                                                IdentifierName("Value")))))))))),

                            // public bool Equals(T other) -> System.IEquatable<T>
                            MethodDeclaration(BoolType, "Equals")
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeName, Identifier("other")))))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(EqualsExpression(
                                        IdentifierName("Value"),
                                        SimpleMemberAccessExpression(IdentifierName("other"), IdentifierName("Value"))))))),

                            // public static bool operator ==(T left, T right) -> System.Numerics.IEqualityOperators<T, T, bool>
                            EqualsEqualsOperatorDeclaration(BoolType)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(typeName, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(EqualsExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        SimpleMemberAccessExpression(IdentifierName("right"), IdentifierName("Value"))))))),

                            // public static bool operator !=(T left, T right) -> System.Numerics.IEqualityOperators<T, T, bool>
                            ExclamationEqualsOperatorDeclaration(BoolType)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(typeName, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(LogicalNotExpression(ParenthesizedExpression(EqualsExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        SimpleMemberAccessExpression(IdentifierName("right"), IdentifierName("Value"))))))))),

                            // public static bool operator ==(T left, uint right) -> System.Numerics.IEqualityOperators<T, uint, bool>
                            EqualsEqualsOperatorDeclaration(BoolType)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(UIntType, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(EqualsExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        IdentifierName("right")))))),

                            // public static bool operator !=(T left, uint right) -> System.Numerics.IEqualityOperators<T, uint, bool>
                            ExclamationEqualsOperatorDeclaration(BoolType)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(UIntType, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(LogicalNotExpression(ParenthesizedExpression(EqualsExpression(
                                        IdentifierName("left"),
                                        IdentifierName("right")))))))),

                            // public static T operator +(T left, T right) -> System.Numerics.IAdditionOperators<T, T, T>
                            PlusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(typeName, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(AddExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        SimpleMemberAccessExpression(IdentifierName("right"), IdentifierName("Value"))))))),

                            // public static T operator +(T left, uint right) -> System.Numerics.IAdditionOperators<T, uint, T>
                            PlusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(UIntType, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(AddExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        IdentifierName("right")))))),

                            // public static T operator -(T left, T right) -> System.Numerics.ISubtractionOperators<T, T, T>
                            MinusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(typeName, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(SubtractExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        SimpleMemberAccessExpression(IdentifierName("right"), IdentifierName("Value"))))))),

                            // public static T operator -(T left, uint right) -> System.Numerics.ISubtractionOperators<T, uint, T>
                            MinusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeName, Identifier("left")),
                                    Parameter(UIntType, Identifier("right"))
                                ])))
                                .WithBody(Block(SingletonList(
                                    ReturnStatement(SubtractExpression(
                                        SimpleMemberAccessExpression(IdentifierName("left"), IdentifierName("Value")),
                                        IdentifierName("right")))))),

                            // public static unsafe T operator ++(T value) -> System.Numerics.IIncrementOperators<T>
                            PlusPlusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticUnsafeTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeName, Identifier("value")))))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    ExpressionStatement(
                                        PreIncrementExpression(PointerIndirectionExpression(CastExpression(PointerType(UIntType), AddressOfExpression(IdentifierName("value")))))),
                                    ReturnStatement(IdentifierName("value"))
                                ]))),

                            // public static unsafe T operator --(T value) -> System.Numerics.IDecrementOperators<T>
                            MinusMinusOperatorDeclaration(typeName)
                                .WithModifiers(PublicStaticUnsafeTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeName, Identifier("value")))))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    ExpressionStatement(
                                        PreDecrementExpression(PointerIndirectionExpression(CastExpression(PointerType(UIntType), AddressOfExpression(IdentifierName("value")))))),
                                    ReturnStatement(IdentifierName("value"))
                                ]))),
                        ]))))))
            .NormalizeWhitespace();

        context.AddSource($"{metadataName}.g.cs", syntax.ToFullStringWithHeader());
    }

    private static TypeOfExpressionSyntax GenerateTypeOfIdentityConverterGenericExpression(string typeName)
    {
        return TypeOfExpression(QualifiedName(NameOfSnapHutaoModelPrimitiveConverter, GenericName("IdentityConverter")
            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(typeName))))));
    }

    private static TypeSyntax GenerateGenericType(NameSyntax left, string genericName, TypeSyntax type)
    {
        return QualifiedName(left, GenericName(genericName)
            .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type))));
    }

    private static TypeSyntax GenerateGenericType(NameSyntax left, string genericName, TypeSyntax type1, TypeSyntax type2, TypeSyntax type3)
    {
        return QualifiedName(left, GenericName(genericName)
            .WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>([type1, type2, type3]))));
    }

    private sealed record IdentityStructMetadata
    {
        public string? Name { get; set; }

        public string? Documentation { get; set; }
    }
}