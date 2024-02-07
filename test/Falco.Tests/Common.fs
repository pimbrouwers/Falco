[<AutoOpen>]
module Falco.Tests.Common

open System
open System.IO
open System.IO.Pipelines
open System.Security.Claims
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Net.Http.Headers
open NSubstitute
open System.Collections.Generic

let shouldBeSome pred (option : Option<'a>) =
    match option with
    | Some o -> pred o
    | None   -> sprintf "Should not be None" |> should equal false

let shouldBeNone (option : Option<'a>) =
    match option with
    | Some o -> sprintf "Should not be Some" |> should equal false
    | None   -> ()

[<CLIMutable>]
type FakeRecord = { Name : string }

let getResponseBody (ctx : HttpContext) =
    task {
        ctx.Response.Body.Position <- 0L
        use reader = new StreamReader(ctx.Response.Body)
        return! reader.ReadToEndAsync()
    }


[<Literal>]
let AuthScheme = "Testing"

type TestingHandlerOptions() =
  inherit AuthenticationSchemeOptions()

type TestingHandler(options, logger, encoder, clock) =
  inherit AuthenticationHandler<TestingHandlerOptions>(options, logger, encoder, clock)

  override _.HandleAuthenticateAsync() =
      Task.FromResult(AuthenticateResult.NoResult())

  override me.HandleChallengeAsync(properties) =
      me.Context.Response.StatusCode <- 401
      me.Context.Response.Headers.SetCommaSeparatedValues(HeaderNames.WWWAuthenticate, AuthScheme)
      me.Context.Response.Headers.Add(HeaderNames.Location, properties.RedirectUri)
      Task.CompletedTask

let getHttpContextWriteable (authenticated : bool) =
    let ctx = Substitute.For<HttpContext>()

    let req = Substitute.For<HttpRequest>()
    req.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore

    let resp = Substitute.For<HttpResponse>()
    let respBody = new MemoryStream()

    resp.Headers.Returns(Substitute.For<HeaderDictionary>()) |> ignore
    resp.BodyWriter.Returns(PipeWriter.Create(respBody)) |> ignore
    resp.Body <- respBody
    
    let serviceCollection = ServiceCollection()

    let antiforgery = Substitute.For<IAntiforgery>()
    antiforgery.IsRequestValidAsync(ctx).Returns(Task.FromResult(true)) |> ignore

    serviceCollection
        .AddLogging()
        .AddAuthentication()
        .AddScheme<TestingHandlerOptions, TestingHandler>(AuthScheme, ignore)
        .Services.AddSingleton<IAntiforgery>(antiforgery)
        |> ignore

    let provider = serviceCollection.BuildServiceProvider()

    let identity = Substitute.For<ClaimsIdentity>()
    identity.IsAuthenticated.Returns(authenticated) |> ignore

    let user = Substitute.For<ClaimsPrincipal>()
    user.Identity.Returns(identity) |> ignore

    ctx.Request.Returns(req) |> ignore
    ctx.Response.Returns(resp) |> ignore
    ctx.RequestServices.Returns(provider) |> ignore
    ctx.User.Returns(user) |> ignore

    ctx

let cookieCollection cookies =
  { new IRequestCookieCollection with
    member __.ContainsKey(key: string) = Map.containsKey key cookies
    member __.Count = Map.count cookies
    member __.GetEnumerator() = (Map.toSeq cookies |> Seq.map KeyValuePair).GetEnumerator()
    member __.GetEnumerator() = __.GetEnumerator() :> Collections.IEnumerator
    member __.Item with get (key: string): string = Map.find key cookies
    member __.Keys = Map.toSeq cookies |> Seq.map fst |> ResizeArray :> Collections.Generic.ICollection<string>
    member __.TryGetValue(key: string, value: byref<string>): bool =
      match Map.tryFind key cookies with
      | Some _ -> true
      | _ -> false }
