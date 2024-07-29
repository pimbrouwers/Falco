namespace Falco

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.FileProviders
open Falco.StringUtils

[<AutoOpen>]
module Routing =
    /// Constructor for multi-method HttpEndpoint.
    let all
        (pattern : string)
        (handlers : (HttpVerb * HttpHandler) seq) : HttpEndpoint =
        { Pattern  = pattern; Handlers = handlers }

    /// Constructor for a singular HttpEndpoint.
    let route
        (verb : HttpVerb)
        (pattern : string)
        (handler : HttpHandler) : HttpEndpoint =
        all pattern [ verb, handler ]

    /// HttpEndpoint constructor that matches any HttpVerb.
    let any (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route ANY pattern handler

    /// GET HttpEndpoint constructor.
    let get (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route GET pattern handler

    /// HEAD HttpEndpoint constructor.
    let head (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route HEAD pattern handler

    /// POST HttpEndpoint constructor.
    let post (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route POST pattern handler

    /// PUT HttpEndpoint constructor.
    let put (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route PUT pattern handler

    /// PATCH HttpEndpoint constructor.
    let patch (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route PATCH pattern handler

    /// DELETE HttpEndpoint constructor.
    let delete (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route DELETE pattern handler

    /// OPTIONS HttpEndpoint constructor.
    let options (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route OPTIONS pattern handler

    /// TRACE HttpEndpoint construct.
    let trace (pattern : string) (handler : HttpHandler) : HttpEndpoint =
        route TRACE pattern handler

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint seq) =
    inherit EndpointDataSource()

    let conventions = List<Action<EndpointBuilder>>()

    interface IEndpointConventionBuilder with
        member _.Add(convention: Action<EndpointBuilder>) : unit =
            conventions.Add(convention)

    member val FalcoEndpoints = List<HttpEndpoint>()

    override x.Endpoints
        with get() = x.BuildEndpoints()

    override _.GetChangeToken() = NullChangeToken.Singleton

    member private x.BuildEndpoints () =
        let endpoints = List<Endpoint>()
        let mutable order = 0

        for endpoint in Seq.concat [ httpEndpoints; x.FalcoEndpoints ] do
            let routePattern = Patterns.RoutePatternFactory.Parse endpoint.Pattern

            for (verb, handler) in endpoint.Handlers do
                order <- order + 1

                let verbStr = verb.ToString()

                let displayName =
                    if strEmpty verbStr then endpoint.Pattern
                    else strConcat [|verbStr; " "; endpoint.Pattern|]

                let requestDelegate = HttpHandler.toRequestDelegate handler

                let endpointBuilder = RouteEndpointBuilder(requestDelegate, routePattern, order, DisplayName = displayName)
                endpointBuilder.DisplayName <- displayName

                for convention in conventions do
                    convention.Invoke(endpointBuilder)

                endpoints.Add(endpointBuilder.Build())

        endpoints


[<Sealed>]
type FalcoEndpointBuilder internal (dataSource : FalcoEndpointDatasource) =
    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP GET requests for the specified pattern.
    member this.FalcoGet(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ GET, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP HEAD requests for the specified pattern.
    member this.FalcoHead(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ HEAD, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP POST requests for the specified pattern.
    member this.FalcoPost(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ POST, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP PUT requests for the specified pattern.
    member this.FalcoPut(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ PUT, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP DELETE requests for the specified pattern.
    member this.FalcoDelete(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ DELETE, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP OPTIONS requests for the specified pattern.
    member this.FalcoOptions(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ OPTIONS, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP TRACE requests for the specified pattern.
    member this.FalcoTrace(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ TRACE, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP PATCH requests for the specified pattern.
    member this.FalcoPatch(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ PATCH, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches all HTTP requests for the specified pattern.
    member this.FalcoAny(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ ANY, handler ] })
        this

    /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches the provided HTTP requests for the specified pattern.
    member this.FalcoAll(pattern : string, handlers : (HttpVerb * HttpHandler) seq) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = handlers })
        this
