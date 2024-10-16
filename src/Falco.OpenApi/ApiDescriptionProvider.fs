namespace Falco.OpenApi

open System
open Microsoft.AspNetCore.Routing
open Falco
open Microsoft.AspNetCore.Mvc.Abstractions
open Microsoft.AspNetCore.Mvc.ApiExplorer
open Microsoft.AspNetCore.Mvc.ModelBinding

[<Sealed>]
type internal FalcoApiDescriptionProvider (dataSource : FalcoEndpointDataSource) =
    let createApiDescriptions (endpoint : RouteEndpoint) =
        let httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()
        ArgumentNullException.ThrowIfNull httpMethodMetadata

        let endpointName = endpoint.Metadata.GetMetadata<FalcoEndpointNameMetadata>()
        let endpointDescription = endpoint.Metadata.GetMetadata<FalcoEndpointDescriptionMetadata>()
        let acceptsMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointAcceptsMetadata>()
        let responseTypeMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointResponseMetadata>()
        let routeParamsMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointRouteMetadata>()
        let queryParamsMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointQueryMetadata>()

        let createApiDescription httpMethod =
            let apiDescription =
                ApiDescription(
                    HttpMethod = httpMethod,
                    RelativePath = endpoint.RoutePattern.RawText.TrimStart('/'),
                    ActionDescriptor = ActionDescriptor(
                        DisplayName = (if isNull endpointDescription then endpoint.DisplayName else endpointDescription.Description),
                        RouteValues = dict [ "controller", if isNull endpointName then endpoint.DisplayName else endpointName.Name ]))

            // request body
            for param in acceptsMetadata do
                apiDescription.ParameterDescriptions.Add(
                    ApiParameterDescription(
                        ModelMetadata = param,
                        Source = param.BindingSource,
                        DefaultValue = null,
                        Type = param.ModelType,
                        Name = param.Name,
                        IsRequired = param.IsRequired))

                for contentType in param.ContentTypes do
                    apiDescription.SupportedRequestFormats.Add(
                        ApiRequestFormat(
                            MediaType = contentType))

            // route params
            for param in routeParamsMetadata do
                apiDescription.ParameterDescriptions.Add(
                    ApiParameterDescription(
                        Source = BindingSource.Path,
                        DefaultValue = null,
                        Type = param.Type,
                        Name = param.Name,
                        IsRequired = param.Required))

            // query params
            for param in queryParamsMetadata do
                apiDescription.ParameterDescriptions.Add(
                    ApiParameterDescription(
                        Source = BindingSource.Query,
                        DefaultValue = null,
                        Type = param.Type,
                        Name = param.Name,
                        IsRequired = param.Required))

            // response
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
