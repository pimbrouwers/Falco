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
    let route verb pattern handler =
        all pattern [ verb, handler ]

    /// HttpEndpoint constructor that matches any HttpVerb.
    let any pattern handler =
        route ANY pattern handler

    /// GET HttpEndpoint constructor.
    let get pattern handler =
        route GET pattern handler

    /// HEAD HttpEndpoint constructor.
    let head pattern handler =
        route HEAD pattern handler

    /// POST HttpEndpoint constructor.
    let post pattern handler =
        route POST pattern handler

    /// PUT HttpEndpoint constructor.
    let put pattern handler =
        route PUT pattern handler

    /// PATCH HttpEndpoint constructor.
    let patch pattern handler =
        route PATCH pattern handler

    /// DELETE HttpEndpoint constructor.
    let delete pattern handler =
        route DELETE pattern handler

    /// OPTIONS HttpEndpoint constructor.
    let options pattern handler =
        route OPTIONS pattern handler

    /// TRACE HttpEndpoint construct.
    let trace pattern handler =
        route TRACE pattern handler

    /// HttpEndpoint constructor that matches any HttpVerb which maps the route
    /// using the provided `map` function.
    let mapAny pattern map handler =
        any pattern (Request.mapRoute map handler)

    /// GET HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapGet pattern map handler =
        get pattern (Request.mapRoute map handler)

    /// HEAD HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapHead pattern map handler =
        head pattern (Request.mapRoute map handler)

    /// POST HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapPost pattern map handler =
        post pattern (Request.mapRoute map handler)

    /// PUT HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapPut pattern map handler =
        put pattern (Request.mapRoute map handler)

    /// PATCH HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapPatch pattern map handler =
        patch pattern (Request.mapRoute map handler)

    /// DELETE HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapDelete pattern map handler =
        delete pattern (Request.mapRoute map handler)

    /// OPTIONS HttpEndpoint constructor which maps the route using the provided
    /// `map` function.
    let mapOptions pattern map handler =
        options pattern (Request.mapRoute map handler)

    /// TRACE HttpEndpoint construct which maps the route using the provided
    /// `map` function.
    let mapTrace pattern map handler =
        trace pattern (Request.mapRoute map handler)


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
    member this.FalcoGet(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ GET, handler ] })
        this

    member this.FalcoHead(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ HEAD, handler ] })
        this

    member this.FalcoPost(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ POST, handler ] })
        this

    member this.FalcoPut(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ PUT, handler ] })
        this

    member this.FalcoDelete(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ DELETE, handler ] })
        this

    member this.FalcoOptions(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ OPTIONS, handler ] })
        this

    member this.FalcoTrace(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ TRACE, handler ] })
        this

    member this.FalcoPatch(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ PATCH, handler ] })
        this

    member this.FalcoAny(pattern : string, handler : HttpHandler) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = [ ANY, handler ] })
        this

    member this.FalcoAll(pattern : string, handlers : (HttpVerb * HttpHandler) seq) : FalcoEndpointBuilder =
        dataSource.FalcoEndpoints.Add({
            Pattern = pattern
            Handlers = handlers })
        this
