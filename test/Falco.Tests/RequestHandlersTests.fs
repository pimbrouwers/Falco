module Falco.Tests.RequestHandlers

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open NSubstitute
open Xunit
open Microsoft.AspNetCore.Routing
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

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