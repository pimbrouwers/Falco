[<AutoOpen>]
module Falco.Tests.Common 

open System.IO
open System.IO.Pipelines
open System.Security.Claims
open FSharp.Control.Tasks
open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open NSubstitute

[<CLIMutable>]
type FakeRecord = { Name : string }

let getResponseBody (ctx : HttpContext) =
    task {
        ctx.Response.Body.Position <- 0L
        use reader = new StreamReader(ctx.Response.Body)
        return! reader.ReadToEndAsync()
    }

let getHttpContextWriteable (authenticated : bool) =
    let req = Substitute.For<HttpRequest>()    
    req.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore
    
    let resp = Substitute.For<HttpResponse>()    
    let respBody = new MemoryStream()
    resp.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore
    resp.BodyWriter.Returns(PipeWriter.Create(respBody)) |> ignore
    resp.Body <- respBody    
    resp.StatusCode <- 200 

    
    let identity = Substitute.For<ClaimsIdentity>()
    identity.IsAuthenticated.Returns(authenticated) |> ignore
    
    let user = Substitute.For<ClaimsPrincipal>()
    user.Identity.Returns(identity) |> ignore

    let ctx = Substitute.For<HttpContext>()    
    ctx.Request.Returns(req) |> ignore
    ctx.Response.Returns(resp) |> ignore
    ctx.User.Returns(user) |> ignore

    ctx