using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration.Xaml;

[Generator]
internal class XamlUnloadObjectOverrideGenerator : IIncrementalGenerator
{
    private const string ClassMetadataName = "Snap.Hutao.UI.Xaml.Control.ScopedPage";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<XamlUnloadObjectOverrideGeneratorContext> inheritClasses = context.SyntaxProvider
            .CreateSyntaxProvider(SyntaxNodeHelper.TypeHasBaseType, InheritedType)
            .Where(static c => c is not null)
            .Distinct();

        context.RegisterSourceOutput(inheritClasses, GenerateWrapper);
    }

    private static XamlUnloadObjectOverrideGeneratorContext InheritedType(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.BaseType?.HasFullyQualifiedMetadataName(ClassMetadataName) is true)
            {
                return XamlUnloadObjectOverrideGeneratorContext.Create(typeSymbol);
            }
        }

        return default!;
    }

    private static void GenerateWrapper(SourceProductionContext production, XamlUnloadObjectOverrideGeneratorContext context)
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

    private static void Generate(SourceProductionContext production, XamlUnloadObjectOverrideGeneratorContext context)
    {
        CompilationUnitSyntax syntax = context.Hierarchy.GetCompilationUnit(
            [
                MethodDeclaration(VoidType, Identifier("UnloadObjectOverride"))
                    .WithModifiers(PublicOverrideTokenList)
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(ParseTypeName("global::Microsoft.UI.Xaml.DependencyObject"), Identifier("unloadableObject")))))
                    .WithBody(Block(SingletonList<StatementSyntax>(
                        ExpressionStatement(
                            InvocationExpression(IdentifierName("UnloadObject"))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName("unloadableObject"))))))))),
            ])
            .NormalizeWhitespace();

        production.AddSource(context.Hierarchy.FileNameHint, syntax.ToFullStringWithHeader());
    }

    private sealed record XamlUnloadObjectOverrideGeneratorContext
    {
        public required HierarchyInfo Hierarchy { get; init; }

        public static XamlUnloadObjectOverrideGeneratorContext Create(INamedTypeSymbol symbol)
        {
            return new()
            {
                Hierarchy = HierarchyInfo.Create(symbol),
            };
        }
    }
}