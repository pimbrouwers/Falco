namespace Falco.OpenApi

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Metadata
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
open Microsoft.AspNetCore.Routing

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointNameMetadata(name) =
    interface IEndpointNameMetadata with
        member val EndpointName : string = name

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointDescriptionMetadata(description) =
    interface IEndpointDescriptionMetadata with
        member val Description : string = description

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointSummaryMetadata(summary) =
    interface IEndpointSummaryMetadata with
        member val Summary : string = summary

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointTagsMetadata(tags : string list) =
    interface ITagsMetadata with
        member val Tags = tags

type FalcoEndpointParameterMetadataSource =
    | PathParameter
    | QueryParameter

[<Sealed>]
type internal FalcoEndpointParameterMetadata(
    source : FalcoEndpointParameterMetadataSource,
    type' : Type,
    name : string,
    required : bool) =
    member val Source : FalcoEndpointParameterMetadataSource = source
    member val Type : Type = type'
    member val Name : string = name
    member val Required : bool = required

[<Sealed>]
type internal FalcoEndpointResponseMetadata(type', statusCode, contentTypes) =
    member val Type : Type = type'
    member val StatusCode : int = statusCode
    member val ContentTypes : string seq = contentTypes

type internal FalcoEndpointModelMetadata(
    Identity : ModelMetadataIdentity,
    BindingSource : BindingSource,
    DisplayName : string,
    IsRequired : bool) =
    inherit ModelMetadata(Identity)

    override val AdditionalValues : IReadOnlyDictionary<obj, obj>
    override val BinderModelName : string
    override val BinderType  : Type
    override val BindingSource  : BindingSource = BindingSource
    override val ConvertEmptyStringToNull  : bool
    override val DataTypeName  : string
    override val Description  : string
    override val DisplayFormatString  : string
    override val DisplayName  : string = DisplayName
    override val EditFormatString  : string
    override val ElementMetadata  : ModelMetadata
    override val EnumGroupedDisplayNamesAndValues  : IEnumerable<KeyValuePair<EnumGroupAndName, string>>
    override val EnumNamesAndValues  : IReadOnlyDictionary<string, string>
    override val HasNonDefaultEditFormat  : bool
    override val HideSurroundingHtml  : bool
    override val HtmlEncode  : bool
    override val IsBindingAllowed  : bool
    override val IsBindingRequired  : bool
    override val IsEnum  : bool
    override val IsFlagsEnum  : bool
    override val IsReadOnly  : bool
    override val IsRequired  : bool = IsRequired
    override val ModelBindingMessageProvider  : ModelBindingMessageProvider
    override val NullDisplayText  : string
    override val Order  : int
    override val Placeholder  : string
    override val Properties  : ModelPropertyCollection
    override val PropertyFilterProvider  : IPropertyFilterProvider
    override val PropertyGetter  : Func<obj, obj>
    override val PropertySetter  : Action<obj, obj>
    override val ShowForDisplay  : bool
    override val ShowForEdit  : bool
    override val SimpleDisplayProperty  : string
    override val TemplateHint  : string
    override val ValidateChildren  : bool
    override val ValidatorMetadata  : IReadOnlyList<obj>

[<Sealed>]
type internal FalcoEndpointAcceptsMetadata(
    type' : Type,
    contentTypes: string seq,
    required : bool) =
    inherit FalcoEndpointModelMetadata(
        Identity = ModelMetadataIdentity.ForType(type'),
        BindingSource = BindingSource.Body,
        DisplayName = "Request Body",
        IsRequired = required)
    member val ContentTypes = contentTypes
