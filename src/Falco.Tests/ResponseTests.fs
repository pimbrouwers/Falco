module Falco.Tests.Response

open System.Text
open System.Text.Json
open Falco
open Falco.Markup
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Microsoft.Net.Http.Headers
open NSubstitute
open Xunit

[<Fact>]
let ``Response.withStatusCode should modify HttpResponse StatusCode`` () =
    let ctx = getHttpContextWriteable false

    let expected = 204

    task {
        do! ctx
            |> (Response.withStatusCode expected >> Response.ofEmpty)

        ctx.Response.StatusCode
        |> should equal expected
    }

[<Fact>]
let ``Response.withHeader should set header`` () =
    let serverName = "Kestrel"
    let ctx = getHttpContextWriteable false

    task {
        do! ctx
            |> (Response.withHeader HeaderNames.Server serverName >> Response.ofEmpty)

        ctx.Response.Headers.[HeaderNames.Server]
        |> should equal serverName
    }

[<Fact>]
let ``Response.withContentType should set header`` () =
    let contentType = "text/plain; charset=utf-8"
    let ctx = getHttpContextWriteable false

    task {
        do! ctx
            |> (Response.withHeader HeaderNames.ContentType contentType>> Response.ofEmpty)

        ctx.Response.Headers.[HeaderNames.ContentType]
        |> should equal contentType
    }

[<Theory>]
[<InlineData(false)>]
[<InlineData(true)>]
let ``Response.redirect temporary should invoke HttpResponse Redirect with provided bool`` (permanent : bool) =
    let ctx = getHttpContextWriteable false

    task {
        do! ctx
            |> Response.redirect "/" permanent

        ctx.Response.Received().Redirect("/", permanent)
    }

[<Fact>]
let ``Response.ofPlainText produces text/plain result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "hello"

    task {
        do! ctx
            |> Response.ofPlainText expected

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "text/plain; charset=utf-8"
    }

[<Fact>]
let ``Response.ofJson produces applicaiton/json result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "{\"Name\":\"John Doe\"}"

    task {
        do! ctx
            |> Response.ofJson { Name = "John Doe"}

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "application/json; charset=utf-8"
    }

[<Fact>]
let ``Response.ofJsonOptions produces applicaiton/json result ignoring nulls`` () =
    let ctx = getHttpContextWriteable false

    let expected = "{}"

    task {
        let jsonOptions = JsonSerializerOptions()
        jsonOptions.IgnoreNullValues <- true

        do! ctx
            |> Response.ofJsonOptions jsonOptions { Name = null }

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "application/json; charset=utf-8"
    }

[<Fact>]
let ``Response.ofHtml produces text/html result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "<!DOCTYPE html><html><div class=\"my-class\"><h1>hello</h1></div></html>"

    let doc =
        Elem.html [] [
                Elem.div [ Attr.class' "my-class" ] [
                        Elem.h1 [] [ Text.raw "hello" ]
                    ]
            ]

    task {
        do! ctx
            |> Response.ofHtml doc

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "text/html; charset=utf-8"
    }

[<Fact>]
let ``Response.ofHtmlString produces text/html result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "<!DOCTYPE html><html><div class=\"my-class\"><h1>hello</h1></div></html>"

    task {
        do! ctx
            |> Response.ofHtmlString expected

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "text/html; charset=utf-8"
    }
