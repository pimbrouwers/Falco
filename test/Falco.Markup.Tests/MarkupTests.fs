module Falco.Tests.Markup

open System
open Falco.Markup
open FsUnit.Xunit
open Xunit
        
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
    let t = Elem.selfClosingTag "hr" []
    renderNode t |> should equal "<hr />"

[<Fact>]
let ``Self-closing tag with attrs should render with trailing slash`` () =
    let t = Elem.selfClosingTag "hr" [ Attr.class' "my-class" ]
    renderNode t |> should equal "<hr class=\"my-class\" />"

[<Fact>]
let ``Standard tag should render with multiple attributes`` () =
    let t = Elem.tag "div" [ Attr.create "class" "my-class"; Attr.autofocus; Attr.create "data-bind" "slider" ] []
    renderNode t |> should equal "<div class=\"my-class\" autofocus data-bind=\"slider\"></div>"

[<Fact>]
let ``Script should contain src, lang and async`` () =
    let t = Elem.script [ Attr.src "http://example.org/example.js";  Attr.lang "javascript"; Attr.async ] []
    renderNode t |> should equal "<script src=\"http://example.org/example.js\" lang=\"javascript\" async></script>"

[<Fact>]
let ``Should produce valid html doc`` () =
    let doc = 
        Elem.html [] [
            Elem.body [] [
                Elem.div [ Attr.class' "my-class" ] [
                    Elem.h1 [] [ Text.raw "hello" ] ] ] ]
    renderHtml doc |> should equal "<!DOCTYPE html><html><body><div class=\"my-class\"><h1>hello</h1></div></body></html>"
    
type Product =
    { Name : string 
      Price : float 
      Description : string }

[<Fact>]
let ``Should produce valid html doc for large result`` () =
    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

    let products = 
        [ 1..25000 ]
        |> List.map (fun i -> { Name = sprintf "Name %i" i; Price = i |> float; Description = lorem})

    let elem product =                
        Elem.li [] [ 
            Elem.h2 [] [ Text.raw product.Name ] 
            Text.rawf "Only %f" product.Price
            Text.raw product.Description ]

    let productElems = 
        products
        |> List.map elem
        |> Elem.ul [ Attr.id "products" ]     

    let doc = 
        Elem.html [] [
            Elem.body [] [
                Elem.div [ Attr.class' "my-class" ] [ productElems ] ] ]

    let render = renderHtml doc
    render |> fun s -> s.Substring(0, 27) |> should equal "<!DOCTYPE html><html><body>"
    render |> fun s -> s.Substring(s.Length - 14, 14) |> should equal "</body></html>"