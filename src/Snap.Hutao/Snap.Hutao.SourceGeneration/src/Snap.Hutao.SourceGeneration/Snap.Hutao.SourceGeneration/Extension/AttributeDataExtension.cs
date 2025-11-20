// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class AttributeDataExtension
{
    public static bool HasNamedArgument<T>(this AttributeData attributeData, string name, T? value)
    {
        foreach ((string propertyName, TypedConstant constant) in attributeData.NamedArguments)
        {
            if (propertyName == name)
            {
                return constant.Value is T argumentValue && EqualityComparer<T?>.Default.Equals(argumentValue, value);
            }
        }

        return false;
    }

    [Obsolete]
    public static bool HasNamedArgument<TValue>(this AttributeData attributeData, string name, Func<TValue, bool> predicate)
    {
        foreach ((string propertyName, TypedConstant constant) in attributeData.NamedArguments)
        {
            if (propertyName == name)
            {
                return constant.Value is TValue argumentValue && predicate(argumentValue);
            }
        }

        return false;
    }

    public static bool TryGetConstructorArgument<T>(this AttributeData attributeData, int index, [NotNullWhen(true)] out T? result)
    {
        if (attributeData.ConstructorArguments.Length > index &&
            attributeData.ConstructorArguments[index].Value is T argument)
        {
            result = argument;
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryGetConstructorArgument(this AttributeData attributeData, int index, out TypedConstant result)
    {
        if (attributeData.ConstructorArguments.Length > index)
        {
            result = attributeData.ConstructorArguments[index];
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryGetNamedArgument(this AttributeData data, string key, out TypedConstant value)
    {
        foreach ((string propertyName, TypedConstant constant) in data.NamedArguments)
        {
            if (propertyName == key)
            {
                value = constant;
                return true;
            }
        }

        value = default;
        return false;
    }
}