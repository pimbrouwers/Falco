module Falco.Tests.Handlers 

open System.Text
open Falco
open Falco.ViewEngine
open FSharp.Control.Tasks
open FsUnit.Xunit
open Microsoft.Net.Http.Headers
open NSubstitute
open Xunit

type JsonOutTest = { Name : string }

[<Fact>]
let ``setStatusCode should modify HttpResponse StatusCode`` () =
    let ctx = getHttpContextWriteable false

    let expected = 204

    task {
        let! result = setStatusCode 204 shortCircuit ctx
        result.IsSome |> should equal true

        result.Value.Response.StatusCode |> should equal expected
    }

[<Fact>]
let ``redirect temporary should invoke HttpResponse Redirect with false`` () =
    let ctx = getHttpContextWriteable false

    task {
        let! result = redirect "/" false shortCircuit ctx
        result.IsSome |> should equal true
        result.Value.Response.Received().Redirect("/", false)
    }

[<Fact>]
let ``redirect permanent should invoke HttpResponse Redirect with true`` () =
    let ctx = getHttpContextWriteable false

    task {
        let! result = redirect "/" true shortCircuit ctx
        result.IsSome |> should equal true
        result.Value.Response.Received().Redirect("/", true)
    }

[<Fact>]
let ``textOut produces text/plain result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "hello"

    task {
        let! result = textOut "hello" shortCircuit ctx
        
        result.IsSome |> should equal true

        let! body = getBody result.Value
        let contentLength = result.Value.Response.ContentLength        
        let contentType = result.Value.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "text/plain; charset=utf-8"
    }

[<Fact>]
let ``jsonOut produces applicaiton/json result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "{\"Name\":\"John Doe\"}"

    task {
        let! result = jsonOut { Name = "John Doe"} shortCircuit ctx

        result.IsSome |> should equal true

        let! body = getBody result.Value
        let contentLength = result.Value.Response.ContentLength        
        let contentType = result.Value.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "application/json; charset=utf-8"
    }

[<Fact>]
let ``htmlOut produces text/html result`` () =
    let ctx = getHttpContextWriteable false

    let expected = "<!DOCTYPE html><html><div class=\"my-class\"><h1>hello</h1></div></html>"

    let doc = html [] [
            div [ _class "my-class" ] [
                    h1 [] [ raw "hello" ]
                ]
        ]

    task {
        let! result = htmlOut doc shortCircuit ctx

        result.IsSome |> should equal true

        let! body = getBody result.Value
        let contentLength = result.Value.Response.ContentLength        
        let contentType = result.Value.Response.Headers.[HeaderNames.ContentType]

        body          |> should equal expected
        contentLength |> should equal (Encoding.UTF8.GetBytes expected).LongLength
        contentType   |> should equal "text/html; charset=utf-8"
    }
