[<AutoOpen>]
module Falco.Routing

/// Constructor for HttpEndpoint
let route 
    (verb : HttpVerb) 
    (pattern : string) 
    (handler : HttpHandler) : HttpEndpoint =
    { 
        Pattern = pattern
        Verb  = verb
        Handler = handler
    }

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