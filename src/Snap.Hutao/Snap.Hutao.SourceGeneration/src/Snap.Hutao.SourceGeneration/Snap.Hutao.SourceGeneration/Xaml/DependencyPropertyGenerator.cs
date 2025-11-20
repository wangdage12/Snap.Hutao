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
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class DependencyPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<DependencyPropertyGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.DependencyPropertyAttributeT,
                SyntaxNodeHelper.Is<TypeDeclarationSyntax>,
                Transform)
            .Where(static c => c is not null);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static DependencyPropertyGeneratorContext Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return default!;
        }

        return DependencyPropertyGeneratorContext.Create(typeSymbol);
    }

    private static void GenerateWrapper(SourceProductionContext production, DependencyPropertyGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, DependencyPropertyGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy
            .GetCompilationUnit([.. GenerateMembers(context)])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateMembers(DependencyPropertyGeneratorContext context)
    {
        foreach (AttributeInfo attribute in context.Attributes)
        {
            if (!attribute.TryGetConstructorArgument(0, out string? name))
            {
                continue;
            }

            if (!attribute.TryGetTypeArgument(0, out TypeArgumentInfo? propertyType))
            {
                continue;
            }

            TypeSyntax propertyTypeSyntaxWithoutNullabilityAnnotation = propertyType.GetSyntax();
            TypeSyntax propertyTypeSyntax = propertyTypeSyntaxWithoutNullabilityAnnotation;

            if (!attribute.HasNamedArgument("NotNull", true))
            {
                propertyTypeSyntax = NullableType(propertyTypeSyntax);
            }

            // Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata)
            // RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata)
            SeparatedSyntaxList<ArgumentSyntax> registerArguments = SeparatedList(
            [
                Argument(StringLiteralExpression(name)),                                        // name
                Argument(TypeOfExpression(propertyTypeSyntaxWithoutNullabilityAnnotation)),     // propertyType
                Argument(TypeOfExpression(IdentifierName(context.Hierarchy.Hierarchy[0].Name))) // ownerType
            ]);

            // PropertyMetadata.Create(object defaultValue)
            // PropertyMetadata.Create(object defaultValue, PropertyChangedCallback propertyChangedCallback)
            // These two are not supposed to be used because they can be called multiple times and cause issues.
            // PropertyMetadata.Create(CreateDefaultValueCallback createDefaultValueCallback)
            // PropertyMetadata.Create(CreateDefaultValueCallback createDefaultValueCallback, PropertyChangedCallback propertyChangedCallback)
            SeparatedSyntaxList<ArgumentSyntax> createArguments = SeparatedList<ArgumentSyntax>();
            if (attribute.TryGetNamedArgument("CreateDefaultValueCallbackName", out string? createDefaultValueCallbackName))
            {
                createArguments = createArguments.Add(Argument(InvocationExpression(IdentifierName(createDefaultValueCallbackName)).WithEmptyArgumentList()));
            }
            else
            {
                bool hasDefaultValue = attribute.TryGetNamedArgument("DefaultValue", out TypedConstantInfo? defaultValue);
                createArguments = createArguments.Add(hasDefaultValue
                    ? Argument(defaultValue!.GetSyntax())
                    : Argument(DefaultExpression(ObjectType)));
            }

            if (attribute.TryGetNamedArgument("PropertyChangedCallbackName", out string? propertyChangedCallbackName))
            {
                createArguments = createArguments.Add(Argument(IdentifierName(propertyChangedCallbackName)));
            }

            registerArguments = registerArguments.Add(Argument(InvocationExpression(
                    SimpleMemberAccessExpression(
                        SimpleMemberAccessExpression(
                            NameOfMicrosoftUIXaml,
                            IdentifierName("PropertyMetadata")),
                        IdentifierName("Create")))
                .WithArgumentList(ArgumentList(createArguments))));

            bool isAttached = attribute.HasNamedArgument("IsAttached", true);

            TypeSyntax dependencyPropertyType = QualifiedName(NameOfMicrosoftUIXaml, IdentifierName("DependencyProperty"));
            string propertyName = $"{name}Property";

            yield return FieldDeclaration(VariableDeclaration(dependencyPropertyType)
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(Identifier(propertyName))
                        .WithInitializer(EqualsValueClause(
                            InvocationExpression(SimpleMemberAccessExpression(
                                    SimpleMemberAccessExpression(
                                        NameOfMicrosoftUIXaml,
                                        IdentifierName("DependencyProperty")),
                                    IdentifierName(isAttached ? "RegisterAttached" : "Register")))
                                .WithArgumentList(ArgumentList(registerArguments)))))))
                .WithModifiers(PrivateStaticReadonlyTokenList);

            if (!isAttached)
            {
                // Generate a property for non-attached properties
                yield return PropertyDeclaration(propertyTypeSyntax, Identifier(name))
                    .WithModifiers(PublicTokenList)
                    .WithIdentifier(Identifier(name))
                    .WithAccessorList(AccessorList(List(
                    [
                        GetAccessorDeclaration().WithExpressionBody(ArrowExpressionClause(CastExpression(
                            propertyTypeSyntaxWithoutNullabilityAnnotation,
                            InvocationExpression(IdentifierName("GetValue"))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName(propertyName)))))))),
                        SetAccessorDeclaration().WithExpressionBody(ArrowExpressionClause(
                            InvocationExpression(IdentifierName("SetValue"))
                                .WithArgumentList(ArgumentList(SeparatedList(
                                [
                                    Argument(IdentifierName(propertyName)),
                                    Argument(IdentifierName("value"))
                                ])))))
                    ])));
            }
            else
            {
                // Generate static methods for attached properties
                TypeSyntax targetTypeSyntax;
                if (attribute.TryGetNamedArgument("TargetType", out TypedConstantInfo? targetType) &&
                    targetType is TypedConstantInfo.Type type)
                {
                    targetTypeSyntax = ParseName(type.FullyQualifiedTypeName);
                }
                else
                {
                    targetTypeSyntax = QualifiedName(NameOfMicrosoftUIXaml, IdentifierName("DependencyObject"));
                }

                // Generate static methods for attached properties
                yield return MethodDeclaration(propertyTypeSyntax, Identifier($"Get{name}"))
                    .WithModifiers(PublicStaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(targetTypeSyntax, Identifier("obj"))
                    ])))
                    .WithBody(Block(List<StatementSyntax>(
                    [
                        ExpressionStatement(ArgumentNullExceptionThrowIfNull(IdentifierName("obj"))),

                        ReturnStatement(CastExpression(
                            propertyTypeSyntax,
                            InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        IdentifierName("obj"),
                                        IdentifierName("GetValue")))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName(propertyName)))))))
                    ])));

                yield return MethodDeclaration(VoidType, Identifier($"Set{name}"))
                    .WithModifiers(PublicStaticTokenList)
                    .WithParameterList(ParameterList(SeparatedList(
                    [
                        Parameter(targetTypeSyntax, Identifier("obj")),
                        Parameter(propertyTypeSyntax, Identifier("value"))
                    ])))
                    .WithBody(Block(List<StatementSyntax>(
                    [
                        ExpressionStatement(ArgumentNullExceptionThrowIfNull(IdentifierName("obj"))),

                        ExpressionStatement(
                            InvocationExpression(
                                    SimpleMemberAccessExpression(
                                        IdentifierName("obj"),
                                        IdentifierName("SetValue")))
                                .WithArgumentList(ArgumentList(SeparatedList(
                                [
                                    Argument(IdentifierName(propertyName)),
                                    Argument(IdentifierName("value"))
                                ]))))
                    ])));
            }
        }
    }

    private sealed record DependencyPropertyGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<AttributeInfo> Attributes { get; init; }

        public static DependencyPropertyGeneratorContext Create(INamedTypeSymbol typeSymbol)
        {
            return new()
            {
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Attributes = ImmutableArray.CreateRange(typeSymbol.GetAttributes(), AttributeInfo.Create)
            };
        }
    }
}