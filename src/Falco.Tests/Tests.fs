namespace Falco.Tests

open System
open Xunit
open FsUnit.Xunit

module Html =
    open Falco.ViewEngine
    
    [<Fact>]
    let ``Self-closing tag should render with trailing slash`` () =
        let t = selfClosingTag "hr" []
        renderNode t |> should equal "<hr />"

    [<Fact>]
    let ``Standard tag should render with attributes`` () =
        let t = tag "div" [ attr "class" (Some "my-class") ] []
        renderNode t |> should equal "<div class=\"my-class\"></div>"

    [<Fact>]
    let ``Should produce valid html doc`` () =
        let doc = html [] [
                div [ _class "my-class" ] [
                        h1 [] [ rawText "hello" ]
                    ]
            ]
        renderHtml doc |> should equal "<!DOCTYPE html><html><div class=\"my-class\"><h1>hello</h1></div></html>"