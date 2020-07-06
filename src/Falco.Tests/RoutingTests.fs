module Falco.Tests.Routing

open Xunit
open Falco
open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open NSubstitute

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

[<Fact>]
let ``RouteValue returns None for missing`` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary()
    (ctx.TryGetRouteValue "name").IsNone |> should equal true

[<Fact>]
let ``RouteValue returns Some `` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
    let name = ctx.TryGetRouteValue "name"            
    name.IsSome |> should equal true
    name        |> Option.iter (fun n -> n |> should equal "world")
     
[<Fact>]
let ``RouteValues returns entire route collection`` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
    let routeValues = ctx.GetRouteValues()
    routeValues.Count    |> should equal 1
    routeValues.["name"] |> should equal "world"
