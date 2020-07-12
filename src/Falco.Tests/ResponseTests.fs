module Falco.Tests.Response

open System.Text
open System.Text.Json
open Falco
open Falco.Markup
open FSharp.Control.Tasks
open FsUnit.Xunit
open Microsoft.Net.Http.Headers
open NSubstitute
open Xunit

[<Fact>]
let ``setStatusCode should modify HttpResponse StatusCode`` () =
    let ctx = getHttpContextWriteable false

    let expected = 204

    task {
        do! ctx 
            |> Response.withStatusCode expected 
            |> fun ctx -> ctx.Response.CompleteAsync ()
        
        ctx.Response.StatusCode 
        |> should equal expected
    }

[<Theory>]
[<InlineData(false)>]
[<InlineData(true)>]
let ``redirect temporary should invoke HttpResponse Redirect with provided bool`` (permanent : bool) =
    let ctx = getHttpContextWriteable false

    task {
        do! ctx
            |> Response.redirect "/" permanent
                    
        ctx.Response.Received().Redirect("/", permanent)
    }

[<Fact>]
let ``textOut produces text/plain result`` () =
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
let ``jsonOut produces applicaiton/json result`` () =
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
let ``jsonOutWithOptions produces applicaiton/json result ignoring nulls`` () =
    let ctx = getHttpContextWriteable false

    let expected = "{}"

    task {
        let jsonOptions = JsonSerializerOptions()
        jsonOptions.IgnoreNullValues <- true

        do! ctx
            |> Response.ofJsonOptions { Name = null } jsonOptions

        let! body = getResponseBody ctx
        let contentLength = ctx.Response.ContentLength        
        let contentType = ctx.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "application/json; charset=utf-8"
    }

[<Fact>]
let ``htmlOut produces text/html result`` () =
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
