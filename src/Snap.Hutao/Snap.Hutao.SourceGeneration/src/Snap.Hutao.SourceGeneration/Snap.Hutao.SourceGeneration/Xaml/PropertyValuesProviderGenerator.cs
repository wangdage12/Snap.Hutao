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

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator(LanguageNames.CSharp)]
internal sealed class PropertyValuesProviderGenerator : IIncrementalGenerator
{
    public const string InterfaceMetadataName = "Snap.Hutao.UI.Xaml.Data.IPropertyValuesProvider";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<PropertyValuesProviderGeneratorContext> commands = context.SyntaxProvider
            .CreateSyntaxProvider(SyntaxNodeHelper.TypeHasBaseType, InheritedType)
            .Where(static c => c is not null);

        context.RegisterSourceOutput(commands, GenerateWrapper);
    }

    private static PropertyValuesProviderGeneratorContext InheritedType(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.Interfaces.Any(static symbol => symbol.HasFullyQualifiedMetadataName(InterfaceMetadataName)))
            {
                return PropertyValuesProviderGeneratorContext.Create(typeSymbol);
            }
        }

        return default!;
    }

    private static void GenerateWrapper(SourceProductionContext production, PropertyValuesProviderGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, PropertyValuesProviderGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
            [
                MethodDeclaration(NullableObjectType, Identifier("GetPropertyValue"))
                    .WithModifiers(PublicTokenList)
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(StringType, Identifier("propertyName")))))
                    .WithBody(Block(SingletonList(
                        ReturnStatement(SwitchExpression(IdentifierName("propertyName"))
                            .WithArms(SeparatedList(GenerateSwitchExpressionArms(context)))))))
            ])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static IEnumerable<SwitchExpressionArmSyntax> GenerateSwitchExpressionArms(PropertyValuesProviderGeneratorContext context)
    {
        foreach (PropertyInfo property in context.Properties)
        {
            IdentifierNameSyntax propertyName = IdentifierName(property.Name);

            // nameof(${propertyName}) => ${propertyName}
            yield return SwitchExpressionArm(
                ConstantPattern(NameOfExpression(propertyName)),
                propertyName);
        }

        // _ => default
        yield return SwitchExpressionArm(DiscardPattern(), DefaultLiteralExpression);
    }

    private sealed record PropertyValuesProviderGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<PropertyInfo> Properties { get; init; }

        public static PropertyValuesProviderGeneratorContext Create(INamedTypeSymbol typeSymbol)
        {
            return new()
            {
                Hierarchy = HierarchyInfo.Create(typeSymbol),
                Properties = typeSymbol
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(static prop => prop.DeclaredAccessibility is Accessibility.Public)
                    .Select(PropertyInfo.Create)
                    .ToImmutableArray(),
            };
        }
    }
}