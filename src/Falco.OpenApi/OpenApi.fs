namespace Falco.OpenApi

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Mvc.ApiExplorer
open Microsoft.Extensions.DependencyInjection
open Falco

[<RequireQualifiedAccess>]
module OpenApi =
    let private contentTypeFromType (t : Type) =
        if t = typeof<string> then
            "text/plain"
        else
            "application/json"

    /// Specifies the name of the endpoint which corresponds to the OperationId
    /// in the OpenAPI specification.
    let name name endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointNameMetadata(name))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies a description of the endpoint which corresponds to the
    /// Description in the OpenAPI specification.
    let description description endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointDescriptionMetadata(description))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies a summary of the endpoint which corresponds to the Summary
    /// in the OpenAPI specification.
    let summary summary endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointSummaryMetadata(summary))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies tags for the endpoint which corresponds to the Tags in the
    /// OpenAPI specification.
    let tags tags endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointTagsMetadata(tags))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    type Accepts =
        { Input : Type
          ContentTypes : string seq
          Required : bool }

    /// Specifies the input type that the endpoint accepts. This corresponds to
    /// the RequestBody in the OpenAPI specification.
    let accepts accepts endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                FalcoEndpointAcceptsMetadata(
                    type' = accepts.Input,
                    required = accepts.Required,
                    contentTypes = accepts.ContentTypes))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies the input type that the endpoint accepts. This corresponds to
    /// the RequestBody in the OpenAPI specification.
    let acceptsType (t : Type) endpoint =
        accepts
            { Input = t
              ContentTypes = [ contentTypeFromType t ]
              Required = true }
            endpoint

    type Returns =
        { Return : Type
          ContentTypes : string seq
          Status : int }

    /// Specifies the return type of the endpoint. This corresponds to the
    /// Response in the OpenAPI specification.
    let returns (returns : Returns) endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                FalcoEndpointResponseMetadata(
                    type' = returns.Return,
                    statusCode = returns.Status,
                    contentTypes = returns.ContentTypes))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies the return type of the endpoint. This corresponds to the
    /// Response in the OpenAPI specification.
    let returnType (t : Type) endpoint =
        returns
            { Return = t
              ContentTypes = [ contentTypeFromType t ]
              Status = 200 }
            endpoint

    type Parameter =
        { Name : string
          Type : Type
          Required : bool }

    let private createParameterMetadata source param =
        FalcoEndpointParameterMetadata(
            source = source,
            type' = param.Type,
            name = param.Name,
            required = param.Required)

    /// Specifies a route parameter for the endpoint. This corresponds to the
    /// PathParameter in the OpenAPI specification.
    let route routeParams endpoint =
        let configure (x : EndpointBuilder) =
            for param in routeParams do
                x.Metadata.Add(createParameterMetadata PathParameter param)
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    /// Specifies a query parameter for the endpoint. This corresponds to the
    /// QueryParameter in the OpenAPI specification.
    let query queryParams endpoint =
        let configure (x : EndpointBuilder) =
            for param in queryParams do
                x.Metadata.Add(createParameterMetadata QueryParameter param)
            x

        { endpoint with Configure = endpoint.Configure >> configure }

[<AutoOpen>]
module FalcoOpenApiExtensions =
    type IServiceCollection with
        member this.AddFalcoOpenApi() : IServiceCollection =
            this.AddSingleton<FalcoEndpointDataSource>()
                .AddSingleton<IApiDescriptionProvider, FalcoApiDescriptionProvider>()
                .AddEndpointsApiExplorer()
