module Falco.Tests.Core

open System.Text
open Xunit
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
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