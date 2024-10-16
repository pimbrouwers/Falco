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

    let name name endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointNameMetadata(name))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let description description endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(FalcoEndpointDescriptionMetadata(description))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    type Accepts =
        { Input : Type
          ContentTypes : string seq
          Required : bool }

    let accepts accepts endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                FalcoEndpointAcceptsMetadata(
                    type' = accepts.Input,
                    required = accepts.Required,
                    contentTypes = accepts.ContentTypes))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let acceptsType (t : Type) endpoint =
        accepts
            { Input = t
              ContentTypes = [ contentTypeFromType t ]
              Required = false }
            endpoint


    type Returns =
        { Return : Type
          ContentTypes : string seq
          Status : int }

    let returns (returns : Returns) endpoint =
        let configure (x : EndpointBuilder) =
            x.Metadata.Add(
                FalcoEndpointResponseMetadata(
                    type' = returns.Return,
                    statusCode = returns.Status,
                    contentTypes = returns.ContentTypes))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

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

    let routeParam routeParams endpoint =
        let configure (x : EndpointBuilder) =
            for param in routeParams do
                x.Metadata.Add(
                    FalcoEndpointRouteMetadata(
                        type' = param.Type,
                        name = param.Name,
                        required = param.Required))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

    let queryParam queryParams endpoint =
        let configure (x : EndpointBuilder) =
            for param in queryParams do
                x.Metadata.Add(
                    FalcoEndpointQueryMetadata(
                        type' = param.Type,
                        name = param.Name,
                        required = param.Required))
            x

        { endpoint with Configure = endpoint.Configure >> configure }

[<AutoOpen>]
module Extensions =
    type IServiceCollection with
        member this.AddFalcoOpenApi() : IServiceCollection =
            this.AddSingleton<FalcoEndpointDataSource>()
                .AddSingleton<IApiDescriptionProvider, FalcoApiDescriptionProvider>()
                .AddEndpointsApiExplorer()
