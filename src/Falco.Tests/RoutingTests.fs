module Falco.Tests.Routing

open Xunit
open Falco
open FsUnit.Xunit

let emptyHandler : HttpHandler = Response.ofPlainText ""

[<Fact>]
let ``route function should return valid HttpEndpoint`` () =    
    let routeVerb = GET
    let routePattern = "/"
    let endpoint = route routeVerb routePattern emptyHandler
    
    endpoint.Verb    |> should equal routeVerb
    endpoint.Pattern |> should equal routePattern
    endpoint.Handler |> should be instanceOfType<HttpHandler>

let testEndpointFunction 
    (fn : MapHttpEndpoint)
    (verb : HttpVerb) =
    let pattern = "/"
    let endpoint = fn pattern emptyHandler
    endpoint.Pattern |> should equal pattern
    endpoint.Verb    |> should equal verb

[<Fact>]
let ``any function returns HttpEndpoint matching ANY HttpVerb`` () = 
    [
        any, ANY
        get, GET
        head, HEAD
        post, POST
        put, PUT
        patch, PATCH
        delete, DELETE
        options, OPTIONS
        trace, TRACE
    ]
    |> List.iter (fun (fn, verb) -> testEndpointFunction fn verb)

