module Falco.Tests.Request

open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open NSubstitute
open Xunit
open Microsoft.AspNetCore.Routing
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open System.Security.Claims

[<Fact>]
let ``Request.getVerb should return HttpVerb from HttpContext`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.Method <- "GET"

    Request.getVerb ctx
    |> should equal GET

[<Fact>]
let ``Request.getHeader should work for present and missing header names`` () =
    let serverName = "Kestrel"
    let ctx = getHttpContextWriteable false
    ctx.Request.Headers.Add(HeaderNames.Server, StringValues(serverName))

    let headers =  Request.getHeaders ctx

    headers.GetString HeaderNames.Server "" |> should equal serverName
    headers.TryGetString "missing" |> should equal None

[<Fact>]
let ``Request.getRouteValues should return Map<string, string> from HttpContext`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    let route = Request.getRoute ctx

    route.GetString "name" ""
    |> should equal "falco"

[<Fact>]
let ``Request.ifAuthenticatedWithScope should invoke handleOk if authenticated with scope claim from issuer`` () =
    let ctx = getHttpContextWriteable true
    let claims = [
        Claim("sub", "123", "str", "issuer");
        Claim("scope", "read create", "str", "another-issuer")
    ]
    ctx.User.Claims.Returns(claims) |> ignore

    let handleOk = fun _ -> task { true |> should equal true } :> Task
    let handleError = fun _ -> task { true |> should equal false } :> Task

    task {
        do! Request.ifAuthenticatedWithScope "another-issuer" "create" handleOk handleError ctx
    }

[<Fact>]
let ``Request.ifAuthenticatedWithScope should invoke handleError if not authenticated`` () =
    let ctx = getHttpContextWriteable false
    let claims = [
        Claim("sub", "123", "str", "issuer");
        Claim("scope", "read create", "str", "another-issuer")
    ]
    ctx.User.Claims.Returns(claims) |> ignore

    let handleOk = fun _ -> task { true |> should equal false } :> Task
    let handleError = fun _ -> task { true |> should equal true } :> Task

    task {
        do! Request.ifAuthenticatedWithScope "another-issuer" "create" handleOk handleError ctx
    }

[<Fact>]
let ``Request.ifAuthenticatedWithScope should invoke handleError if authenticated with no scope claim from issuer`` () =
    let ctx = getHttpContextWriteable true
    let claims = [
        Claim("sub", "123", "str", "issuer");
        Claim("scope", "read create", "str", "another-issuer")
    ]
    ctx.User.Claims.Returns(claims) |> ignore

    let handleOk = fun _ -> task { true |> should equal false } :> Task
    let handleError = fun _ -> task { true |> should equal true } :> Task

    task {
        do! Request.ifAuthenticatedWithScope "issuer" "create" handleOk handleError ctx
    }

let handleOk fn v =
    fun ctx ->
        fn v
        true |> should equal true
        Response.ofEmpty ctx

let handleError _ =
    fun ctx ->
        false |> should equal true
        Response.ofEmpty ctx

[<Fact>]
let ``Request.mapJson`` () =
    let ctx = getHttpContextWriteable false
    use ms = new MemoryStream(Encoding.UTF8.GetBytes("{{\"name\":\"falco\"}"))
    ctx.Request.Body.Returns(ms) |> ignore

    let predicates j =
        j.Name |> should equal "falco"

    Request.mapJson (handleOk predicates)

[<Fact>]
let ``Request.mapRoute`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    let predicates name =
        name |> should equal "falco"

    Request.mapRoute (fun r -> r.GetString "name" "" |> Ok) (handleOk predicates)

let ``Request.mapQuery`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.Cookies <- Map.ofList ["name", "falco"] |> cookieCollection

    let predicates name =
        name |> should equal "falco"

    Request.mapCookie (fun q -> q.GetString "name" "" |> Ok) (handleOk predicates)

let ``Request.mapCookie`` () =
    let ctx = getHttpContextWriteable false
    let query = Dictionary<string, StringValues>()
    query.Add("name", StringValues("falco"))
    ctx.Request.Query <- QueryCollection(query)

    let predicates name =
        name |> should equal "falco"

    Request.mapQuery (fun c -> c.GetString "name" "" |> Ok) (handleOk predicates)

[<Fact>]
let ``Request.mapForm`` () =
    let ctx = getHttpContextWriteable false
    let form = Dictionary<string, StringValues>()
    form.Add("name", StringValues("falco"))
    ctx.Request.ReadFormAsync().Returns(FormCollection(form)) |> ignore

    let predicates name =
        name |> should equal "falco"

    Request.mapForm (fun f -> f.GetString "name" "" |> Ok) (handleOk predicates)

[<Fact>]
let ``Request.mapFormStream`` () =
    let ctx = getHttpContextWriteable false
    let form = Dictionary<string, StringValues>()
    form.Add("name", StringValues("falco"))
    ctx.Request.ReadFormAsync().Returns(FormCollection(form)) |> ignore

    let predicates name =
        name |> should equal "falco"

    Request.mapFormStream (fun f -> f.GetString "name" "" |> Ok) (handleOk predicates)