module Falco.Tests.SecurityTests

open FsUnit.Xunit
open Xunit
open Falco.Security
open Falco.Markup
open Microsoft.AspNetCore.Antiforgery
open System.Security.Claims
open NSubstitute

module Xss =
    [<Fact>]
    let ``antiforgetInput should return valid XmlNode`` () =
        let token = AntiforgeryTokenSet("REQUEST_TOKEN", "COOKIE_TOKEN", "FORM_FIELD_NAME", "HEADER_NAME")
        let input = Xss.antiforgeryInput token

        let expected = "<input type=\"hidden\" name=\"FORM_FIELD_NAME\" value=\"REQUEST_TOKEN\" />"

        match input with
        | TextNode _
        | ParentNode _  ->
            false |> should equal true

        | input ->
            renderNode input
            |> should equal expected

module Auth =
    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    let ``Auth.isAuthenticated return based on principal authentication`` (isAuthenticated : bool) =
        let ctx = getHttpContextWriteable isAuthenticated

        Auth.isAuthenticated ctx
        |> should equal isAuthenticated

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    let ``Auth.isInRole returns if principal has role`` (inRole : bool) =
        let ctx = getHttpContextWriteable false
        let role = "Admin"
        ctx.User.IsInRole(role).Returns(inRole) |> ignore
        Auth.isInRole [role] ctx
        |> should equal inRole

    [<Fact>]
    let ``Auth.tryFindClaim returns claim if exists`` () =
        let ctx = getHttpContextWriteable true
        let claimType = "email"
        let claimValue = "test@test.com"
        ctx.User.Claims.Returns([Claim(claimType, claimValue)]) |> ignore

        Auth.getClaims ctx
        |> Seq.head
        |> fun claim ->            
            claim.Value
            |> should equal claimValue

    [<Fact>]
    let ``Auth.tryFindClaim returns none claim if does not exist`` () =
        let ctx = getHttpContextWriteable true
        ctx.User.Claims.Returns([]) |> ignore

        Auth.getClaims ctx
        |> Seq.length 
        |> should equal 0
        
    [<Fact>]
    let ``Auth.hasScope should return true if scope claim from issuer is found and has specific value`` () =
        let ctx = getHttpContextWriteable true
        let claims = [
            Claim("sub", "123", "str", "issuer1");
            Claim("scope", "read create update delete", "str", "issuer2")
        ]
        ctx.User.Claims.Returns(claims) |> ignore

        Auth.hasScope "issuer2" "update" ctx
        |> should equal true

    [<Fact>]
    let ``Auth.hasScope should return false if no claim from issuer is found`` () =
        let ctx = getHttpContextWriteable true
        let claims = [
            Claim("sub", "123", "str", "issuer1");
            Claim("scope", "read create update delete", "str", "issuer2")
        ]
        ctx.User.Claims.Returns(claims) |> ignore

        Auth.hasScope "issuer3" "update" ctx
        |> should equal false

    [<Fact>]
    let ``Auth.hasScope should return false if scope claim from issuer is not found`` () =
        let ctx = getHttpContextWriteable true
        let claims = [
            Claim("sub", "123", "str", "issuer1");
            Claim("scope", "read create update delete", "str", "issuer2")
        ]
        ctx.User.Claims.Returns(claims) |> ignore

        Auth.hasScope "issuer1" "update" ctx
        |> should equal false

    [<Fact>]
    let ``Auth.hasScope should return false if scope claim from issuer has not specific value`` () =
        let ctx = getHttpContextWriteable true
        let claims = [
            Claim("sub", "123", "str", "issuer1");
            Claim("scope", "read create update delete", "str", "issuer2")
        ]
        ctx.User.Claims.Returns(claims) |> ignore

        Auth.hasScope "issuer2" "manage" ctx
        |> should equal false