module Falco.Routing

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.FileProviders
open Falco.StringUtils

/// The process of associating a route and handler.
type MapHttpEndpoint = string -> HttpHandler -> HttpEndpoint

module HttpVerb =
    let toHttpMethodMetadata verb =
        let verbStr = verb.ToString()
        match verb with
        | ANY -> HttpMethodMetadata [||]
        | _   -> HttpMethodMetadata [|verbStr|]

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint list) =
    inherit EndpointDataSource()

    [<Literal>]
    let DefaultOrder = 0

    let endpoints =
        [| for endpoint in httpEndpoints do
            let routePattern = Patterns.RoutePatternFactory.Parse endpoint.Pattern

            for (verb, handler) in endpoint.Handlers do
                let routeNameMetadata = RouteNameMetadata(endpoint.Pattern)

                let verbStr = verb.ToString()
                let displayName = if strEmpty verbStr then endpoint.Pattern else strConcat [|verbStr; " "; endpoint.Pattern|]
                let httpMethodMetadata = HttpVerb.toHttpMethodMetadata verb

                let metadata = EndpointMetadataCollection(routeNameMetadata, httpMethodMetadata)

                let requestDelegate = HttpHandler.toRequestDelegate handler

                RouteEndpoint(requestDelegate, routePattern, DefaultOrder, metadata, displayName) :> Endpoint |]

    override _.Endpoints = endpoints :> _
    override _.GetChangeToken() = NullChangeToken.Singleton :> _

/// Constructor for multi-method HttpEndpoint.
let all (pattern : string) (handlers : (HttpVerb * HttpHandler) list) : HttpEndpoint =
    { Pattern  = pattern; Handlers = handlers }

/// Constructor for a singular HttpEndpoint.
let route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) : HttpEndpoint =
    all pattern [ verb, handler ]

/// HttpEndpoint constructor that matches any HttpVerb.
let any : MapHttpEndpoint = route ANY

/// GET HttpEndpoint constructor.
let get : MapHttpEndpoint = route GET

/// HEAD HttpEndpoint constructor.
let head : MapHttpEndpoint = route HEAD

/// POST HttpEndpoint constructor.
let post : MapHttpEndpoint = route POST

/// PUT HttpEndpoint constructor.
let put : MapHttpEndpoint = route PUT

/// PATCH HttpEndpoint constructor.
let patch : MapHttpEndpoint = route PATCH

/// DELETE HttpEndpoint constructor.
let delete : MapHttpEndpoint = route DELETE

/// OPTIONS HttpEndpoint constructor.
let options : MapHttpEndpoint = route OPTIONS

/// TRACE HttpEndpoint construct.
let trace : MapHttpEndpoint = route TRACE