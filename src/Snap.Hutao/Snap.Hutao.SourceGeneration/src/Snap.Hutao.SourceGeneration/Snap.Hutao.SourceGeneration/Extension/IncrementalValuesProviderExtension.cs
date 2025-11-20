// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Snap.Hutao.SourceGeneration.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Snap.Hutao.SourceGeneration.Extension;

internal static class IncrementalValuesProviderExtension
{
    public static IncrementalValuesProvider<(TKey Left, EquatableArray<TElement> Right)> GroupBy<TLeft, TRight, TKey, TElement>(
        this IncrementalValuesProvider<(TLeft Left, TRight Right)> source,
        Func<(TLeft Left, TRight Right), TKey> keySelector,
        Func<(TLeft Left, TRight Right), TElement> elementSelector)
        where TLeft : IEquatable<TLeft>
        where TRight : IEquatable<TRight>
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        return source.Collect().SelectMany((item, token) =>
        {
            Dictionary<TKey, ImmutableArray<TElement>.Builder> map = new();

            foreach ((TLeft, TRight) pair in item)
            {
                TKey key = keySelector(pair);
                TElement element = elementSelector(pair);

                if (!map.TryGetValue(key, out ImmutableArray<TElement>.Builder builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();

                    map.Add(key, builder);
                }

                builder.Add(element);
            }

            token.ThrowIfCancellationRequested();

            ImmutableArray<(TKey Key, EquatableArray<TElement> Elements)>.Builder result =
                ImmutableArray.CreateBuilder<(TKey, EquatableArray<TElement>)>();

            foreach (KeyValuePair<TKey, ImmutableArray<TElement>.Builder> entry in map)
            {
                result.Add((entry.Key, entry.Value.ToImmutable()));
            }

            return result;
        });
    }

    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TElement> Right)> GroupBy<TKey, TElement>(
        this IncrementalValuesProvider<TElement> source,
        Func<TElement, TKey> keySelector)
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        return source.Collect().SelectMany((item, token) =>
        {
            Dictionary<TKey, ImmutableArray<TElement>.Builder> map = new();

            foreach (TElement source in item)
            {
                TKey key = keySelector(source);

                if (!map.TryGetValue(key, out ImmutableArray<TElement>.Builder builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();

                    map.Add(key, builder);
                }

                builder.Add(source);
            }

            token.ThrowIfCancellationRequested();

            ImmutableArray<(TKey Key, EquatableArray<TElement> Elements)>.Builder result =
                ImmutableArray.CreateBuilder<(TKey, EquatableArray<TElement>)>();

            foreach (KeyValuePair<TKey, ImmutableArray<TElement>.Builder> entry in map)
            {
                result.Add((entry.Key, entry.Value.ToImmutable()));
            }

            return result;
        });
    }

    public static IncrementalValuesProvider<T> Distinct<T>(this IncrementalValuesProvider<T> source)
        where T : IEquatable<T>
    {
        return source.Collect().SelectMany((array, token) => array.Distinct().ToImmutableArray());
    }

    public static IncrementalValuesProvider<T> Concat<T>(this IncrementalValuesProvider<T> source, IncrementalValuesProvider<T> other)
    {
        return source.Collect().Combine(other.Collect()).SelectMany(static (t, token) => t.Left.AddRange(t.Right));
    }
}