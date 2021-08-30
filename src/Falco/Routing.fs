module Falco.Routing

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.FileProviders
open Falco.StringUtils

/// The process of associating a route and handler
type MapHttpEndpoint = string -> HttpHandler -> HttpEndpoint

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint list) =
    inherit EndpointDataSource()

    [<Literal>]
    let defaultOrder = 0

    let endpoints =
        [| for endpoint in httpEndpoints do
            let routePattern = Patterns.RoutePatternFactory.Parse endpoint.Pattern

            for (verb, handler) in endpoint.Handlers do
                let requestDelegate = HttpHandler.toRequestDelegate handler
                let verbStr = verb.ToString()
                let displayName = if strEmpty verbStr then endpoint.Pattern else strConcat [|verbStr; " "; endpoint.Pattern|]
                let httpMethod = HttpVerb.toHttpMethodMetadata verb
                let metadata = EndpointMetadataCollection(httpMethod)
                RouteEndpoint(requestDelegate, routePattern, defaultOrder, metadata, displayName) :> Endpoint |]

    override _.Endpoints = endpoints :> _
    override _.GetChangeToken() = NullChangeToken.Singleton :> _

/// Constructor for multi-method HttpEndpoint
let all (pattern : string) (handlers : (HttpVerb * HttpHandler) list) : HttpEndpoint =
    { Pattern  = pattern; Handlers = handlers  }

/// Constructor for a singular HttpEndpoint
let route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) : HttpEndpoint =
    all pattern [ verb, handler ]

/// HttpEndpoint constructor that matches any HttpVerb
let any : MapHttpEndpoint = route ANY

/// GET HttpEndpoint constructor
let get : MapHttpEndpoint = route GET

/// HEAD HttpEndpoint constructor
let head : MapHttpEndpoint = route HEAD

/// POST HttpEndpoint constructor
let post : MapHttpEndpoint = route POST

/// PUT HttpEndpoint constructor
let put : MapHttpEndpoint = route PUT

/// PATCH HttpEndpoint constructor
let patch : MapHttpEndpoint = route PATCH

/// DELETE HttpEndpoint constructor
let delete : MapHttpEndpoint = route DELETE

/// OPTIONS HttpEndpoint constructor
let options : MapHttpEndpoint = route OPTIONS

/// TRACE HttpEndpoint construct
let trace : MapHttpEndpoint = route TRACE