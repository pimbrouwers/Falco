module Falco.Tests.Core

open Xunit
open Falco
open FsUnit.Xunit
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open NSubstitute

[<Fact>]
let ``GetService should return dependency``() =
    let t = typeof<IAntiforgery>
    let ctx = Substitute.For<HttpContext>()
    ctx.RequestServices.GetService(t).Returns(Substitute.For<IAntiforgery>()) |> ignore

    ctx.GetService<IAntiforgery>()
    |> should be instanceOfType<IAntiforgery>