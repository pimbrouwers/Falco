module Falco.Tests.Routing

open Xunit
open Falco
open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open NSubstitute

[<Fact>]
let ``can create RequestDelegate from HttpHandler`` () =
    let handler = textOut "hello"
    handler
    |> createRequestDelete
    |> should be ofExactType<RequestDelegate>

[<Fact>]
let ``can create RequestDelegate from composed HttpHandler's`` () =
    let handler = setStatusCode 403 >=> textOut "forbidden"
    handler
    |> createRequestDelete
    |> should be ofExactType<RequestDelegate>
 
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
