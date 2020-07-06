module Falco.Tests.Markup

open Falco.Markup
open FsUnit.Xunit
open Xunit
        
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
    let t = Elem.selfClosingTag "hr" [ Attr.class' "my-class" ]
    renderNode t |> should equal "<hr class=\"my-class\" />"

[<Fact>]
let ``Standard tag should render with multiple attributes`` () =
    let t = Elem.tag "div" [ Attr.create "class" "my-class"; Attr.autofocus; Attr.create "data-bind" "slider" ] []
    renderNode t |> should equal "<div class=\"my-class\" autofocus data-bind=\"slider\"></div>"

[<Fact>]
let ``Should produce valid html doc`` () =
    let doc = 
        Elem.html [] [
                Elem.body [] [
                        Elem.div [ Attr.class' "my-class" ] [
                                Elem.h1 [] [ raw "hello" ]
                            ]
                    ]
            ]
    renderHtml doc |> should equal "<!DOCTYPE html><html><body><div class=\"my-class\"><h1>hello</h1></div></body></html>"
