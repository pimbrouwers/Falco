module Falco.Tests.Core

open System.Text
open Xunit
open Falco
open FSharp.Control.Tasks
open FsUnit.Xunit
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open NSubstitute

[<Fact>]
let ``GetService should throw on missing dependency``() =            
    let t = typeof<IAntiforgery>
    let ctx = Substitute.For<HttpContext>()
    ctx.RequestServices.GetService(t).Returns(null :> IAntiforgery) |> ignore

    (fun () -> ctx.GetService<IAntiforgery>() |> ignore)
    |> should throw typeof<InvalidDependencyException>

[<Fact>]
let ``GetService should return dependency``() =            
    let t = typeof<IAntiforgery>
    let ctx = Substitute.For<HttpContext>()
    ctx.RequestServices.GetService(t).Returns(Substitute.For<IAntiforgery>()) |> ignore

    ctx.GetService<IAntiforgery>()
    |> should be instanceOfType<IAntiforgery>

[<Fact>]
let ``WriteString writes to body and sets content length`` () =            
    let ctx = getHttpContextWriteable false
        
    let expected = "hello world"
        
    task {
        let! _ = ctx.Response.WriteString Encoding.UTF8 expected
        let! body = getBody ctx
        let contentLength = ctx.Response.ContentLength            

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
    }
    |> ignore


[<Fact>]
let ``RouteValue returns None for missing`` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary()
    (ctx.Request.TryGetRouteValue "name").IsNone |> should equal true

[<Fact>]
let ``RouteValue returns Some `` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
    let name = ctx.Request.TryGetRouteValue "name"            
    name.IsSome |> should equal true
    name        |> Option.iter (fun n -> n |> should equal "world")
     
[<Fact>]
let ``RouteValues returns entire route collection`` () =
    let ctx = Substitute.For<HttpContext>()
    ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
    let routeValues = ctx.Request.GetRouteValues()
    routeValues.Count    |> should equal 1
    routeValues.["name"] |> should equal "world"
