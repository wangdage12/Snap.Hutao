// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Collections.Immutable;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class UnsafePropertyBackingFieldAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<FieldAccessorGeneratorContext> provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownMetadataNames.FieldAccessAttribute,
                SyntaxNodeHelper.Is<PropertyDeclarationSyntax>,
                FieldAccessorPropertyEntry.Create)
            .Where(static entry => entry is not null)
            .GroupBy(static entry => entry.Hierarchy)
            .Select(FieldAccessorGeneratorContext.Create);

        context.RegisterSourceOutput(provider, GenerateWrapper);
    }

    private static void GenerateWrapper(SourceProductionContext production, FieldAccessorGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, FieldAccessorGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
                GenerateAccessMethods(context, production.CancellationToken))
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private static ImmutableArray<MemberDeclarationSyntax> GenerateAccessMethods(FieldAccessorGeneratorContext context, CancellationToken token)
    {
        ImmutableArray<MemberDeclarationSyntax>.Builder accessMethodsBuilder = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>(context.Properties.Length);
        foreach (FieldAccessorPropertyEntry entry in context.Properties)
        {
            TypeSyntax propertyType = ParseTypeName(entry.Property.FullyQualifiedTypeNameWithNullabilityAnnotation);
            RefTypeSyntax refPropertyType = RefType(propertyType);
            if (entry.ReadOnly)
            {
                refPropertyType = refPropertyType.WithReadOnlyKeyword(ReadOnlyKeyword);
            }

            MethodDeclarationSyntax method = MethodDeclaration(refPropertyType, Identifier($"FieldRefOf{entry.Property.Name}"))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    GenerateUnsafeAccessorAttribute(entry.Field.MinimallyQualifiedName)))))
                .WithModifiers(PrivateStaticExternTokenList)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(ParseTypeName(entry.Hierarchy.Hierarchy[0].FullyQualifiedName), Identifier("self")))))
                .WithSemicolonToken(SemicolonToken);

            token.ThrowIfCancellationRequested();
            accessMethodsBuilder.Add(method);
        }

        token.ThrowIfCancellationRequested();
        return accessMethodsBuilder.ToImmutable();
    }

    private static AttributeSyntax GenerateUnsafeAccessorAttribute(string fieldName)
    {
        return Attribute(NameOfSystemRuntimeCompilerServicesUnsafeAccessor)
            .WithArgumentList(AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(
                [
                    AttributeArgument(SimpleMemberAccessExpression(
                        NameOfSystemRuntimeCompilerServicesUnsafeAccessorKind,
                        IdentifierName("Field"))),
                    AttributeArgument(StringLiteralExpression(fieldName)).WithNameEquals(NameEquals("Name")),
                ])));
    }

    private sealed record FieldAccessorPropertyEntry
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required PropertyInfo Property { get; init; }

        public required FieldInfo Field { get; init; }

        public required bool ReadOnly { get; init; }

        public static FieldAccessorPropertyEntry Create(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            if (context.TargetSymbol is not IPropertySymbol propertySymbol)
            {
                return default!;
            }

            if (propertySymbol.RefCustomModifiers.Length > 0 || (propertySymbol.GetMethod is null && propertySymbol.SetMethod is null))
            {
                return default!;
            }

            // { get; } or { get; init; } or { init; } => ref readonly
            // { get; set; } or { set; } => ref
            bool readOnly = propertySymbol.SetMethod is null || propertySymbol.SetMethod.IsInitOnly;

            IFieldSymbol? fieldSymbol = null;
            foreach (IFieldSymbol field in propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, propertySymbol))
                {
                    fieldSymbol = field;
                    break;
                }
            }

            if (fieldSymbol is null)
            {
                return default!;
            }

            return new()
            {
                Hierarchy = HierarchyInfo.Create(propertySymbol.ContainingType),
                Property = PropertyInfo.Create(propertySymbol),
                Field = FieldInfo.Create(fieldSymbol),
                ReadOnly = readOnly,
            };
        }
    }

    private sealed record FieldAccessorGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public required EquatableArray<FieldAccessorPropertyEntry> Properties { get; init; }

        public static FieldAccessorGeneratorContext Create((HierarchyInfo Hierarchy, EquatableArray<FieldAccessorPropertyEntry> Properties) tuple, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return new()
            {
                Hierarchy = tuple.Hierarchy,
                Properties = tuple.Properties,
            };
        }
    }
}