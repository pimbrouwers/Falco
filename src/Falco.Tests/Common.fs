[<AutoOpen>]
module Falco.Tests.Common 

open System.IO
open System.IO.Pipelines
open System.Security.Claims
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open NSubstitute

let getBody (ctx : HttpContext) =
    task {
        ctx.Response.Body.Position <- 0L
        use reader = new StreamReader(ctx.Response.Body)
        return! reader.ReadToEndAsync()
    }

let getHttpContextWriteable (authenticated : bool) =
    let headers = Substitute.For<HeaderDictionary>()
    
    let resp = Substitute.For<HttpResponse>()
    let str = new MemoryStream()
    resp.Headers.Returns(headers) |> ignore
    resp.BodyWriter.Returns(PipeWriter.Create(str)) |> ignore
    resp.Body <- str    
    resp.StatusCode <- 200 

    
    let identity = Substitute.For<ClaimsIdentity>()
    identity.IsAuthenticated.Returns(authenticated) |> ignore
    
    let user = Substitute.For<ClaimsPrincipal>()
    user.Identity.Returns(identity) |> ignore

    let ctx = Substitute.For<HttpContext>()    
    ctx.Response.Returns(resp) |> ignore
    ctx.User.Returns(user) |> ignore

    ctx