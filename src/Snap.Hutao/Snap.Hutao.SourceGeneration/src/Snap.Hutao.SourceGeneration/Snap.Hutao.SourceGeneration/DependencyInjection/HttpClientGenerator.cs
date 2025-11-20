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
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;
using TypeInfo = Snap.Hutao.SourceGeneration.Model.TypeInfo;

namespace Snap.Hutao.SourceGeneration.DependencyInjection;

[Generator(LanguageNames.CSharp)]
internal sealed class HttpClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<HttpClientGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.HttpClientAttribute,
                SyntaxNodeHelper.Is<ClassDeclarationSyntax>,
                HttpClientEntry.Create)
            .Where(static entry => entry is not null)
            .Collect()
            .Select(HttpClientGeneratorContext.Create);

        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, HttpClientGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, HttpClientGeneratorContext context)
    {
        CompilationUnitSyntax syntax = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective("System.Net.Http")))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("ServiceCollectionExtension")
                        .WithModifiers(InternalStaticPartialTokenList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            MethodDeclaration(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection,"AddHttpClients")
                                .WithModifiers(PublicStaticPartialTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection, Identifier("services"))
                                        .WithModifiers(ThisTokenList))))
                                .WithBody(Block(List(GenerateAddHttpClients(context))))))))))
            .NormalizeWhitespace();

        production.AddSource("ServiceCollectionExtension.g.cs", syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<StatementSyntax> GenerateAddHttpClients(HttpClientGeneratorContext context)
    {
        foreach (HttpClientEntry entry in context.HttpClients)
        {
            TypeSyntax targetType = ParseTypeName(entry.Type.FullyQualifiedName);
            SeparatedSyntaxList<TypeSyntax> typeArguments = SingletonSeparatedList(targetType);
            if (entry.Attribute.TryGetConstructorArgument(1, out TypedConstantInfo? info) &&
                info is TypedConstantInfo.Type typeInfo) // [HttpClient(config, typeof(T))]
            {
                typeArguments = typeArguments.Insert(0, ParseTypeName(typeInfo.FullyQualifiedTypeName));
            }

            InvocationExpressionSyntax invocation = InvocationExpression(
                    SimpleMemberAccessExpression(
                        IdentifierName("services"),
                        GenericName(Identifier("AddHttpClient"))
                            .WithTypeArgumentList(TypeArgumentList(typeArguments))))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(entry.ConfigurationName)))));

            if (entry.PrimaryHttpMessageHandler is { NamedArguments.IsEmpty: false })
            {
                invocation = InvocationExpression(SimpleMemberAccessExpression(
                        invocation,
                        IdentifierName("ConfigurePrimaryHttpMessageHandler")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(ParenthesizedLambdaExpression()
                            .WithParameterList(ParameterList(SeparatedList(
                            [
                                Parameter(Identifier("handler")),
                                Parameter(Identifier("serviceProvider"))
                            ])))
                            .WithBlock(Block(List(
                                GenerateConfigurePrimaryHttpMessageHandlerStatements(entry.PrimaryHttpMessageHandler.NamedArguments))))))));
            }

            yield return ExpressionStatement(invocation);
        }

        yield return ReturnStatement(IdentifierName("services"));
    }

    private static IEnumerable<StatementSyntax> GenerateConfigurePrimaryHttpMessageHandlerStatements(ImmutableArray<(string, TypedConstantInfo)> namedArguments)
    {
        yield return LocalDeclarationStatement(VariableDeclaration(TypeOfSystemNetHttpSocketsHttpHandler)
            .WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier("typedHandler"))
                    .WithInitializer(EqualsValueClause(CastExpression(TypeOfSystemNetHttpSocketsHttpHandler, IdentifierName("handler")))))));

        foreach ((string name, TypedConstantInfo typedConstant) in namedArguments)
        {
            yield return ExpressionStatement(SimpleAssignmentExpression(
                SimpleMemberAccessExpression(IdentifierName("typedHandler"), IdentifierName(name)),
                typedConstant.GetSyntax()));
        }
    }

    private sealed record HttpClientEntry : IComparable<HttpClientEntry?>
    {
        public required AttributeInfo Attribute { get; init; }

        public required AttributeInfo? PrimaryHttpMessageHandler { get; init; }

        public required string ConfigurationName { get; init; }

        public required TypeInfo Type { get; init; }

        public static HttpClientEntry Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol || context.Attributes.SingleOrDefault() is not { } httpClient)
            {
                return default!;
            }

            AttributeInfo? primaryHttpMessageHandler = null;
            foreach (AttributeData attribute in typeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.HasFullyQualifiedMetadataName(WellKnownMetadataNames.PrimaryHttpMessageHandlerAttribute) is true)
                {
                    primaryHttpMessageHandler = AttributeInfo.Create(attribute);
                }
            }

            return new()
            {
                Attribute = AttributeInfo.Create(httpClient),
                PrimaryHttpMessageHandler = primaryHttpMessageHandler,
                ConfigurationName = $"{httpClient.ConstructorArguments[0].ToCSharpString()[WellKnownMetadataNames.HttpClientConfiguration.Length..]}Configuration",
                Type = TypeInfo.Create(typeSymbol),
            };
        }

        public int CompareTo(HttpClientEntry? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            return string.Compare(Type.FullyQualifiedName, other.Type.FullyQualifiedName, StringComparison.Ordinal);
        }
    }

    private sealed record HttpClientGeneratorContext
    {
        public required EquatableArray<HttpClientEntry> HttpClients { get; init; }

        public static HttpClientGeneratorContext Create(ImmutableArray<HttpClientEntry> services, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return new()
            {
                HttpClients = services.Sort(),
            };
        }
    }
}