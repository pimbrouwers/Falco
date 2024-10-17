namespace Falco.OpenApi

open System
open Microsoft.AspNetCore.Http.Metadata
open Microsoft.AspNetCore.Mvc.Abstractions
open Microsoft.AspNetCore.Mvc.ApiExplorer
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Routing
open Falco

[<Sealed>]
type internal FalcoApiDescriptionProvider (dataSource : FalcoEndpointDataSource) =
    let createActionDescriptor (endpoint : RouteEndpoint) =
        let endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()
        let endpointDescription = endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()
        let endpointSummary = endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()
        let endpointTags = endpoint.Metadata.GetMetadata<ITagsMetadata>()

        let descriptor =
            ActionDescriptor(
                DisplayName = (if isNull endpointDescription then endpoint.DisplayName else endpointDescription.Description),
                RouteValues = dict [
                    "controller", if isNull endpointName then endpoint.DisplayName else endpointName.EndpointName
                    "action", if isNull endpointDescription then endpoint.DisplayName else endpointDescription.Description ],
                EndpointMetadata = new ResizeArray<obj>())

        if not (isNull endpointName) then
            descriptor.EndpointMetadata.Add(endpointName)

        if not (isNull endpointDescription) then
            descriptor.EndpointMetadata.Add(endpointDescription)

        if not (isNull endpointSummary) then
            descriptor.EndpointMetadata.Add(endpointSummary)

        if not (isNull endpointTags) then
            descriptor.EndpointMetadata.Add(endpointTags)

        descriptor

    let createApiRequestBody (param : FalcoEndpointAcceptsMetadata) =
        ApiParameterDescription(
            ModelMetadata = param,
            Source = param.BindingSource,
            Type = param.ModelType,
            Name = param.Name,
            IsRequired = param.IsRequired)

    let createApiResponse (responseType : FalcoEndpointResponseMetadata) contentType =
        let responseFormats = ResizeArray<ApiResponseFormat>()
        responseFormats.Add(ApiResponseFormat(MediaType = contentType))
        ApiResponseType(
            Type = responseType.Type,
            StatusCode = responseType.StatusCode,
            ApiResponseFormats = responseFormats)

    let createApiParameter (param : FalcoEndpointParameterMetadata) =
        let source =
            match param.Source with
            | PathParameter -> BindingSource.Path
            | QueryParameter -> BindingSource.Query

        ApiParameterDescription(
            Source = source,
            Type = param.Type,
            Name = param.Name,
            IsRequired = param.Required)

    let createApiDescription httpMethod (endpoint : RouteEndpoint) =
        let apiDescription =
            ApiDescription(
                ActionDescriptor = createActionDescriptor endpoint,
                HttpMethod = httpMethod,
                RelativePath = endpoint.RoutePattern.RawText.TrimStart('/'))

        // request body
        let acceptsMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointAcceptsMetadata>()
        for param in acceptsMetadata do
            apiDescription.ParameterDescriptions.Add(createApiRequestBody param)

            for contentType in param.ContentTypes do
                apiDescription.SupportedRequestFormats.Add(
                    ApiRequestFormat(MediaType = contentType))

        // route, query and header params
        let routeParamsMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointParameterMetadata>()
        for param in routeParamsMetadata do
            apiDescription.ParameterDescriptions.Add(createApiParameter param)

        // response
        let responseTypeMetadata = endpoint.Metadata.GetOrderedMetadata<FalcoEndpointResponseMetadata>()
        for responseType in responseTypeMetadata do
            for contentType in responseType.ContentTypes do
                apiDescription.SupportedResponseTypes.Add(createApiResponse responseType contentType)

        apiDescription

    let createApiDescriptions (endpoint : RouteEndpoint) =
        let httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()
        ArgumentNullException.ThrowIfNull httpMethodMetadata

        seq {
            for httpMethod in httpMethodMetadata.HttpMethods do
                createApiDescription httpMethod endpoint
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
