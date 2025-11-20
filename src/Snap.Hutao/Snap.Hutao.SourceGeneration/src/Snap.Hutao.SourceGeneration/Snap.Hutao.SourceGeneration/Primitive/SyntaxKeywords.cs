// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Snap.Hutao.SourceGeneration.Primitive;

internal static class SyntaxKeywords
{
    public static SyntaxToken AbstractKeyword { get; } = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

    public static SyntaxToken BoolKeyword { get; } = SyntaxFactory.Token(SyntaxKind.BoolKeyword);

    public static SyntaxToken ClassKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ClassKeyword);

    public static SyntaxToken ConstKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ConstKeyword);

    public static SyntaxToken EnableKeyword { get; } = SyntaxFactory.Token(SyntaxKind.EnableKeyword);

    public static SyntaxToken ExternKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ExternKeyword);

    public static SyntaxToken FieldKeyword { get; } = SyntaxFactory.Token(SyntaxKind.FieldKeyword);

    public static SyntaxToken ImplicitKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ImplicitKeyword);

    public static SyntaxToken IntKeyword { get; } = SyntaxFactory.Token(SyntaxKind.IntKeyword);

    public static SyntaxToken InternalKeyword { get; } = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

    public static SyntaxToken ObjectKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ObjectKeyword);

    public static SyntaxToken OverrideKeyword { get; } = SyntaxFactory.Token(SyntaxKind.OverrideKeyword);

    public static SyntaxToken ParamsKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ParamsKeyword);

    public static SyntaxToken PartialKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

    public static SyntaxToken PrivateKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);

    public static SyntaxToken ProtectedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);

    public static SyntaxToken PublicKeyword { get; } = SyntaxFactory.Token(SyntaxKind.PublicKeyword);

    public static SyntaxToken ReadOnlyKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);

    public static SyntaxToken RecordKeyword { get; } = SyntaxFactory.Token(SyntaxKind.RecordKeyword);

    public static SyntaxToken SealedKeyword { get; } = SyntaxFactory.Token(SyntaxKind.SealedKeyword);

    public static SyntaxToken StaticKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StaticKeyword);

    public static SyntaxToken StringKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StringKeyword);

    public static SyntaxToken StructKeyword { get; } = SyntaxFactory.Token(SyntaxKind.StructKeyword);

    public static SyntaxToken ThisKeyword { get; } = SyntaxFactory.Token(SyntaxKind.ThisKeyword);

    public static SyntaxToken UIntKeyword { get; } = SyntaxFactory.Token(SyntaxKind.UIntKeyword);

    public static SyntaxToken UnsafeKeyword { get; } = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword);

    public static SyntaxToken VoidKeyword { get; } = SyntaxFactory.Token(SyntaxKind.VoidKeyword);
}