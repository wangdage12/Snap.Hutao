// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Enum;

[Generator(LanguageNames.CSharp)]
internal class ExtendedEnumGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ExtendedEnumGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.ExtendedEnumAttribute,
                SyntaxNodeHelper.Is<EnumDeclarationSyntax>,
                ExtendedEnumGeneratorContext.Create)
            .Where(static c => c is not null);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ExtendedEnumGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception e)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", e.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, ExtendedEnumGeneratorContext context)
    {
        TypeSyntax enumType = context.Type.GetTypeSyntax();

        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("System.Globalization")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Resource.Localization")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration($"{context.Type.Name}Extension")
                        .WithModifiers(InternalStaticPartialTokenList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            // public static string? GetName(this T value)
                            MethodDeclaration(NullableStringType, "GetName")
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisTokenList))))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(SwitchExpression(IdentifierName("value"))
                                        .WithArms(SeparatedList(GenerateGetNameSwitchArms(enumType, context.Fields))))))),

                            // public static string? GetLocalizedDescriptionOrDefault(this T value, ResourceManager resourceManager, CultureInfo cultureInfo)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescriptionOrDefault")
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisTokenList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager")),
                                    Parameter(TypeOfSystemGlobalizationCultureInfo, Identifier("cultureInfo"))
                                ])))
                                .WithBody(Block(List<StatementSyntax>(
                                [
                                    LocalDeclarationStatement(VariableDeclaration(StringType)
                                        .WithVariables(SingletonSeparatedList(
                                            VariableDeclarator(Identifier("key"))
                                                .WithInitializer(EqualsValueClause(SwitchExpression(IdentifierName("value"))
                                                    .WithArms(SeparatedList(GenerateGetLocalizedDescriptionOrDefaultSwitchArms(enumType, context.Fields)))))))),
                                    ReturnStatement(InvocationExpression(SimpleMemberAccessExpression(
                                            IdentifierName("resourceManager"),
                                            IdentifierName("GetString")))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("key")),
                                            Argument(IdentifierName("cultureInfo"))
                                        ]))))
                                ]))),

                            // public static string? GetLocalizedDescriptionOrDefault(this T value, ResourceManager resourceManager)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescriptionOrDefault")
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisTokenList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(InvocationExpression(IdentifierName("GetLocalizedDescriptionOrDefault"))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("value")),
                                            Argument(IdentifierName("resourceManager")),
                                            Argument(SimpleMemberAccessExpression(TypeOfSystemGlobalizationCultureInfo, IdentifierName("CurrentCulture")))
                                        ]))))))),

                            // public static string GetLocalizedDescription(this T value, ResourceManager resourceManager, CultureInfo cultureInfo)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescription")
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisTokenList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager")),
                                    Parameter(TypeOfSystemGlobalizationCultureInfo, Identifier("cultureInfo"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(CoalesceExpression(
                                        InvocationExpression(IdentifierName("GetLocalizedDescriptionOrDefault"))
                                            .WithArgumentList(ArgumentList(SeparatedList(
                                            [
                                                Argument(IdentifierName("value")),
                                                Argument(IdentifierName("resourceManager")),
                                                Argument(IdentifierName("cultureInfo"))
                                            ]))),
                                        CoalesceExpression(
                                            InvocationExpression(IdentifierName("GetName"))
                                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                                    Argument(IdentifierName("value"))))),
                                            SimpleMemberAccessExpression(StringType, IdentifierName("Empty")))))))),

                            // public static string GetLocalizedDescription(this T value, ResourceManager resourceManager)
                            MethodDeclaration(NullableStringType, "GetLocalizedDescription")
                                .WithModifiers(PublicStaticTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(enumType, Identifier("value")).WithModifiers(ThisTokenList),
                                    Parameter(TypeOfSystemResourcesResourceManager, Identifier("resourceManager"))
                                ])))
                                .WithBody(Block(SingletonList<StatementSyntax>(
                                    ReturnStatement(InvocationExpression(IdentifierName("GetLocalizedDescription"))
                                        .WithArgumentList(ArgumentList(SeparatedList(
                                        [
                                            Argument(IdentifierName("value")),
                                            Argument(IdentifierName("resourceManager")),
                                            Argument(SimpleMemberAccessExpression(TypeOfSystemGlobalizationCultureInfo, IdentifierName("CurrentCulture")))
                                        ])))))))
                        ]))))))
            .NormalizeWhitespace();

        production.AddSource(context.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetNameSwitchArms(TypeSyntax enumType, ImmutableArray<(FieldInfo Field, AttributeInfo? Attribute)> fields)
    {
        foreach ((FieldInfo field, _) in fields)
        {
            yield return SwitchExpressionArm(
                ConstantPattern(SimpleMemberAccessExpression(enumType, IdentifierName(field.MinimallyQualifiedName))),
                StringLiteralExpression(field.MinimallyQualifiedName));
        }

        yield return SwitchExpressionArm(
            DiscardPattern(),
            InvocationExpression(SimpleMemberAccessExpression(
                    TypeOfSystemEnum,
                    IdentifierName("GetName")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("value"))))));
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateGetLocalizedDescriptionOrDefaultSwitchArms(TypeSyntax enumType, ImmutableArray<(FieldInfo Field, AttributeInfo? Attribute)> fields)
    {
        foreach ((FieldInfo field, AttributeInfo? localizationKeyInfo) in fields)
        {
            if (localizationKeyInfo is not null && localizationKeyInfo.TryGetConstructorArgument(0, out string? localizationKey))
            {
                yield return SwitchExpressionArm(
                    ConstantPattern(SimpleMemberAccessExpression(enumType, IdentifierName(field.MinimallyQualifiedName))),
                    StringLiteralExpression(localizationKey));
            }
        }

        yield return SwitchExpressionArm(
            DiscardPattern(),
            SimpleMemberAccessExpression(StringType, IdentifierName("Empty")));
    }

    private sealed record ExtendedEnumGeneratorContext
    {
        public required string FileNameHint { get; init; }

        public required Model.TypeInfo Type { get; init; }

        public required EquatableArray<(FieldInfo Field, AttributeInfo? Attribute)> Fields { get; init; }

        public static ExtendedEnumGeneratorContext Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol symbol)
            {
                return default!;
            }

            return new()
            {
                FileNameHint = symbol.GetFullyQualifiedMetadataName(),
                Type = Model.TypeInfo.Create(symbol),
                Fields = symbol
                    .GetMembers()
                    .OfType<IFieldSymbol>()
                    .Select(static field => (FieldInfo.Create(field), AttributeInfo.CreateOrDefault(field.GetAttributes().SingleOrDefault(static data => data.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.LocalizationKeyAttribute) is true))))
                    .ToImmutableArray(),
            };
        }
    }
}