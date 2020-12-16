module Falco.Tests.Request

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
let ``Request.tryBindJson should return Error on failure`` () =
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
let ``Request.tryBindJsonOptions should return empty record `` () =
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

[<Fact>]
let ``Request.ifAuthenticatedWithScope should invoke handleOk if authenticated with scope`` () =
    let ctx = getHttpContextWriteable true
    let claims = [
        Claim("sub", "123", "str", "issuer");
        Claim("scope", "read create", "str", "another-issuer")
    ]
    ctx.User.Claims.Returns(claims) |> ignore

    let handleOk = fun _ -> task { true |> should equal true }
    let handleError = fun _ -> task { true |> should equal false }

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

    let handleOk = fun _ -> task { true |> should equal false }
    let handleError = fun _ -> task { true |> should equal true }

    task {
        do! Request.ifAuthenticatedWithScope "another-issuer" "create" handleOk handleError ctx
    }

[<Fact>]
let ``Request.ifAuthenticatedWithScope should invoke handleError if authenticated with no scope from issuer`` () =
    let ctx = getHttpContextWriteable true
    let claims = [
        Claim("sub", "123", "str", "issuer");
        Claim("scope", "read create", "str", "another-issuer")
    ]
    ctx.User.Claims.Returns(claims) |> ignore

    let handleOk = fun _ -> task { true |> should equal false }
    let handleError = fun _ -> task { true |> should equal true }

    task {
        do! Request.ifAuthenticatedWithScope "issuer" "create" handleOk handleError ctx
    }