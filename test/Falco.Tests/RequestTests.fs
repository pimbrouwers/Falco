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

    headers.GetString HeaderNames.Server |> should equal serverName
    headers.TryGetString "missing" |> should equal None

[<Fact>]
let ``Request.getRouteValues should return Map<string, string> from HttpContext`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    let route = Request.getRoute ctx

    route.GetString "name"
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

[<Fact>]
let ``Request.mapJson`` () =
    let ctx = getHttpContextWriteable false
    use ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":\"falco\"}"))
    ctx.Request.Body.Returns(ms) |> ignore

    let handle json : HttpHandler =
        json.Name |> should equal "falco"
        Response.ofEmpty

    Request.mapJson handle ctx

[<Fact>]
let ``Request.mapRoute`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.RouteValues <- RouteValueDictionary({|name="falco"|})

    let handle name : HttpHandler =
        name |> should equal "falco"
        Response.ofEmpty

    Request.mapRoute (fun r -> r.GetString "name") handle ctx

[<Fact>]
let ``Request.mapCookie`` () =
    let ctx = getHttpContextWriteable false
    ctx.Request.Cookies <- Map.ofList ["name", "falco"] |> cookieCollection

    let handle name : HttpHandler =
        name |> should equal "falco"
        Response.ofEmpty

    Request.mapCookie (fun q -> q.GetString "name") handle ctx

[<Fact>]
let ``Request.mapQuery`` () =
    let ctx = getHttpContextWriteable false
    let query = Dictionary<string, StringValues>()
    query.Add("name", StringValues("falco"))
    ctx.Request.Query <- QueryCollection(query)

    let handle name : HttpHandler =
        name |> should equal "falco"
        Response.ofEmpty

    Request.mapQuery (fun c -> c.GetString "name") handle ctx

[<Fact>]
let ``Request.mapForm`` () =
    let ctx = getHttpContextWriteable false
    let form = Dictionary<string, StringValues>()
    form.Add("name", StringValues("falco"))
    ctx.Request.ReadFormAsync().Returns(FormCollection(form)) |> ignore

    let handle name : HttpHandler =
        name |> should equal "falco"
        Response.ofEmpty

    Request.mapForm (fun f -> f.GetString "name") handle ctx |> ignore
    Request.mapFormSecure (fun f -> f.GetString "name") handle Response.ofEmpty ctx |> ignore

[<Fact>]
let ``Request.mapFormStream`` () =
    let ctx = getHttpContextWriteable false
    let body =
        "--9051914041544843365972754266\r\n" +
        "Content-Disposition: form-data; name=\"name\"\r\n" +
        "\r\n" +
        "falco\r\n" +
        "--9051914041544843365972754266\r\n" +
        "Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
        "Content-Type: text/plain\r\n" +
        "\r\n" +
        "Content of a.txt.\r\n" +
        "\r\n" +
        "--9051914041544843365972754266\r\n" +
        "Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
        "Content-Type: text/html\r\n" +
        "\r\n" +
        "<!DOCTYPE html><title>Content of a.html.</title>\r\n" +
        "\r\n" +
        "--9051914041544843365972754266--\r\n";

    use ms = new MemoryStream(Encoding.UTF8.GetBytes(body))
    ctx.Request.Body.Returns(ms) |> ignore

    let contentType = "multipart/form-data;boundary=\"9051914041544843365972754266\""
    ctx.Request.ContentType <- contentType

    let handle (formValue : string, files : IFormFileCollection option) : HttpHandler =
        formValue |> should equal "falco"
        files |> shouldBeSome (fun x ->
            x.Count |> should equal 2

            // can we access the files?
            use ms = new MemoryStream()
            use st1 = x.[0].OpenReadStream()
            st1.CopyTo(ms)

            ms.SetLength(0)
            use st2 = x.[1].OpenReadStream()
            st1.CopyTo(ms))
        Response.ofEmpty

    Request.mapFormStream (fun f -> f.GetString "name", f.Files) handle ctx |> ignore
    Request.mapFormStreamSecure (fun f -> f.GetString "name", f.Files) handle Response.ofEmpty ctx |> ignore

type HandlerEndSignal = | HandlerEndSignal
type PipelineEndSignal = | PipelineEndSignal

[<Fact>]
let ``Request.httpPipeTask awaits the next handler before returning`` () =
    let ctx = getHttpContextWriteable false

    let handlerTcs = TaskCompletionSource<HandlerEndSignal>()
    let pipelineEndTcs = TaskCompletionSource<PipelineEndSignal>()

    let prepare ctx = Task.FromResult(42)

    let handler arg : HttpHandler =
        fun ctx -> handlerTcs.Task

    task {
        // Start the pipeline task:
        let pipelineTask =
            task {
                do! Request.httpPipeTask prepare handler ctx
                pipelineEndTcs.SetResult(PipelineEndSignal)
            }

        // Since we didn't unlock the handler task yet, the timeout should expire first:
        let timeout = Task.Delay(100)
        let! firstTaskToComplete = Task.WhenAny(handlerTcs.Task, pipelineEndTcs.Task, timeout)

        firstTaskToComplete |> should equal timeout

        // Now let's unlock the handler task:
        handlerTcs.SetResult(HandlerEndSignal)

        // And wait for the end of the pipeline:
        let! firstTaskToComplete = Task.WhenAny(handlerTcs.Task, pipelineEndTcs.Task)

        // The handler should have completed first:
        firstTaskToComplete |> should equal handlerTcs.Task
    }

