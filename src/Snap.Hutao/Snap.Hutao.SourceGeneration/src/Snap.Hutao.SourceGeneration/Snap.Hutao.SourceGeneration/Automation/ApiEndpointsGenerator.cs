// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.SyntaxKeywords;

namespace Snap.Hutao.SourceGeneration.Automation;

[Generator(LanguageNames.CSharp)]
internal sealed class ApiEndpointsGenerator : IIncrementalGenerator
{
    private const string FileName = "Endpoints.csv";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EndpointsMetadataContext> provider = context.AdditionalTextsProvider
            .Where(Match)
            .Select(EndpointsMetadataContext.Create)
            .Where(EndpointsMetadataContextNotEmpty);
        context.RegisterImplementationSourceOutput(provider, GenerateWrapper);
    }

    private static bool Match(AdditionalText text)
    {
        // Match '*Endpoints.csv' files
        return Path.GetFileName(text.Path).EndsWith(FileName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndpointsMetadataContextNotEmpty(EndpointsMetadataContext context)
    {
        return !context.Endpoints.IsEmpty;
    }

    private static void GenerateWrapper(SourceProductionContext production, EndpointsMetadataContext context)
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

    private static void Generate(SourceProductionContext production, EndpointsMetadataContext context)
    {
        string interfaceName = $"I{context.Name}";
        string chineseImplName = $"{context.Name}ImplementationForChinese";
        string overseaImplName = $"{context.Name}ImplementationForOversea";

        IdentifierNameSyntax interfaceIdentifier = IdentifierName(interfaceName);

        CompilationUnitSyntax compilation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration(context.ExtraInfo?.Namespace ?? "Snap.Hutao.Web")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                    [
                        InterfaceDeclaration(interfaceName)
                            .WithModifiers(InternalPartialTokenList)
                            .WithMembers(List(GenerateInterfaceMethods(context.Endpoints))),
                        ClassDeclaration(chineseImplName)
                            .WithModifiers(InternalAbstractTokenList)
                            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(interfaceIdentifier))))
                            .WithMembers(List(GenerateClassMethods(context.Endpoints, true))),
                        ClassDeclaration(overseaImplName)
                            .WithModifiers(InternalAbstractTokenList)
                            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(interfaceIdentifier))))
                            .WithMembers(List(GenerateClassMethods(context.Endpoints, false)))
                    ]))))
            .NormalizeWhitespace();

        production.AddSource($"{context.Name}.g.cs", compilation.ToFullStringWithHeader());
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaceMethods(ImmutableArray<EndpointsMetadata> metadataArray)
    {
        foreach (EndpointsMetadata metadata in metadataArray)
        {
            if (metadata.GetMethodDeclaration() is not MethodDeclarationSyntax methodDeclaration)
            {
                continue;
            }

            string lead = $"""
                /// <summary>
                /// <code>CN: {metadata.Chinese?.Replace("&", "&amp;")}</code>
                /// <code>OS: {metadata.Oversea?.Replace("&", "&amp;")}</code>
                /// </summary>

                """;

            yield return methodDeclaration
                .WithLeadingTrivia(ParseLeadingTrivia(lead))
                .WithSemicolonToken(SemicolonToken);
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateClassMethods(ImmutableArray<EndpointsMetadata> metadataArray, bool isChinese)
    {
        foreach (EndpointsMetadata metadata in metadataArray)
        {
            if (metadata.GetMethodDeclaration() is not MethodDeclarationSyntax methodDeclaration)
            {
                continue;
            }

            yield return methodDeclaration
                .WithModifiers(PublicTokenList)
                .WithExpressionBody(ArrowExpressionClause(isChinese ? metadata.GetChineseExpression() : metadata.GetOverseaExpression()))
                .WithSemicolonToken(SemicolonToken);
        }
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        List<string> fields = [];
        StringBuilder currentField = new();
        bool insideQuotes = false;

        ReadOnlySpan<char> lineSpan = line.AsSpan();
        for (int i = 0; i < lineSpan.Length; i++)
        {
            ref readonly char currentChar = ref lineSpan[i];

            if (currentChar is '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 处理双引号转义
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (currentChar == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(currentChar);
            }
        }

        // 添加最后一个字段
        fields.Add(currentField.ToString());

        return fields;
    }

    private sealed record EndpointsMetadataContext
    {
        public required string Name { get; init; }

        public required EndpointsExtraInfo? ExtraInfo { get; init; }

        public required EquatableArray<EndpointsMetadata> Endpoints { get; init; }

        public static EndpointsMetadataContext Create(AdditionalText text, CancellationToken token)
        {
            string fileName = Path.GetFileNameWithoutExtension(text.Path);

            EndpointsExtraInfo? extraInfo = default;
            ImmutableArray<EndpointsMetadata>.Builder endpointsBuilder = ImmutableArray.CreateBuilder<EndpointsMetadata>();
            using (StringReader reader = new(text.GetText(token)!.ToString()))
            {
                while (reader.ReadLine() is { Length: > 0 } line)
                {
                    if (line is "Name,CN,OS")
                    {
                        continue;
                    }

                    if (line.StartsWith("Extra:", StringComparison.OrdinalIgnoreCase))
                    {
                        extraInfo = JsonSerializer.Deserialize<EndpointsExtraInfo>(line[6..]);
                        continue;
                    }

                    IReadOnlyList<string> columns = ParseCsvLine(line);
                    EndpointsMetadata metadata = new()
                    {
                        MethodString = columns.ElementAtOrDefault(0),
                        Chinese = columns.ElementAtOrDefault(1),
                        Oversea = columns.ElementAtOrDefault(2),
                    };

                    endpointsBuilder.Add(metadata);
                }
            }

            return new()
            {
                Name = fileName,
                ExtraInfo = extraInfo,
                Endpoints = endpointsBuilder.ToImmutable(),
            };
        }
    }

    private sealed class EndpointsMetadata : IEquatable<EndpointsMetadata?>
    {
        public required string? MethodString { get; init; }

        public required string? Chinese { get; init; }

        public required string? Oversea { get; init; }

        public MemberDeclarationSyntax? GetMethodDeclaration()
        {
            return string.IsNullOrEmpty(MethodString) ? default : ParseMemberDeclaration(MethodString!);
        }

        public ExpressionSyntax GetChineseExpression()
        {
            return string.IsNullOrEmpty(Chinese) ? WellKnownSyntax.ThrowNotSupportedException : ParseExpression($"$\"{Chinese}\"");
        }

        public ExpressionSyntax GetOverseaExpression()
        {
            return string.IsNullOrEmpty(Oversea) ? WellKnownSyntax.ThrowNotSupportedException : ParseExpression($"$\"{Oversea}\"");
        }

        public bool Equals(EndpointsMetadata? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return MethodString == other.MethodString && Chinese == other.Chinese && Oversea == other.Oversea;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is EndpointsMetadata other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MethodString, Chinese, Oversea);
        }
    }

    private sealed record EndpointsExtraInfo
    {
        public string? Namespace { get; init; }
    }
}