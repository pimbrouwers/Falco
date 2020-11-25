module Falco.Tests.Markup

open System
open Falco.Markup
open FsUnit.Xunit
open Xunit
        
[<Fact>]
let ``Text.empty should be empty`` () =    
    renderNode Text.empty |> should equal String.Empty

[<Fact>]
let ``Text.raw should not be encoded`` () =
    let rawText = Text.raw "<div>"
    renderNode rawText |> should equal "<div>"

[<Fact>]
let ``Text.raw should not be encoded, but template applied`` () =
    let rawText = Text.rawf "<div>%s</div>" "falco"
    renderNode rawText |> should equal "<div>falco</div>"

[<Fact>]
let ``Text.enc should be encoded`` () =
    let encodedText = Text.enc "<div>"
    renderNode encodedText |> should equal "&lt;div&gt;"

[<Fact>]
let ``Text.comment should equal HTML comment`` () =
    let rawText = Text.comment "test comment"
    renderNode rawText |> should equal "<!-- test comment -->"

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
                                Elem.h1 [] [ Text.raw "hello" ]
                            ]
                    ]
            ]
    renderHtml doc |> should equal "<!DOCTYPE html><html><body><div class=\"my-class\"><h1>hello</h1></div></body></html>"

[<Fact>]
let ``Attr.merge should combine two XmlAttribute lists`` () =
    Attr.merge
        [ KeyValueAttr("class", "ma2") ] 
        [ KeyValueAttr("id", "some-el"); KeyValueAttr("class", "bg-red"); NonValueAttr("readonly") ]
    |> should equal [ KeyValueAttr("class", "ma2 bg-red"); KeyValueAttr("id", "some-el"); NonValueAttr("readonly") ]

[<Fact>]
let ``Attr.merge should work with bogus "class" NonValeAttr`` () =
    Attr.merge
        [ KeyValueAttr("class", "ma2") ] 
        [ KeyValueAttr("id", "some-el"); KeyValueAttr("class", "bg-red"); NonValueAttr("class") ]
    |> should equal [ KeyValueAttr("class", "ma2 bg-red"); KeyValueAttr("id", "some-el") ]

    
