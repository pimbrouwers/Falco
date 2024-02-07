namespace Falco

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.FileProviders
open Falco.StringUtils

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint seq) =
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
                let httpMethodMetadata =
                    match verb with
                    | ANY -> HttpMethodMetadata [||]
                    | _   -> HttpMethodMetadata [|verbStr|]

                let metadata = EndpointMetadataCollection(routeNameMetadata, httpMethodMetadata)

                let requestDelegate = HttpHandler.toRequestDelegate handler

                RouteEndpoint(requestDelegate, routePattern, DefaultOrder, metadata, displayName) :> Endpoint |]

    override _.Endpoints = endpoints :> _

    override _.GetChangeToken() = NullChangeToken.Singleton :> _


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
