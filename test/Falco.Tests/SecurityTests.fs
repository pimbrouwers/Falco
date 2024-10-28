module Falco.Tests.SecurityTests

open FsUnit.Xunit
open Xunit
open Falco.Security
open Falco.Markup
open Microsoft.AspNetCore.Antiforgery

module Xsrf =
    [<Fact>]
    let ``antiforgetInput should return valid XmlNode`` () =
        let token = AntiforgeryTokenSet("REQUEST_TOKEN", "COOKIE_TOKEN", "FORM_FIELD_NAME", "HEADER_NAME")
        let input = Xsrf.antiforgeryInput token

        let expected = "<input type=\"hidden\" name=\"FORM_FIELD_NAME\" value=\"REQUEST_TOKEN\" />"

        match input with
        | TextNode _
        | ParentNode _  ->
            false |> should equal true

        | input ->
            renderNode input
            |> should equal expected
