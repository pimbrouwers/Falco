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
    let ``Can make StringCollectionReader from IQueryCollection`` () =
        StringCollectionReader(QueryCollection(Dictionary()))        
        |> should not' throw

    [<Fact>]
    let ``Can make StringCollectionReader from IFormCollection`` () =
        StringCollectionReader(FormCollection(Dictionary()))        
        |> should not' throw

    [<Fact>]
    let ``StringCollectionReader value lookups are case-insensitive`` () =
        let values = 
            [ 
                "FString", [|"John Doe"; "Jane Doe"|] |> StringValues                
            ]
            |> Map.ofList
            |> fun m -> Dictionary(m)

        let scr = StringCollectionReader(values)

        // single values
        scr.TryGet "FSTRING"   |> Option.iter (should equal "John Doe")
        scr.TryGet "FString"   |> Option.iter (should equal "John Doe")
        scr.TryGet "fstriNG"   |> Option.iter (should equal "John Doe")
        scr?FSTRING.AsString() |> should equal "John Doe"
        scr?FString.AsString() |> should equal "John Doe"
        scr?fstrINg.AsString() |> should equal "John Doe"

        // arrays
        scr.TryArrayString "FSTRING" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
        scr.TryArrayString "fString" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
        scr.TryArrayString "fstriNG" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
        scr?FSTRING.AsArrayString()  |> should equal [|"John Doe";"Jane Doe"|]
        scr?fString.AsArrayString()  |> should equal [|"John Doe";"Jane Doe"|]
        scr?fstriNG.AsArrayString()  |> should equal [|"John Doe";"Jane Doe"|]

    [<Fact>] 
    let ``Inline StringCollectionReader from query collection should resolve primitives`` () =
        let now = DateTime.Now.ToString()
        let offsetNow = DateTimeOffset.Now.ToString()
        let timespan = TimeSpan.FromSeconds(1.0).ToString()
        let guid = Guid().ToString()
     
        let values = 
            [ 
                "fstring", [|"John Doe"; "Jane Doe"|] |> StringValues
                "fint16", [|"16";"17"|] |> StringValues
                "fint32", [|"32";"33"|] |> StringValues
                "fint64", [|"64";"65"|] |> StringValues
                "fbool", [|"true";"false"|] |> StringValues
                "ffloat", [|"1.234";"1.235"|] |> StringValues
                "fdecimal", [|"4.567";"4.568"|] |> StringValues
                "fdatetime", [|now|] |> StringValues
                "fdatetimeoffset", [|offsetNow|] |> StringValues
                "ftimespan", [|timespan|] |> StringValues
                "fguid", [|guid|] |> StringValues
            ]
            |> Map.ofList
            |> fun m -> Dictionary(m)

        let scr = StringCollectionReader(values)

        // single values
        scr.TryGetString "fstring"                   |> Option.iter (should equal "John Doe")
        scr.TryGetInt16 "fint16"                     |> Option.iter (should equal 16s)
        scr.TryGetInt32 "fint32"                     |> Option.iter (should equal 32)
        scr.TryGetInt "fint32"                       |> Option.iter (should equal 32)
        scr.TryGetInt64 "fint64"                     |> Option.iter (should equal 64L)
        scr.TryGetBoolean "fbool"                    |> Option.iter (should equal true)
        scr.TryGetFloat "ffloat"                     |> Option.iter (should equal 1.234)
        scr.TryGetDecimal "fdecimal"                 |> Option.iter (should equal 4.567M)
        scr.TryGetDateTime "fdatetime"               |> Option.iter (should equal (DateTime.Parse(now)))
        scr.TryGetDateTimeOffset "fdatetimeoffset"   |> Option.iter (should equal (DateTimeOffset.Parse(offsetNow)))
        scr.TryGetTimeSpan "ftimespan"               |> Option.iter (should equal (TimeSpan.Parse(timespan)))
        scr.TryGetGuid "fguid"                       |> Option.iter (should equal (Guid.Parse(guid)))

        scr?fstring.AsString()                       |> should equal "John Doe"
        scr?fint16.AsInt16()                         |> should equal 16s
        scr?fint32.AsInt32()                         |> should equal 32
        scr?fint32.AsInt()                           |> should equal 32
        scr?fint64.AsInt64()                         |> should equal 64L
        scr?fbool.AsBoolean()                        |> should equal true
        scr?ffloat.AsFloat()                         |> should equal 1.234
        scr?fdecimal.AsDecimal()                     |> should equal 4.567M
        scr?fdatetime.AsDateTime()                   |> should equal (DateTime.Parse(now))
        scr?fdatetimeoffset.AsDateTimeOffset()       |> should equal (DateTimeOffset.Parse(offsetNow))
        scr?ftimespan.AsTimeSpan()                   |> should equal (TimeSpan.Parse(timespan))
        scr?fguid.AsGuid()                           |> should equal (Guid.Parse(guid))
        
        // array values
        scr.TryArrayString "fstring"                 |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
        scr.TryArrayInt16 "fint16"                   |> Option.iter (should equal [|16s;17s|])
        scr.TryArrayInt32 "fint32"                   |> Option.iter (should equal [|32;33|])
        scr.TryArrayInt "fint32"                     |> Option.iter (should equal [|32;33|])
        scr.TryArrayInt64 "fint64"                   |> Option.iter (should equal [|64L;65L|])
        scr.TryArrayBoolean "fbool"                  |> Option.iter (should equal [|true;false|])
        scr.TryArrayFloat "ffloat"                   |> Option.iter (should equal [|1.234;1.235|])
        scr.TryArrayDecimal "fdecimal"               |> Option.iter (should equal [|4.567M;4.568M|])
        scr.TryArrayDateTime "fdatetime"             |> Option.iter (should equal [|DateTime.Parse(now)|])
        scr.TryArrayDateTimeOffset "fdatetimeoffset" |> Option.iter (should equal [|DateTimeOffset.Parse(offsetNow)|])
        scr.TryArrayTimeSpan "ftimespan"             |> Option.iter (should equal [|TimeSpan.Parse(timespan)|])
        scr.TryArrayGuid "fguid"                     |> Option.iter (should equal [|Guid.Parse(guid)|])

        scr?fstring.AsArrayString()                  |> should equal [|"John Doe";"Jane Doe"|]
        scr?fint16.AsArrayInt16()                    |> should equal [|16s;17s|]
        scr?fint32.AsArrayInt32()                    |> should equal [|32;33|]
        scr?fint32.AsArrayInt()                      |> should equal [|32;33|]
        scr?fint64.AsArrayInt64()                    |> should equal [|64L;65L|]
        scr?fbool.AsArrayBoolean()                   |> should equal [|true;false|]
        scr?ffloat.AsArrayFloat()                    |> should equal [|1.234;1.235|]
        scr?fdecimal.AsArrayDecimal()                |> should equal [|4.567M;4.568M|]
        scr?fdatetime.AsArrayDateTime()              |> should equal [|DateTime.Parse(now)|]
        scr?fdatetimeoffset.AsArrayDateTimeOffset()  |> should equal [|DateTimeOffset.Parse(offsetNow)|]
        scr?ftimespan.AsArrayTimeSpan()              |> should equal [|TimeSpan.Parse(timespan)|]
        scr?fguid.AsArrayGuid()                      |> should equal [|Guid.Parse(guid)|]
                    
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
