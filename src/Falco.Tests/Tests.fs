namespace Falco.Tests

open System
open System.Collections.Generic
open System.IO
open System.Text
open Xunit
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Primitives
open NSubstitute

module Core =
    [<Fact>]    
    let ``strJoin should combine strings`` () =
        [|"the";"man";"jumped";"high"|]
        |> strJoin " "
        |> should equal "the man jumped high"
            
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
 
    [<Fact>]
    let ``RouteValue returns None for missing`` () =
        let ctx = Substitute.For<HttpContext>()
        ctx.Request.RouteValues <- new RouteValueDictionary()
        (ctx.TryGetRouteValue "name").IsNone |> should equal true

    [<Fact>]
    let ``RouteValue returns Some `` () =
        let ctx = Substitute.For<HttpContext>()
        ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
        let name = ctx.TryGetRouteValue "name"            
        name.IsSome |> should equal true
        name        |> Option.iter (fun n -> n |> should equal "world")
     
    [<Fact>]
    let ``RouteValues returns entire route collection`` () =
        let ctx = Substitute.For<HttpContext>()
        ctx.Request.RouteValues <- new RouteValueDictionary(dict["name", "world"])
        let routeValues = ctx.GetRouteValues()
        routeValues.Count    |> should equal 1
        routeValues.["name"] |> should equal "world"

module ModelBinding =
    [<Fact>] 
    let ``Inline query operator ?  should work`` () =
        let queryDict = 
            [| 
                "str", [|"John"|] |> StringValues
                "num", [|"1"|]    |> StringValues 
            |]
            |> Map.ofArray
            |> fun m -> Dictionary(m)

        let query = QueryCollection(queryDict)
        let ctx = Substitute.For<HttpContext>()
        ctx.Request.Query.Returns(query) |> ignore

        ctx.Query()?str.AsString()  |> should equal "John"
        ctx.Query()?num.AsInteger() |> should equal 1

    [<Fact>]
    let ``GetFormAsync should produce Map<string, string[]>`` () =        
        let formDictionary = 
            [| "name", [|"rick";"jim";"bob"|] |> StringValues |]            
            |> Map.ofArray
            |> fun m -> Dictionary(m)            
        let form = FormCollection(formDictionary)
        let ctx = Substitute.For<HttpContext>()
        ctx.Request.ReadFormAsync().Returns(form) |> ignore
        
        let expected = 
            [|   
                "name", [|"rick";"jim";"bob"|]
            |]
            |> Map.ofArray

        let formValues = ctx.GetFormAsync () |> Async.AwaitTask |> Async.RunSynchronously

        formValues |> should equal expected
    
    [<CLIMutable>]
    type ModelTest = 
        {
            Fstring         : string
            Fint16          : int16
            Fint32          : int32
            Fint64          : int64                        
            Fbool           : bool
            Ffloat          : float
            Fdecimal        : decimal
            FDateTime       : DateTime
            FDateTimeOffset : DateTimeOffset
            FTimeSpan       : TimeSpan
            FGuid           : Guid
            FStringList     : string seq
        }

    [<Fact>]
    let ``tryParseModel should produce record with CLIMutable containing primitives`` () =        
        let now = DateTime.Now.ToString()
        let offsetNow = DateTimeOffset.Now.ToString()
        let timespan = TimeSpan.FromSeconds(1.0).ToString()
        let guid = Guid().ToString()
        let expected = 
            { 
                Fstring = "John Doe"
                Fint16 = 0s
                Fint32 = 0
                Fint64 = 0L
                Fbool = true
                Ffloat = 0.0
                Fdecimal = 0M
                FDateTime = DateTime.Parse now
                FDateTimeOffset = DateTimeOffset.Parse offsetNow
                FTimeSpan = TimeSpan.Parse timespan
                FGuid = Guid.Parse guid
                FStringList = ["John"; "Doe"]
            }

        let values = dict [ 
                "fstring", [|"John Doe"|]
                "fint16", [|"0"|]
                "fint32", [|"0"|]
                "fint64", [|"0"|]
                "fbool", [|"true"|]
                "ffloat", [|"0.0"|]
                "fdecimal", [|"0"|]
                "fdatetime", [|now|]
                "fdatetimeoffset", [|offsetNow|]
                "ftimespan", [|timespan|]
                "fguid", [|guid|]
                "fstringlist", [|"John"; "Doe"|]
            ]

        let formTest = tryBindModel<ModelTest> values
        
        formTest
        |> Result.map (fun f -> f |> should equal expected)
        |> Result.mapError (fun _ -> false |> should equal true) // always a fail

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
