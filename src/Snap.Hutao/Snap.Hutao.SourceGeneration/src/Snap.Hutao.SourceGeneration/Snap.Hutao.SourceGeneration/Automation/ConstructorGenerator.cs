// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;
using TypeInfo = Snap.Hutao.SourceGeneration.Model.TypeInfo;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ConstructorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ConstructorGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.GeneratedConstructorAttribute,
                SyntaxNodeHelper.Is<BaseMethodDeclarationSyntax>,
                ConstructorGeneratorContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, ConstructorGeneratorContext context)
    {
        try
        {
            Generate(production, context);
        }
        catch (Exception ex)
        {
            production.AddSource($"Error-{Guid.NewGuid().ToString()}.g.cs", ex.ToString());
        }
    }

    private static void Generate(SourceProductionContext production, ConstructorGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
        [
            GenerateConstructorDeclaration(context)
                .WithParameterList(GenerateConstructorParameterList(context))
                .WithBody(Block(List(GenerateConstructorBodyStatements(context, production.CancellationToken)))),

            // Property declarations
            .. GeneratePropertyDeclarations(context, production.CancellationToken),

            // PreConstruct & PostConstruct Method declarations
            MethodDeclaration(VoidType, Identifier("PreConstruct"))
                .WithModifiers(PartialTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                .WithSemicolonToken(SemicolonToken),
            MethodDeclaration(VoidType, Identifier("PostConstruct"))
                .WithModifiers(PartialTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(TypeOfSystemIServiceProvider, Identifier("serviceProvider")))))
                .WithSemicolonToken(SemicolonToken)
        ]).NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static ConstructorDeclarationSyntax GenerateConstructorDeclaration(ConstructorGeneratorContext context)
    {
        ConstructorDeclarationSyntax constructorDeclaration = ConstructorDeclaration(Identifier(context.Hierarchy.Hierarchy[0].Name))
            .WithModifiers(context.DeclaredAccessibility.ToSyntaxTokenList(PartialKeyword));

        if (context.Attribute.HasNamedArgument("CallBaseConstructor", true))
        {
            string serviceProvider = context.Parameters.Single(static p => p.FullyQualifiedTypeMetadataName is WellKnownMetadataNames.IServiceProvider).Name;

            constructorDeclaration = constructorDeclaration.WithInitializer(
                BaseConstructorInitializer(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(serviceProvider))))));
        }

        return constructorDeclaration;
    }

    private static ParameterListSyntax GenerateConstructorParameterList(ConstructorGeneratorContext context)
    {
        return ParameterList(SeparatedList(context.Parameters.Select(static p => p.GetSyntax())));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyStatements(ConstructorGeneratorContext context, CancellationToken token)
    {
        string serviceProvider = context.Parameters.Single(static p => p.FullyQualifiedTypeMetadataName is WellKnownMetadataNames.IServiceProvider).Name;

        // Call PreConstruct
        token.ThrowIfCancellationRequested();
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PreConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName(serviceProvider))))));

        // Assign fields
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyFieldAssignments(serviceProvider, context, token))
        {
            token.ThrowIfCancellationRequested();
            yield return statementSyntax;
        }

        // Assign properties
        foreach (StatementSyntax? statementSyntax in GenerateConstructorBodyPropertyAssignments(serviceProvider, context, token))
        {
            token.ThrowIfCancellationRequested();
            yield return statementSyntax;
        }

        token.ThrowIfCancellationRequested();

        // Call Register for IRecipient interfaces
        foreach (TypeInfo recipientInterface in context.Interfaces)
        {
            string messageTypeString = recipientInterface.TypeArguments.Single().FullyQualifiedTypeNameWithNullabilityAnnotations;
            TypeSyntax messageType = ParseTypeName(messageTypeString);

            token.ThrowIfCancellationRequested();
            yield return ExpressionStatement(InvocationExpression(SimpleMemberAccessExpression(
                    TypeOfCommunityToolkitMvvmMessagingIMessengerExtensions,
                    GenericName(Identifier("Register"))
                        .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(messageType)))))
                .WithArgumentList(ArgumentList(SeparatedList(
                [
                    Argument(ServiceProviderGetRequiredService(IdentifierName(serviceProvider), TypeOfCommunityToolkitMvvmMessagingIMessenger)),
                    Argument(ThisExpression())
                ]))));
        }

        // Call InitializeComponent if specified
        if (context.Attribute.HasNamedArgument("InitializeComponent", true))
        {
            token.ThrowIfCancellationRequested();
            yield return ExpressionStatement(InvocationExpression(IdentifierName("InitializeComponent")));
        }

        // Call PostConstruct
        token.ThrowIfCancellationRequested();
        yield return ExpressionStatement(InvocationExpression(IdentifierName("PostConstruct"))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(IdentifierName(serviceProvider))))));
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyFieldAssignments(string serviceProviderName, ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach ((bool shouldSkip, FieldInfo fieldInfo) in context.Fields)
        {
            if (shouldSkip)
            {
                yield return EmptyStatement().WithTrailingTrivia(Comment($"// Skipped field with initializer: {fieldInfo.MinimallyQualifiedName}"));
                continue;
            }

            fieldInfo.TryGetAttributeWithFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute, out AttributeInfo? fromKeyed);
            yield return GenerateConstructorBodyMemberAssignment(
                fieldInfo.FullyQualifiedTypeNameWithNullabilityAnnotation,
                fieldInfo.MinimallyQualifiedName,
                serviceProviderName,
                context,
                fromKeyed,
                token);
        }
    }

    private static IEnumerable<StatementSyntax> GenerateConstructorBodyPropertyAssignments(string serviceProviderName, ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach (PropertyInfo propertyInfo in context.Properties)
        {
            propertyInfo.TryGetAttributeWithFullyQualifiedMetadataName(WellKnownMetadataNames.FromKeyedServicesAttribute, out AttributeInfo? fromKeyed);
            yield return GenerateConstructorBodyMemberAssignment(
                propertyInfo.FullyQualifiedTypeNameWithNullabilityAnnotation,
                propertyInfo.Name,
                serviceProviderName,
                context,
                fromKeyed,
                token);
        }
    }

    private static StatementSyntax GenerateConstructorBodyMemberAssignment(
        string fullyQualifiedMemberTypeName,
        string memberName,
        string serviceProviderName,
        ConstructorGeneratorContext context,
        AttributeInfo? fromKeyed,
        CancellationToken token)
    {
        TypeSyntax propertyType = ParseTypeName(fullyQualifiedMemberTypeName);
        MemberAccessExpressionSyntax memberAccess = SimpleMemberAccessExpression(ThisExpression(), IdentifierName(memberName));
        token.ThrowIfCancellationRequested();
        return fullyQualifiedMemberTypeName switch
        {
            "global::System.Net.Http.HttpClient" => context.Parameters.SingleOrDefault(static p => p.FullyQualifiedTypeMetadataName is WellKnownMetadataNames.HttpClient) is { } httpClient
                ? ExpressionStatement(SimpleAssignmentExpression(memberAccess, IdentifierName(httpClient.Name)))
                : ExpressionStatement(SimpleAssignmentExpression(memberAccess,
                    InvocationExpression(SimpleMemberAccessExpression(
                            ServiceProviderGetRequiredService(IdentifierName(serviceProviderName), TypeOfSystemNetHttpIHttpClientFactory),
                            IdentifierName("CreateClient")))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                            Argument(NameOfExpression(IdentifierName(context.Hierarchy.Hierarchy[0].Name)))))))),
            _ => context.Parameters.SingleOrDefault(p => p.FullyQualifiedTypeName == fullyQualifiedMemberTypeName) is { } parameter
                ? ExpressionStatement(SimpleAssignmentExpression(memberAccess, IdentifierName(parameter.Name)))
                : fromKeyed is not null
                    ? ExpressionStatement(SimpleAssignmentExpression(
                        memberAccess,
                        ServiceProviderGetRequiredKeyedService(IdentifierName(serviceProviderName), propertyType, fromKeyed.ConstructorArguments.Single().GetSyntax())))
                    : ExpressionStatement(SimpleAssignmentExpression(
                        memberAccess,
                        ServiceProviderGetRequiredService(IdentifierName(serviceProviderName), propertyType)))
        };
    }

    private static IEnumerable<PropertyDeclarationSyntax> GeneratePropertyDeclarations(ConstructorGeneratorContext context, CancellationToken token)
    {
        foreach (PropertyInfo propertyInfo in context.Properties)
        {
            TypeSyntax propertyType = ParseTypeName(propertyInfo.FullyQualifiedTypeNameWithNullabilityAnnotation);
            token.ThrowIfCancellationRequested();
            yield return PropertyDeclaration(propertyType, Identifier(propertyInfo.Name))
                .WithModifiers(propertyInfo.DeclaredAccessibility.ToSyntaxTokenList(PartialKeyword))
                .WithAccessorList(AccessorList(SingletonList(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(FieldExpression()))
                        .WithSemicolonToken(SemicolonToken))));
        }
    }

    private sealed record ConstructorGeneratorContext
    {
        public required AttributeInfo Attribute { get; init; }

        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<ParameterInfo> Parameters { get; init; }

        public required EquatableArray<(bool ShouldSkip, FieldInfo Field)> Fields { get; init; }

        public required EquatableArray<PropertyInfo> Properties { get; init; }

        public required EquatableArray<TypeInfo> Interfaces { get; init; }

        public required Accessibility DeclaredAccessibility { get; init; }

        public static ConstructorGeneratorContext Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not IMethodSymbol { ContainingType: { } typeSymbol } constructorSymbol)
            {
                return default!;
            }

            ImmutableArray<(bool ShouldSkip, FieldInfo Field)>.Builder fieldsBuilder = ImmutableArray.CreateBuilder<(bool ShouldSkip, FieldInfo Field)>();
            ImmutableArray<PropertyInfo>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<PropertyInfo>();

            foreach (ISymbol member in typeSymbol.GetMembers())
            {
                switch (member)
                {
                    case IFieldSymbol fieldSymbol:
                        {
                            if (fieldSymbol.IsImplicitlyDeclared || fieldSymbol.HasConstantValue || fieldSymbol.IsStatic || !fieldSymbol.IsReadOnly)
                            {
                                continue;
                            }

                            bool shouldSkip = false;
                            foreach (SyntaxReference syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
                            {
                                if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null })
                                {
                                    // Skip field with initializer
                                    shouldSkip = true;
                                    break;
                                }
                            }

                            fieldsBuilder.Add((shouldSkip, FieldInfo.Create(fieldSymbol)));
                            break;
                        }
                    case IPropertySymbol propertySymbol:
                        {
                            if (propertySymbol.IsStatic || propertySymbol.IsImplicitlyDeclared || !propertySymbol.IsPartialDefinition || !propertySymbol.IsReadOnly)
                            {
                                continue;
                            }

                            propertiesBuilder.Add(PropertyInfo.Create(propertySymbol));
                            break;
                        }
                }
            }

            ImmutableArray<TypeInfo>.Builder interfacesBuilder = ImmutableArray.CreateBuilder<TypeInfo>();
            foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.Interfaces)
            {
                if (!interfaceSymbol.HasFullyQualifiedMetadataName("CommunityToolkit.Mvvm.Messaging.IRecipient`1"))
                {
                    continue;
                }

                interfacesBuilder.Add(TypeInfo.Create(interfaceSymbol));
            }

            return new()
            {
                Attribute = AttributeInfo.Create(context.Attributes.Single()),
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Parameters = ImmutableArray.CreateRange(constructorSymbol.Parameters, ParameterInfo.Create),
                Fields = fieldsBuilder.ToImmutable(),
                Properties = propertiesBuilder.ToImmutable(),
                Interfaces = interfacesBuilder.ToImmutable(),
                DeclaredAccessibility = constructorSymbol.DeclaredAccessibility,
            };
        }
    }
}