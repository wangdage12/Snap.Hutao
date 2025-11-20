// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Snap.Hutao.SourceGeneration.Primitive.FastSyntaxFactory;

namespace Snap.Hutao.SourceGeneration;

internal static class WellKnownSyntax
{
    // [global::JetBrains.Annotations.MeansImplicitUse]
    public static readonly AttributeListSyntax JetBrainsAnnotationsMeansImplicitUseAttributeList = AttributeList(SingletonSeparatedList(Attribute(ParseName("global::JetBrains.Annotations.MeansImplicitUse"))));

    // : global::System.Attribute
    public static readonly BaseListSyntax SystemAttributeBaseList = BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(ParseTypeName("global::System.Attribute"))));

    // throw new NotSupportedException()
    public static readonly ExpressionSyntax ThrowNotSupportedException = ThrowExpression(ObjectCreationExpression(IdentifierName("NotSupportedException")).WithEmptyArgumentList());

    public static readonly NameSyntax NameOfCommunityToolkitMvvmInput = ParseName("global::CommunityToolkit.Mvvm.Input");
    public static readonly NameSyntax NameOfCommunityToolkitMvvmInputAsyncRelayCommandOptions = ParseName("global::CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions");
    public static readonly NameSyntax NameOfMicrosoftUIXaml = ParseName("global::Microsoft.UI.Xaml");
    public static readonly NameSyntax NameOfSnapHutaoModelPrimitiveConverter = ParseName("global::Snap.Hutao.Model.Primitive.Converter");
    public static readonly NameSyntax NameOfSystem = ParseName("global::System");
    public static readonly NameSyntax NameOfSystemComponentModelEditorBrowsable = ParseName("global::System.ComponentModel.EditorBrowsable");
    public static readonly NameSyntax NameOfSystemComponentModelEditorBrowsableState = ParseName("global::System.ComponentModel.EditorBrowsableState");
    public static readonly NameSyntax NameOfSystemDiagnosticsCodeAnalysisMaybeNull = ParseName("global::System.Diagnostics.CodeAnalysis.MaybeNull");
    public static readonly NameSyntax NameOfSystemDiagnosticsCodeAnalysisNotNullIfNotNull = ParseName("global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull");
    public static readonly NameSyntax NameOfSystemNumerics = ParseName("global::System.Numerics");
    public static readonly NameSyntax NameOfSystemRuntimeCompilerServicesUnsafeAccessor = ParseName("global::System.Runtime.CompilerServices.UnsafeAccessor");
    public static readonly NameSyntax NameOfSystemRuntimeCompilerServicesUnsafeAccessorKind = ParseName("global::System.Runtime.CompilerServices.UnsafeAccessorKind");
    public static readonly NameSyntax NameOfSystemTextJsonSerializationJsonConverter = ParseName("global::System.Text.Json.Serialization.JsonConverter");

    public static readonly TypeSyntax TypeOfCommunityToolkitMvvmMessagingIMessenger = ParseTypeName("global::CommunityToolkit.Mvvm.Messaging.IMessenger");
    public static readonly TypeSyntax TypeOfCommunityToolkitMvvmMessagingIMessengerExtensions = ParseTypeName("global::CommunityToolkit.Mvvm.Messaging.IMessengerExtensions");
    public static readonly TypeSyntax TypeOfMicrosoftExtensionsDependencyInjectionIServiceCollection = ParseTypeName("global::Microsoft.Extensions.DependencyInjection.IServiceCollection");
    public static readonly TypeSyntax TypeOfMicrosoftExtensionsDependencyInjectionServiceLifetime = ParseTypeName("global::Microsoft.Extensions.DependencyInjection.ServiceLifetime");
    public static readonly TypeSyntax TypeOfSystemArgumentException = ParseTypeName("global::System.ArgumentException");
    public static readonly TypeSyntax TypeOfSystemArgumentNullException = ParseTypeName("global::System.ArgumentNullException");
    public static readonly TypeSyntax TypeOfSystemAttributeTargets = ParseTypeName("global::System.AttributeTargets");
    public static readonly TypeSyntax TypeOfSystemRuntimeCompilerServicesUnsafe = ParseTypeName("global::System.Runtime.CompilerServices.Unsafe");
    public static readonly TypeSyntax TypeOfSystemEnum = ParseTypeName("global::System.Enum");
    public static readonly TypeSyntax TypeOfSystemGlobalizationCultureInfo = ParseTypeName("global::System.Globalization.CultureInfo");
    public static readonly TypeSyntax TypeOfSystemIComparable = ParseTypeName("global::System.IComparable");
    public static readonly TypeSyntax TypeOfSystemIOStream = ParseTypeName("global::System.IO.Stream");
    public static readonly TypeSyntax TypeOfSystemIServiceProvider = ParseTypeName("global::System.IServiceProvider");
    public static readonly TypeSyntax TypeOfSystemNetHttpHttpClient = ParseTypeName("global::System.Net.Http.HttpClient");
    public static readonly TypeSyntax TypeOfSystemNetHttpIHttpClientFactory = ParseTypeName("global::System.Net.Http.IHttpClientFactory");
    public static readonly TypeSyntax TypeOfSystemNetHttpSocketsHttpHandler = ParseTypeName("global::System.Net.Http.SocketsHttpHandler");
    public static readonly TypeSyntax TypeOfSystemResourcesResourceManager = ParseTypeName("global::System.Resources.ResourceManager");
    public static readonly TypeSyntax TypeOfSystemType = ParseTypeName("global::System.Type");

    public static readonly AttributeArgumentSyntax AttributeTargetsClass = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Class")));
    public static readonly AttributeArgumentSyntax AttributeTargetsConstructor = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Constructor")));
    public static readonly AttributeArgumentSyntax AttributeTargetsEnum = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Enum")));
    public static readonly AttributeArgumentSyntax AttributeTargetsField = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Field")));
    public static readonly AttributeArgumentSyntax AttributeTargetsFieldAndProperty = AttributeArgument(BitwiseOrExpression(
        SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Field")),
        SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Property"))));
    public static readonly AttributeArgumentSyntax AttributeTargetsMethod = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Method")));
    public static readonly AttributeArgumentSyntax AttributeTargetsProperty = AttributeArgument(SimpleMemberAccessExpression(TypeOfSystemAttributeTargets, IdentifierName("Property")));

    public static readonly AttributeArgumentSyntax AllowMultipleTrue = AttributeArgument(TrueLiteralExpression).WithNameEquals(NameEquals(IdentifierName("AllowMultiple")));
    public static readonly AttributeArgumentSyntax InheritedFalse = AttributeArgument(FalseLiteralExpression).WithNameEquals(NameEquals(IdentifierName("Inherited")));

    // ArgumentNullException.ThrowIfNull(%argumentExpression%)
    public static InvocationExpressionSyntax ArgumentNullExceptionThrowIfNull(ExpressionSyntax argumentExpression)
    {
        return InvocationExpression(
                SimpleMemberAccessExpression(
                    TypeOfSystemArgumentNullException,
                    IdentifierName("ThrowIfNull")))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                Argument(argumentExpression))));
    }

    // %serviceProvider%.GetRequiredService<%type%>()
    public static InvocationExpressionSyntax ServiceProviderGetRequiredService(ExpressionSyntax serviceProvider, TypeSyntax type)
    {
        return InvocationExpression(SimpleMemberAccessExpression(
                serviceProvider,
                GenericName("GetRequiredService").WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))))
            .WithEmptyArgumentList();
    }

    // %serviceProvider%.GetRequiredKeyedService<%type%>(%argument%)
    public static InvocationExpressionSyntax ServiceProviderGetRequiredKeyedService(ExpressionSyntax serviceProvider, TypeSyntax type, ExpressionSyntax argument)
    {
        return InvocationExpression(SimpleMemberAccessExpression(
                serviceProvider,
                GenericName("GetRequiredKeyedService").WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(type)))))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(argument))));
    }
}