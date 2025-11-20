// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.SourceGeneration.Resx;

/// <summary>Represents a parsed composite format string.</summary>
[DebuggerDisplay("{Format}")]
public sealed class CompositeFormat
{
    /// <summary>The parsed segments that make up the composite format string.</summary>
    /// <remarks>
    /// Every segment represents either a literal or a format hole, based on whether Literal
    /// is non-null or ArgIndex is non-negative.
    /// </remarks>
    private readonly (string? Literal, int ArgIndex, int Alignment, string? Format)[] segments;

    /// <summary>The sum of the lengths of all of the literals in <see cref="segments"/>.</summary>
    private readonly int literalLength;

    /// <summary>The number of segments in <see cref="segments"/> that represent format holes.</summary>
    private readonly int formattedCount;

    /// <summary>The number of args required to satisfy the format holes.</summary>
    /// <remarks>This is equal to one more than the largest index required by any format hole.</remarks>
    private readonly int argsRequired;

    /// <summary>Initializes the instance.</summary>
    /// <param name="format">The composite format string that was parsed.</param>
    /// <param name="segments">The parsed segments.</param>
    private CompositeFormat(string format, (string? Literal, int ArgIndex, int Alignment, string? Format)[] segments)
    {
        // Store the format.
        Format = format;

        // Store the segments.
        this.segments = segments;

        // Compute derivative information from the segments.
        int literalLength = 0, formattedCount = 0, argsRequired = 0;
        foreach ((string? Literal, int ArgIndex, int Alignment, string? Format) segment in segments)
        {
            Debug.Assert((segment.Literal is not null) ^ (segment.ArgIndex >= 0), "The segment should represent a literal or a format hole, but not both.");

            if (segment.Literal is { } literal)
            {
                literalLength += literal.Length; // no concern about overflow as these were parsed out of a single string
            }
            else if (segment.ArgIndex >= 0)
            {
                formattedCount++;
                argsRequired = Math.Max(argsRequired, segment.ArgIndex + 1);
            }
        }

        // Store the derivative information.
        Debug.Assert(literalLength >= 0);
        Debug.Assert(formattedCount >= 0);
        Debug.Assert(formattedCount == 0 || argsRequired > 0);
        this.literalLength = literalLength;
        this.formattedCount = formattedCount;
        this.argsRequired = argsRequired;
    }

