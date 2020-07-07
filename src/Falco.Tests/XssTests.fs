module Falco.Tests.Xss

open FsUnit.Xunit
open Xunit
open Falco.Markup
open Falco.Security
open Microsoft.AspNetCore.Antiforgery

[<Fact>]
let ``antiforgetInput should return valid XmlNode`` () =
    let token = AntiforgeryTokenSet("REQUEST_TOKEN", "COOKIE_TOKEN", "FORM_FIELD_NAME", "HEADER_NAME")
    let input = Xss.antiforgeryInput token
    
    let expected = "<input type=\"hidden\" name=\"FORM_FIELD_NAME\" value=\"REQUEST_TOKEN\" />"
    
    match input with
    | Text _  
    | ParentNode _  -> 
        false |> should equal true

    | input ->        
        renderNode input
        |> should equal expected
        