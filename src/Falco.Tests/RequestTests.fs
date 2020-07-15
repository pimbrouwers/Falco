module Falco.Tests.Request

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open Falco
open FSharp.Control.Tasks
open FsUnit.Xunit
open NSubstitute
open Xunit
open Microsoft.AspNetCore.Routing
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

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
    
    Request.getHeader HeaderNames.Server ctx
    |> should equal [|serverName|]

    Request.getHeader "missing" ctx
    |> should equal [||]

[<Fact>]
let ``Request.getRouteValues should return Map<string, string> from HttpContext`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    let routeValues = Request.getRouteValues ctx

    routeValues.Item("name") 
    |> should equal "falco"

[<Fact>]
let ``Request.tryGetRouteValue should return Some`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    Request.tryGetRouteValue "name" ctx
    |> should equal (Some "falco")

[<Fact>]
let ``Request.tryGetRouteValue should return None`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    Request.tryGetRouteValue "nane" ctx
    |> should equal None

[<Fact>]
let ``Request.tryBindQuery should bind record successfully`` () =
    let ctx = getHttpContextWriteable false
    let query = Dictionary<string, StringValues>()
    query.Add("name", StringValues("falco"))
    ctx.Request.Query <- QueryCollection(query)

    let bind = 
        fun (rd : StringCollectionReader) -> 
            match rd.TryGetString "name" with
            | None -> Error "name not found"
            | Some name -> Ok { Name = name }

    let boundRecord = Request.tryBindQuery bind ctx
    
    match boundRecord with
    | Error _    -> false 
                   |> should equal true

    | Ok record -> record.Name 
                   |> should equal "falco"

[<Fact>]
let ``Request.tryBindForm should return a FormCollectionReader instance`` () =
    let ctx = getHttpContextWriteable false
    let form = Dictionary<string, StringValues>()
    form.Add("name", StringValues("falco"))
    ctx.Request.Query <- QueryCollection(form)
  
    ctx.Request.ReadFormAsync().Returns(FormCollection(form)) |> ignore

    let bind = 
        fun (rd : FormCollectionReader) -> 
            match rd.TryGetString "name" with
            | None -> Error "name not found"
            | Some name -> Ok { Name = name }

    task {
        let! boundRecord = Request.tryBindForm bind ctx
        
        match boundRecord with
        | Error _   -> false 
                       |> should equal true

        | Ok record -> record.Name 
                       |> should equal "falco"
    }

[<Fact>]
let ``Request.tryBindJson should return deserialzed FakeRecord record `` () =
    let ctx = getHttpContextWriteable false
    use ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":\"falco\"}"))    
    ctx.Request.Body.Returns(ms) |> ignore

    task {
        let! boundRecord = Request.tryBindJson<FakeRecord> ctx

        match boundRecord with
        | Error _   -> false 
                       |> should equal true

        | Ok record -> record.Name 
                       |> should equal "falco"
    }

[<Fact>]
let ``Request.tryBindJsonAsync should return Error on failure`` () =
    let ctx = getHttpContextWriteable false
    use ms = new MemoryStream(Encoding.UTF8.GetBytes("{{\"name\":\"falco\"}"))    
    ctx.Request.Body.Returns(ms) |> ignore

    task {
        let! boundRecord = Request.tryBindJson<FakeRecord> ctx

        match boundRecord with
        | Error error -> String.IsNullOrWhiteSpace(error)
                         |> should equal false

        | Ok _        -> false 
                         |> should equal true
    }

[<Fact>]
let ``Request.tryBindJsonAsyncOptions should return empty record `` () =
    let ctx = getHttpContextWriteable false
    use ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":null}"))    
    ctx.Request.Body.Returns(ms) |> ignore

    task {
        let jsonOptions = JsonSerializerOptions()
        jsonOptions.IgnoreNullValues <- true
        jsonOptions.PropertyNameCaseInsensitive <- false

        let! boundRecord = Request.tryBindJsonOptions<FakeRecord> jsonOptions ctx

        match boundRecord with
        | Error _   -> false 
                       |> should equal true

        | Ok record -> record.Name 
                       |> should be null
    }