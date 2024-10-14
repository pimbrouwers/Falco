module Falco.OpenApi

open System
open Microsoft.AspNetCore.Routing
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Metadata
open Microsoft.AspNetCore.Mvc.Abstractions
open Microsoft.AspNetCore.Mvc.ApiExplorer
open Microsoft.OpenApi.Models
open Microsoft.Extensions.DependencyInjection

module OpenApi =
    type Accepts =
        { Input : Type
          ContentTypes : string seq
          Optional : bool }

    type Returns =
        { Return : Type
          ContentTypes : string seq
          Status : int }

    let name name endpoint =
        let configure (x : IEndpointConventionBuilder) =
            x.WithMetadata(EndpointNameMetadata(name))

        { endpoint with Configure = endpoint.Configure >> configure }

    let description description endpoint =
        let configure (x : IEndpointConventionBuilder) =
            x.WithMetadata(EndpointDescriptionAttribute(description))

        { endpoint with Configure = endpoint.Configure >> configure }

    let accepts (accepts : Accepts) endpoint =
        let configure (x : IEndpointConventionBuilder) =
            x.WithMetadata(
                AcceptsMetadata(
                    ``type`` = accepts.Input,
                    contentTypes = Array.ofSeq accepts.ContentTypes,
                    isOptional = accepts.Optional))

        { endpoint with Configure = endpoint.Configure >> configure }

    let returns (returns : Returns) endpoint =
        let configure (x : IEndpointConventionBuilder) =
            x.WithMetadata(
                ProducesResponseTypeMetadata(
                    ``type`` = returns.Return,
                    statusCode = returns.Status,
                    contentTypes = Array.ofSeq returns.ContentTypes))

        { endpoint with Configure = endpoint.Configure >> configure }

    let returnType (t : Type) endpoint =
        let contentType = if t = typeof<string> then "text/plain" else "application/json"
        returns { Return = t; ContentTypes = [ contentType ]; Status = 200 } endpoint

    let configure (op : OpenApiOperation -> Unit) endpoint =
        let configure (x : IEndpointConventionBuilder) =
            x.WithOpenApi(fun o ->
                op o
                o)
        { endpoint with Configure = endpoint.Configure >> configure }

[<Sealed>]
type FalcoApiDescriptionProvider (dataSource : FalcoEndpointDataSource) =
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
                for contentType in accepts.ContentTypes do
                    apiDescription.SupportedRequestFormats.Add(
                        ApiRequestFormat(MediaType = contentType))

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
            this.AddSingleton<FalcoEndpointDataSource>(FalcoEndpointDataSource([]))
                .AddSingleton<IApiDescriptionProvider, FalcoApiDescriptionProvider>()
                .AddEndpointsApiExplorer()
