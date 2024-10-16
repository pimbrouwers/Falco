namespace Falco.OpenApi

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Mvc.ModelBinding.Metadata

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointNameMetadata(name) =
    member val Name : string = name

[<AllowNullLiteral>]
[<Sealed>]
type internal FalcoEndpointDescriptionMetadata(description) =
    member val Description : string = description


[<Sealed>]
type internal FalcoEndpointRouteMetadata(
    type' : Type,
    name : string,
    required : bool) =
    member val Type : Type = type'
    member val Name : string = name
    member val Required : bool = required

[<Sealed>]
type internal FalcoEndpointQueryMetadata(
    type' : Type,
    name : string,
    required : bool) =
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
