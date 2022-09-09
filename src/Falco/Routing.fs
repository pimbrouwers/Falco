namespace Falco

module Routing =
    /// Constructor for multi-method HttpEndpoint.
    let all
        (pattern : string)
        (handlers : (HttpVerb * HttpHandler) list) : HttpEndpoint =
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
