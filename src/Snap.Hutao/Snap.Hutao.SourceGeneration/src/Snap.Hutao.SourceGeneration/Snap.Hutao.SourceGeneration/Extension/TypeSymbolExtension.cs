// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Primitive;
using System;
using System.Linq;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class TypeSymbolExtension
{
    public static bool HasOrInheritsFromFullyQualifiedMetadataName(this ITypeSymbol typeSymbol, string name)
    {
        for (ITypeSymbol? currentType = typeSymbol; currentType is not null; currentType = currentType.BaseType)
        {
            if (currentType.HasFullyQualifiedMetadataName(name))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOrInheritsFromType(this ITypeSymbol typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        for (ITypeSymbol? currentType = typeSymbol; currentType is not null; currentType = currentType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, baseTypeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    public static bool InheritsFromFullyQualifiedMetadataName(this ITypeSymbol typeSymbol, string name)
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;

        while (baseType is not null)
        {
            if (baseType.HasFullyQualifiedMetadataName(name))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static bool InheritsFromType(this ITypeSymbol typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        INamedTypeSymbol? currentBaseTypeSymbol = typeSymbol.BaseType;

        while (currentBaseTypeSymbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentBaseTypeSymbol, baseTypeSymbol))
            {
                return true;
            }

            currentBaseTypeSymbol = currentBaseTypeSymbol.BaseType;
        }

        return false;
    }

    public static bool HasInterfaceWithFullyQualifiedMetadataName(this ITypeSymbol typeSymbol, string name)
    {
        foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
        {
            if (interfaceType.HasFullyQualifiedMetadataName(name))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOrInheritsAttribute(this ITypeSymbol typeSymbol, Func<AttributeData, bool> predicate)
    {
        for (ITypeSymbol? currentType = typeSymbol; currentType is not null; currentType = currentType.BaseType)
        {
            if (currentType.GetAttributes().Any(predicate))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOrInheritsAttributeWithFullyQualifiedMetadataName(this ITypeSymbol typeSymbol, string name)
    {
        for (ITypeSymbol? currentType = typeSymbol; currentType is not null; currentType = currentType.BaseType)
        {
            if (currentType.HasAttributeWithFullyQualifiedMetadataName(name))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOrInheritsAttributeWithType(this ITypeSymbol typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        for (ITypeSymbol? currentType = typeSymbol; currentType is not null; currentType = currentType.BaseType)
        {
            if (currentType.HasAttributeWithType(baseTypeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    public static bool InheritsAttributeWithFullyQualifiedMetadataName(this ITypeSymbol typeSymbol, string name)
    {
        if (typeSymbol.BaseType is { } baseTypeSymbol)
        {
            return HasOrInheritsAttributeWithFullyQualifiedMetadataName(baseTypeSymbol, name);
        }

        return false;
    }

    public static bool HasFullyQualifiedMetadataName(this ITypeSymbol symbol, string name)
    {
        using ImmutableArrayBuilder<char> builder = ImmutableArrayBuilder<char>.Rent();

        symbol.AppendFullyQualifiedMetadataName(in builder);

        return builder.WrittenSpan.SequenceEqual(name.AsSpan());
    }

    public static string GetFullyQualifiedMetadataName(this ITypeSymbol symbol)
    {
        using ImmutableArrayBuilder<char> builder = ImmutableArrayBuilder<char>.Rent();

        symbol.AppendFullyQualifiedMetadataName(in builder);

        return builder.ToString();
    }

    private static void AppendFullyQualifiedMetadataName(this ITypeSymbol symbol, in ImmutableArrayBuilder<char> builder)
    {
        static void BuildFrom(ISymbol? symbol, in ImmutableArrayBuilder<char> builder)
        {
            switch (symbol)
            {
                // Namespaces that are nested also append a leading '.'
                case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                    BuildFrom(symbol.ContainingNamespace, in builder);
                    builder.Add('.');
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;

                // Other namespaces (ie. the one right before global) skip the leading '.'
                case INamespaceSymbol { IsGlobalNamespace: false }:
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;

                // Types with no namespace just have their metadata name directly written
                case ITypeSymbol { ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true } }:
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;

                // Types with a containing non-global namespace also append a leading '.'
                case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                    BuildFrom(namespaceSymbol, in builder);
                    builder.Add('.');
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;

                // Nested types append a leading '+'
                case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                    BuildFrom(typeSymbol, in builder);
                    builder.Add('+');
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;
                default:
                    break;
            }
        }

        BuildFrom(symbol, in builder);
    }
}