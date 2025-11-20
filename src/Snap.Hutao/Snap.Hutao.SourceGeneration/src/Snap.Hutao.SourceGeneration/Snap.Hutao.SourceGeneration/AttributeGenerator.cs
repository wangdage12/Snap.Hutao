// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snap.Hutao.SourceGeneration.Extension;
using Snap.Hutao.SourceGeneration.Primitive;
using System.Runtime.CompilerServices;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;
using static Snap.Hutao.SourceGeneration.WellKnownSyntax;

[assembly:InternalsVisibleTo("Snap.Hutao.SourceGeneration.Test")]

namespace Snap.Hutao.SourceGeneration;

[Generator(LanguageNames.CSharp)]
internal sealed class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAllAttributes);
    }

    public static void GenerateAllAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        CompilationUnitSyntax coreAnnotation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.Annotation")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(Identifier("CommandAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsMethod, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("CommandAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(StringType, Identifier("commandName")))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("CommandAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(StringType, Identifier("commandName")),
                                    Parameter(StringType, Identifier("canExecuteName"))
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("AllowConcurrentExecutions"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                        ])),
                    ClassDeclaration(Identifier("GeneratedConstructorAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsConstructor, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("GeneratedConstructorAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("CallBaseConstructor"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, Identifier("InitializeComponent"))
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithAccessorList(GetAndSetAccessorList)
                        ])),
                    ClassDeclaration(Identifier("BindableCustomPropertyProviderAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList),
                    ClassDeclaration(Identifier("DependencyPropertyAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, allowMultiple: true, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(
                            TypeParameter(Identifier("T")))))
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("DependencyPropertyAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(StringType, Identifier("name"))
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(BoolType, Identifier("IsAttached"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(NullableType(TypeOfSystemType), Identifier("TargetType"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(NullableObjectType, Identifier("DefaultValue"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(NullableStringType, Identifier("CreateDefaultValueCallbackName"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(NullableStringType, Identifier("PropertyChangedCallbackName"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, Identifier("NotNull"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                        ])),
                    ClassDeclaration(Identifier("FieldAccessorAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsProperty, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.Annotation.Attributes.g.cs", coreAnnotation.ToFullStringWithHeader());

        TypeSyntax typeOfHttpClientConfiguration = ParseTypeName("global::Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.HttpClientConfiguration");

        CompilationUnitSyntax coreDependencyInjectionAnnotationHttpClient = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(Identifier("HttpClientAttribute"))
                        .WithAttributeLists(List(
                        [
                            JetBrainsAnnotationsMeansImplicitUseAttributeList,
                            SystemAttributeUsageList(AttributeTargetsClass, inherited: false)
                        ]))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("HttpClientAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(typeOfHttpClientConfiguration, Identifier("configuration")))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("HttpClientAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(typeOfHttpClientConfiguration, Identifier("configuration")),
                                    Parameter(TypeOfSystemType, Identifier("serviceType"))
                                ])))
                                .WithEmptyBlockBody()
                        ])),
                    ClassDeclaration(Identifier("PrimaryHttpMessageHandlerAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(List<MemberDeclarationSyntax>(
                        [
                            PropertyDeclaration(IntType, "MaxAutomaticRedirections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxConnectionsPerServer")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxResponseDrainSize")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(IntType, "MaxResponseHeadersLength")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: MeterFactory, PlaintextStreamFilter, PooledConnectionIdleTimeout, PooledConnectionLifetime
                            // Unsupported: Proxy, RequestHeaderEncodingSelector, ResponseDrainTimeout, ResponseHeaderEncodingSelector
                            // Unsupported: SslOptions
                            PropertyDeclaration(BoolType, "PreAuthenticate")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: KeepAlivePingTimeout
                            PropertyDeclaration(ParseTypeName("global::System.Net.Http.HttpKeepAlivePingPolicy"), "KeepAlivePingPolicy")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: KeepAlivePingDelay, ActivityHeadersPropagator
                            PropertyDeclaration(BoolType, "AllowAutoRedirect")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(ParseTypeName("global::System.Net.DecompressionMethods"), "AutomaticDecompression")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: ConnectCallback
                            PropertyDeclaration(BoolType, "UseCookies")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: CookieContainer, ConnectTimeout, DefaultProxyCredentials
                            PropertyDeclaration(BoolType, "EnableMultipleHttp2Connections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            PropertyDeclaration(BoolType, "EnableMultipleHttp3Connections")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: Expect100ContinueTimeout
                            PropertyDeclaration(IntType, "InitialHttp2StreamWindowSize")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList),
                            // Unsupported: Credentials
                            PropertyDeclaration(BoolType, "UseProxy")
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList)
                        ]))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient.Attributes.g.cs", coreDependencyInjectionAnnotationHttpClient.ToFullStringWithHeader());

        CompilationUnitSyntax coreDependencyInjectionAnnotation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Core.DependencyInjection.Annotation")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(Identifier("ServiceAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsClass, inherited: false)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList).WithMembers(List<MemberDeclarationSyntax>(
                        [
                            ConstructorDeclaration(Identifier("ServiceAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime, Identifier("serviceLifetime")))))
                                .WithEmptyBlockBody(),
                            ConstructorDeclaration(Identifier("ServiceAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime, Identifier("serviceLifetime")),
                                    Parameter(TypeOfSystemType, Identifier("serviceType"))
                                ])))
                                .WithEmptyBlockBody(),
                            PropertyDeclaration(NullableObjectType, Identifier("Key"))
                                .WithModifiers(PublicTokenList)
                                .WithAccessorList(GetAndSetAccessorList)
                        ])),
                    ClassDeclaration(Identifier("FromKeyedServicesAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsFieldAndProperty)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("FromKeyedServicesAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(ObjectType, Identifier("key")))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Core.DependencyInjection.Annotation.Attributes.g.cs", coreDependencyInjectionAnnotation.ToFullStringWithHeader());

        CompilationUnitSyntax resourceLocalization = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("Snap.Hutao.Resource.Localization")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(List<MemberDeclarationSyntax>(
                [
                    ClassDeclaration(Identifier("ExtendedEnumAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsEnum)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList),
                    ClassDeclaration(Identifier("LocalizationKeyAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsField)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("LocalizationKeyAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SingletonSeparatedList(
                                    Parameter(StringType, Identifier("key")))))
                                .WithEmptyBlockBody()))
                ]))))
            .NormalizeWhitespace();

        context.AddSource("Snap.Hutao.Resource.Localization.Attributes.g.cs", resourceLocalization.ToFullStringWithHeader());

        CompilationUnitSyntax interceptsLocation = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(FileScopedNamespaceDeclaration("System.Runtime.CompilerServices")
                .WithLeadingTrivia(NullableEnableTriviaList)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(Identifier("InterceptsLocationAttribute"))
                        .WithAttributeLists(SingletonList(SystemAttributeUsageList(AttributeTargetsMethod, allowMultiple: true)))
                        .WithModifiers(InternalSealedTokenList)
                        .WithBaseList(SystemAttributeBaseList)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            ConstructorDeclaration(Identifier("InterceptsLocationAttribute"))
                                .WithModifiers(PublicTokenList)
                                .WithParameterList(ParameterList(SeparatedList(
                                [
                                    Parameter(IntType, Identifier("version")),
                                    Parameter(StringType, Identifier("data"))
                                ])))
                                .WithEmptyBlockBody()))))))
            .NormalizeWhitespace();

        context.AddSource("System.Runtime.CompilerServices.InterceptsLocationAttribute.g.cs", interceptsLocation.ToFullStringWithHeader());
    }

    private static AttributeListSyntax SystemAttributeUsageList(AttributeArgumentSyntax attributeTargets, bool allowMultiple = false, bool inherited = true)
    {
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments = SingletonSeparatedList(attributeTargets);
        if (allowMultiple)
        {
            arguments = arguments.Add(AllowMultipleTrue);
        }

        if (!inherited)
        {
            arguments = arguments.Add(InheritedFalse);
        }

        return AttributeList(SingletonSeparatedList(
            Attribute(ParseName("global::System.AttributeUsage"))
                .WithArgumentList(AttributeArgumentList(arguments))));
    }
}