    /// <summary>Parse the composite format string <paramref name="format"/>.</summary>
    /// <param name="format">The string to parse.</param>
    /// <returns>The parsed <see cref="CompositeFormat"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
    /// <exception cref="FormatException">A format item in <paramref name="format"/> is invalid.</exception>
    public static CompositeFormat Parse([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        List<(string? Literal, int ArgIndex, int Alignment, string? Format)> segments = [];
        if (!TryParseLiterals(format, segments))
        {
            throw new FormatException();
        }

        return new(format, segments.ToArray());
    }

    public static bool TryParse([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, [NotNullWhen(true)] out CompositeFormat? compositeFormat)
    {
        if (format is null)
        {
            compositeFormat = null;
            return false;
        }

        List<(string? Literal, int ArgIndex, int Alignment, string? Format)> segments = [];
        if (!TryParseLiterals(format, segments))
        {
            compositeFormat = null;
            return false;
        }

        compositeFormat = new(format, segments.ToArray());
        return true;
    }

    /// <summary>Gets the original composite format string used to create this <see cref="CompositeFormat"/> instance.</summary>
    public string Format { get; }

    /// <summary>Gets the minimum number of arguments that must be passed to a formatting operation using this <see cref="CompositeFormat"/>.</summary>
    /// <remarks>It's permissible to supply more arguments than this value, but it's an error to pass fewer.</remarks>
    public int MinimumArgumentCount { get => argsRequired; }

    /// <summary>Throws an exception if the specified number of arguments is fewer than the number required.</summary>
    /// <param name="numArgs">The number of arguments provided by the caller.</param>
    /// <exception cref="FormatException">An insufficient number of arguments were provided.</exception>
    internal void ValidateNumberOfArgs(int numArgs)
    {
        if (numArgs < argsRequired)
        {
            throw new FormatException();
        }
    }

    /// <summary>Parse the composite format string into segments.</summary>
    /// <param name="format">The format string.</param>
    /// <param name="segments">The list into which to store the segments.</param>
    /// <returns>true if the format string can be parsed successfully; otherwise, false.</returns>
    private static bool TryParseLiterals(ReadOnlySpan<char> format, List<(string? Literal, int ArgIndex, int Alignment, string? Format)> segments)
    {
        // This parsing logic is copied from string.Format.  It's the same code modified to not format
        // as part of parsing and instead store the parsed literals and argument specifiers (alignment
        // and format) for later use.

        // Rather than parsing directly into the segments list, literals are parsed into a reusable builder.
        // Due to the nature of the parsing logic copied from string.Format, and our desire not to veer from
        // it significantly in order to maintain compatibility and accidental regression, multiple literals
        // next to each other might be parsed separately due to braces in between them.  This builder then
        // allows us to merge those segments back together easily prior to their being appended to the list.
        ValueStringBuilder vsb = new(stackalloc char[ /*string.StackallocCharBufferSizeLimit*/256]);

        // Repeatedly find the next hole and process it.
        int pos = 0;
        char ch;
        while (true)
        {
            // Skip until either the end of the input or the first unescaped opening brace, whichever comes first.
            // Along the way we need to also unescape escaped closing braces.
            while (true)
            {
                // Find the next brace.  If there isn't one, the remainder of the input is text to be appended, and we're done.
                ReadOnlySpan<char> remainder = format[pos..];
                int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    vsb.Append(remainder);
                    segments.Add((vsb.ToString(), -1, 0, null));
                    return true;
                }

                // Append the text until the brace.
                vsb.Append(remainder[..countUntilNextBrace]);
                pos += countUntilNextBrace;

                // Get the brace.  It must be followed by another character, either a copy of itself in the case of being
                // escaped, or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                if (!TryMoveNext(format, ref pos, out ch))
                {
                    return false;
                }
                if (brace == ch)
                {
                    vsb.Append(ch);
                    pos++;
                    continue;
                }

                // This wasn't an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    return false;
                }

                // Proceed to parse the hole.
                segments.Add((vsb.ToString(), -1, 0, null));
                vsb.Length = 0;
                break;
            }

            // We're now positioned just after the opening brace of an argument hole, which consists of
            // an opening brace, an index, an optional width preceded by a comma, and an optional format
            // preceded by a colon, with arbitrary amounts of spaces throughout.
            int width = 0;
            string? itemFormat = null; // used if itemFormat is null

            // First up is the index parameter, which is of the form:
            //     at least on digit
            //     optional any number of spaces
            // We've already read the first digit into ch.
            Debug.Assert(format[pos - 1] == '{');
            Debug.Assert(ch != '{');
            int index = ch - '0';
            if ((uint)index >= 10u)
            {
                return false;
            }

            // Common case is a single digit index followed by a closing brace.  If it's not a closing brace,
            // proceed to finish parsing the full hole format.
            if (!TryMoveNext(format, ref pos, out ch))
            {
                return false;
            }
            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while ((uint)(ch - '0') <= ('9' - '0'))
                {
                    index = index * 10 + ch - '0';
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        return false;
                    }
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        return false;
                    }
                }

                // Parse the optional alignment, which is of the form:
                //     comma
                //     optional any number of spaces
                //     optional -
                //     at least one digit
                //     optional any number of spaces
                if (ch == ',')
                {
                    // Consume optional whitespace.
                    do
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            return false;
                        }
                    }
                    while (ch == ' ');

                    // Consume an optional minus sign indicating left alignment.
                    int leftJustify = 1;
                    if (ch == '-')
                    {
                        leftJustify = -1;
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            return false;
                        }
                    }

                    // Parse alignment digits. The read character must be a digit.
                    width = ch - '0';
                    if ((uint)width >= 10u)
                    {
                        return false;
                    }
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        return false;
                    }
                    while ((uint)(ch - '0') <= ('9' - '0'))
                    {
                        width = width * 10 + ch - '0';
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            return false;
                        }
                    }
                    width *= leftJustify;

                    // Consume optional whitespace
                    while (ch == ' ')
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            return false;
                        }
                    }
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        return false;
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces aren't allowed.
                    int startingPos = pos;
                    while (true)
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            return false;
                        }

                        if (ch == '}')
                        {
                            // Argument hole closed
                            break;
                        }

                        if (ch == '{')
                        {
                            // Braces inside the argument hole are not supported
                            return false;
                        }
                    }

                    startingPos++;
                    itemFormat = format.Slice(startingPos, pos - startingPos).ToString();
                }
            }

            Debug.Assert(format[pos] == '}');
            pos++;

            segments.Add((null, index, width, itemFormat));

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryMoveNext(ReadOnlySpan<char> format, ref int pos, out char nextChar)
        {
            pos++;
            if ((uint)pos >= (uint)format.Length)
            {
                nextChar = '\0';
                return false;
            }

            nextChar = format[pos];
            return true;
        }
    }
}