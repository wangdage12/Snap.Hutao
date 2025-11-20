// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Model;
using System.Collections.Immutable;

namespace Snap.Hutao.SourceGeneration.Xaml;

internal static class CommandHelper
{
    public static TypeSyntax GetCommandType(AttributedMethodInfo attributedMethod)
    {
        bool isAsync = attributedMethod.Method.FullyQualifiedReturnTypeMetadataName.StartsWith("System.Threading.Tasks.Task");
        SyntaxToken identifier = SyntaxFactory.Identifier(isAsync ? "AsyncRelayCommand" : "RelayCommand");

        TypeSyntax propertyType;
        ImmutableArray<ParameterInfo> parameters = attributedMethod.Method.Parameters;
        if (parameters.Length >= 1)
        {
            TypeSyntax type = SyntaxFactory.ParseTypeName(parameters[0].FullyQualifiedTypeName);
            propertyType = SyntaxFactory.QualifiedName(WellKnownSyntax.NameOfCommunityToolkitMvvmInput, SyntaxFactory.GenericName(identifier).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type))));
        }
        else
        {
            propertyType = SyntaxFactory.QualifiedName(WellKnownSyntax.NameOfCommunityToolkitMvvmInput, SyntaxFactory.IdentifierName(identifier));
        }

        return propertyType;
    }
}