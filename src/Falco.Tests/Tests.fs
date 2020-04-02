namespace Falco.Tests

open System
open System.IO
open System.Text
open Xunit
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open NSubstitute

module Core =
    [<Fact>]    
    let ``strJoin should combine strings`` () =
        [|"the";"man";"jumped";"high"|]
        |> strJoin " "
        |> should equal "the man jumped high"

module Routing =
    [<Fact>]
    let ``can create RequestDelegate from HttpHandler`` () =
        let handler = textOut "hello"
        handler
        |> createRequestDelete
        |> should be ofExactType<RequestDelegate>

    [<Fact>]
    let ``can create RequestDelegate from composed HttpHandler's`` () =
        let handler = setStatusCode 403 >=> textOut "forbidden"
        handler
        |> createRequestDelete
        |> should be ofExactType<RequestDelegate>
    
module Request =
        [<Fact>]
        let ``RouteValue returns None for missing`` () =
            let ctx = Substitute.For<HttpContext>()
            ctx.Request.RouteValues <- new RouteValueDictionary()
            (ctx.RouteValue "name").IsNone |> should equal true

        [<Fact>]
        let ``RouteValue returns Some `` () =
            let ctx = Substitute.For<HttpContext>()
            ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
            let name = ctx.RouteValue "name"            
            name.IsSome |> should equal true
            name        |> Option.map (fun n -> n |> should equal "world")
        
        [<Fact>]
        let ``RouteValues returns entire route collection`` () =
            let ctx = Substitute.For<HttpContext>()
            ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
            let routeValues = ctx.RouteValues()
            routeValues.Count    |> should equal 1
            routeValues.["name"] |> should equal "world"

        [<Fact>]
        let ``GetService should throw on missing dependency``() =            
            let t = typeof<IAntiforgery>
            let ctx = Substitute.For<HttpContext>()
            ctx.RequestServices.GetService(t).Returns(null :> IAntiforgery) |> ignore

            (fun () -> ctx.GetService<IAntiforgery>() |> ignore)
            |> should throw typeof<InvalidDependencyException>

        [<Fact>]
        let ``GetService should return dependency``() =            
            let t = typeof<IAntiforgery>
            let ctx = Substitute.For<HttpContext>()
            ctx.RequestServices.GetService(t).Returns(Substitute.For<IAntiforgery>()) |> ignore

            ctx.GetService<IAntiforgery>()
            |> should be instanceOfType<IAntiforgery>


module Response =
    [<Fact>]
    let ``WriteString writes to body and sets content length`` () =            
        let ctx = Substitute.For<HttpContext>()
        ctx.Response.Body <- new MemoryStream()
            
        let expected = "hello world"
            
        task {
            ctx.WriteString expected |> ignore
            ctx.Response.Body.Position <- 0L
            use reader = new StreamReader(ctx.Response.Body)
            let! body = reader.ReadToEndAsync()                
            let contentLength = ctx.Response.ContentLength
                
            body          |> should equal expected
            contentLength |> should equal (Encoding.UTF8.GetBytes expected)
        }
        |> ignore
    

module Html =
    open Falco.ViewEngine
        
    [<Fact>]
    let ``Text should not be encoded`` () =
        let rawText = raw "<div>"
        renderNode rawText |> should equal "<div>"

    [<Fact>]
    let ``Text should be encoded`` () =
        let encodedText = enc "<div>"
        renderNode encodedText |> should equal "&lt;div&gt;"

    [<Fact>]
    let ``Self-closing tag should render with trailing slash`` () =
        let t = selfClosingTag "hr" [ _class "my-class" ]
        renderNode t |> should equal "<hr class=\"my-class\" />"

    [<Fact>]
    let ``Standard tag should render with attributes`` () =
        let t = tag "div" [ attr "class" (Some "my-class") ] []
        renderNode t |> should equal "<div class=\"my-class\"></div>"

    [<Fact>]
    let ``Should produce valid html doc`` () =
        let doc = html [] [
                div [ _class "my-class" ] [
                        h1 [] [ raw "hello" ]
                    ]
            ]
        renderHtml doc |> should equal "<!DOCTYPE html><html><div class=\"my-class\"><h1>hello</h1></div></html>"