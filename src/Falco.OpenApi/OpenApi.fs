module Falco.OpenApi

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Routing
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Metadata
open Microsoft.AspNetCore.Mvc.Abstractions
open Microsoft.AspNetCore.Mvc.ApiExplorer
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
open Microsoft.Extensions.DependencyInjection

[<RequireQualifiedAccess>]
module OpenApi =
    type Accepts =
        { Input : Type
          ContentTypes : string seq
          Optional : bool }

    type Returns =
        { Return : Type
          ContentTypes : string seq
          Status : int }

    let private contentTypeFromType (t : Type) =
        if t = typeof<string> then
            "text/plain"
        else
            "application/json"

    let name name endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(EndpointNameMetadata(name))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let description description endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(EndpointDescriptionAttribute(description))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let accepts (accepts : Accepts) (endpoint : HttpEndpoint) =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                AcceptsMetadata(
                    ``type`` = accepts.Input,
                    contentTypes = Array.ofSeq accepts.ContentTypes,
                    isOptional = accepts.Optional))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let acceptsType (t : Type) endpoint =
        accepts
            { Input = t
              ContentTypes = [ contentTypeFromType t ]
              Optional = false }
            endpoint

    let returns (returns : Returns) endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                ProducesResponseTypeMetadata(
                    ``type`` = returns.Return,
                    statusCode = returns.Status,
                    contentTypes = Array.ofSeq returns.ContentTypes))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let returnType (t : Type) endpoint =
        returns
            { Return = t
              ContentTypes = [ contentTypeFromType t ]
              Status = 200 }
            endpoint

[<Sealed>]
type internal FalcoEndpointModelMetadata(
    Identity : ModelMetadataIdentity,
    BindingSource : BindingSource) =
    inherit ModelMetadata(Identity)

    member val ModelType : Type = Identity.ModelType
    override val AdditionalValues : IReadOnlyDictionary<obj, obj>
    override val BinderModelName : string
    override val BinderType  : Type
    override val BindingSource  : BindingSource = BindingSource
    override val ConvertEmptyStringToNull  : bool
    override val DataTypeName  : string
    override val Description  : string
    override val DisplayFormatString  : string
    override val DisplayName  : string
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
    override val IsRequired  : bool
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
type internal FalcoApiDescriptionProvider (dataSource : FalcoEndpointDataSource) =
    let createApiDescriptions (endpoint : RouteEndpoint) =
        let httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()
        ArgumentNullException.ThrowIfNull httpMethodMetadata

        let endpointName = endpoint.Metadata.GetMetadata<EndpointNameMetadata>()
        let endpointDescription = endpoint.Metadata.GetMetadata<EndpointDescriptionAttribute>()
        let acceptsTypeMetadata = endpoint.Metadata.GetOrderedMetadata<AcceptsMetadata>()
        let responseTypeMetadata = endpoint.Metadata.GetOrderedMetadata<ProducesResponseTypeMetadata>()
        let createApiDescription httpMethod =
            let name =
                if isNull endpointName then
                    // TODO what is the default name?
                    "imhere"
                else
                    endpointName.EndpointName

            let displayName =
                if isNull endpointDescription then
                    endpoint.DisplayName
                else
                    endpointDescription.Description

            let apiDescription =
                ApiDescription(
                    HttpMethod = httpMethod,
                    RelativePath = endpoint.RoutePattern.RawText.TrimStart('/'),
                    ActionDescriptor = ActionDescriptor(
                        DisplayName = displayName,
                        RouteValues = dict [ "controller", name ]))

            apiDescription.Properties.Add("description", "test")

            for accepts in acceptsTypeMetadata do
                apiDescription.ParameterDescriptions.Add(
                    ApiParameterDescription(
                        Name = "api-parameter-description",
                        ModelMetadata = FalcoEndpointModelMetadata(
                            Identity = ModelMetadataIdentity.ForType(accepts.RequestType),
                            BindingSource = BindingSource.Body),
                        Source = BindingSource.Body,
                        DefaultValue = null,
                        Type = accepts.RequestType))

                for contentType in accepts.ContentTypes do
                    apiDescription.SupportedRequestFormats.Add(
                        ApiRequestFormat(
                            MediaType = contentType))

            for responseType in responseTypeMetadata do
                for contentType in responseType.ContentTypes do
                    let responseFormats = ResizeArray<ApiResponseFormat>()
                    responseFormats.Add(ApiResponseFormat(MediaType = contentType))
                    apiDescription.SupportedResponseTypes.Add(
                    ApiResponseType(
                        Type = responseType.Type,
                        StatusCode = responseType.StatusCode,
                        ApiResponseFormats = responseFormats))

            apiDescription

        seq {
            for httpMethod in httpMethodMetadata.HttpMethods do
                createApiDescription httpMethod
        }

    interface IApiDescriptionProvider with
        member _.Order = 0

        member _.OnProvidersExecuting(context: ApiDescriptionProviderContext) =
            for endpoint in dataSource.Endpoints do
                match endpoint with
                | :? RouteEndpoint as endpoint ->
                    for apiDescription in createApiDescriptions endpoint do
                        context.Results.Add(apiDescription)
                | _ ->
                    ()

        member _.OnProvidersExecuted(_: ApiDescriptionProviderContext) =
            ()

[<AutoOpen>]
module Extensions =
    type IServiceCollection with
        member this.AddFalcoOpenApi() : IServiceCollection =
            this.AddSingleton<FalcoEndpointDataSource>()
                .AddSingleton<IApiDescriptionProvider, FalcoApiDescriptionProvider>()
                .AddEndpointsApiExplorer()
