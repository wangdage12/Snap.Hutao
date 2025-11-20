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
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class CommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<CommandGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.CommandAttribute,
                SyntaxNodeHelper.Is<MethodDeclarationSyntax>,
                Transform)
            .GroupBy(t => t.Left, t => AttributedMethodInfo.Create(t.Right))
            .Select(CommandGeneratorContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static (HierarchyInfo Hierarchy, (EquatableArray<AttributeInfo> Attribute, MethodInfo Method)) Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is IMethodSymbol { ContainingType: { } typeSymbol } methodSymbol)
        {
            return (HierarchyInfo.Create(typeSymbol), (ImmutableArray.CreateRange(context.Attributes, AttributeInfo.Create),  MethodInfo.Create(methodSymbol)));
        }

        return default;
    }

    private static void GenerateWrapper(SourceProductionContext production, CommandGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, CommandGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit([.. GenerateCommandProperties(context.Methods)])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateCommandProperties(EquatableArray<AttributedMethodInfo> methods)
    {
        foreach (AttributedMethodInfo attributedMethod in methods)
        {
            // bool isAsync = attributedMethod.Method.FullyQualifiedReturnTypeMetadataName.StartsWith("System.Threading.Tasks.Task");
            // SyntaxToken identifier = Identifier(isAsync ? "AsyncRelayCommand" : "RelayCommand");
            //
            // TypeSyntax propertyType;
            // ImmutableArray<ParameterInfo> parameters = attributedMethod.Method.Parameters;
            // if (parameters.Length >= 1)
            // {
            //     TypeSyntax type = ParseTypeName(parameters[0].FullyQualifiedTypeName);
            //     propertyType = QualifiedName(NameOfCommunityToolkitMvvmInput, GenericName(identifier).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type))));
            // }
            // else
            // {
            //     propertyType = QualifiedName(NameOfCommunityToolkitMvvmInput, IdentifierName(identifier));
            // }

            foreach (AttributeInfo attribute in attributedMethod.Attributes)
            {
                if (attribute.FullyQualifiedMetadataName is not WellKnownMetadataNames.CommandAttribute)
                {
                    continue;
                }

                if (!attribute.TryGetConstructorArgument(0, out string? commandName))
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments = SingletonSeparatedList(
                    Argument(IdentifierName(attributedMethod.Method.Name)));

                if (attribute.HasNamedArgument("AllowConcurrentExecutions", true))
                {
                    arguments = arguments.Add(Argument(SimpleMemberAccessExpression(
                        NameOfCommunityToolkitMvvmInputAsyncRelayCommandOptions,
                        IdentifierName("AllowConcurrentExecutions"))));
                }

                yield return PropertyDeclaration(CommandHelper.GetCommandType(attributedMethod), commandName)
                    .WithAttributeLists(SingletonList(
                        AttributeList(SingletonSeparatedList(
                            Attribute(NameOfSystemDiagnosticsCodeAnalysisMaybeNull)))
                            .WithTarget(AttributeTargetSpecifier(FieldKeyword))))
                    .WithModifiers(attributedMethod.Method.IsStatic ? PublicStaticTokenList: PublicTokenList)
                    .WithAccessorList(AccessorList(SingletonList(
                        GetAccessorDeclaration()
                            .WithExpressionBody(ArrowExpressionClause(CoalesceAssignmentExpression(
                                FieldExpression(),
                                ImplicitObjectCreationExpression()
                                    .WithArgumentList(ArgumentList(arguments))))))));
            }
        }
    }

    private sealed record CommandGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<AttributedMethodInfo> Methods { get; init; }

        public static CommandGeneratorContext Create((HierarchyInfo Hierarchy, EquatableArray<AttributedMethodInfo> Methods) tuple, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new()
            {
                Hierarchy = tuple.Hierarchy,
                Methods = tuple.Methods,
            };
        }
    }
}