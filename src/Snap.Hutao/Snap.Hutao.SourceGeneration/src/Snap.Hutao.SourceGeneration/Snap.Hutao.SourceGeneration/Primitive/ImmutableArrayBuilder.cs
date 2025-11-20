// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal ref struct ImmutableArrayBuilder<T> : IDisposable
{
    private Writer? writer;

    public static ImmutableArrayBuilder<T> Rent()
    {
        return new(new());
    }

    private ImmutableArrayBuilder(Writer writer)
    {
        this.writer = writer;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => writer!.Count;
    }

    [UnscopedRef]
    public readonly ReadOnlySpan<T> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => writer!.WrittenSpan;
    }

    public readonly void Add(T item)
    {
        writer!.Add(item);
    }

    public readonly void AddRange(scoped ReadOnlySpan<T> items)
    {
        writer!.AddRange(items);
    }

    public readonly ImmutableArray<T> ToImmutable()
    {
        T[] array = writer!.WrittenSpan.ToArray();

        return Unsafe.As<T[], ImmutableArray<T>>(ref array);
    }

    public readonly T[] ToArray()
    {
        return writer!.WrittenSpan.ToArray();
    }

    public readonly IEnumerable<T> AsEnumerable()
    {
        return writer!;
    }

    public override readonly string ToString()
    {
        return writer!.WrittenSpan.ToString();
    }

    public void Dispose()
    {
        Writer? writer = this.writer;

        this.writer = null;

        writer?.Dispose();
    }

    private sealed class Writer : ICollection<T>, IDisposable
    {
        private T?[]? array;
        private int index;

        public Writer()
        {
            array = ArrayPool<T?>.Shared.Rent(typeof(T) == typeof(char) ? 1024 : 8);
            index = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index;
        }

        public ReadOnlySpan<T> WrittenSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(array!, 0, index);
        }

        bool ICollection<T>.IsReadOnly { get => true; }

        public void Add(T value)
        {
            EnsureCapacity(1);
            array![index++] = value;
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            EnsureCapacity(items.Length);

            items.CopyTo(array.AsSpan(index)!);

            index += items.Length;
        }

        public void Dispose()
        {
            T?[]? array = this.array;

            this.array = null;

            if (array is not null)
            {
                ArrayPool<T?>.Shared.Return(array, clearArray: typeof(T) != typeof(char));
            }
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this.array!, 0, array, arrayIndex, this.index);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            T?[] array = this.array!;
            int length = this.index;

            for (int i = 0; i < length; i++)
            {
                yield return array[i]!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int requestedSize)
        {
            if (requestedSize > array!.Length - index)
            {
                ResizeBuffer(requestedSize);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeBuffer(int sizeHint)
        {
            int minimumSize = index + sizeHint;

            T?[] oldArray = array!;
            T?[] newArray = ArrayPool<T?>.Shared.Rent(minimumSize);

            Array.Copy(oldArray, newArray, index);

            array = newArray;

            ArrayPool<T?>.Shared.Return(oldArray, clearArray: typeof(T) != typeof(char));
        }
    }
}