namespace Falco.Tests

open FsUnit.Xunit
open Xunit
open Falco.Markup
open Falco.Security
open Microsoft.AspNetCore.Antiforgery
open System.Security.Claims
open NSubstitute

module Crypto =
    [<Theory>]
    [<InlineData(0, 10000)>]
    [<InlineData(100000, 150000)>]
    let ``randomInt should produce int between min & max``
        (min : int)
        (max : int) =
        Crypto.randomInt min max
        |> fun i ->
            (i >= min && i <= max)
            |> should equal true

    [<Fact>]
    let ``createSalt should generate a random salt of specified length`` () =
        let salt = Crypto.createSalt 16
        salt
        |> fun s -> s.Length |> should equal 24

    let salt = "8BSBv62T/qi2Yf10QBN4Zw=="
    let iterations = 150000
    let password = "falco"

    [<Fact>]
    let ``sha256 should produce expected 44 character string from inputs`` () =
        let expected = "Id62vc62JiHjAXT4m8Ie3nF3fwDZDp256Ug+6crAZyI="
        let byteLen = 32
        let hash = Crypto.sha256 iterations byteLen salt password

        hash.Length = 44 |> should equal true
        hash |> should equal expected

    [<Fact>]
    let ``sha512 should produce expected 44 character string from inputs`` () =
        let expected = "H+g5yVaqtF49c6ZBT7FL/2UJymeWLjyqOMjG8S/bowI="
        let byteLen = 32
        let hash = Crypto.sha512 iterations byteLen salt password

        hash.Length = 44 |> should equal true
        hash |> should equal expected

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

        let claim = Auth.tryFindClaim (fun c -> c.Type = claimType && c.Value = claimValue) ctx
        claim.IsSome |> should equal true

        claim.Value
        |> fun c -> c.Value
        |> should equal claimValue

    [<Fact>]
    let ``Auth.tryFindClaim returns none claim if does not exist`` () =
        let ctx = getHttpContextWriteable true
        ctx.User.Claims.Returns([]) |> ignore

        let claim = Auth.tryFindClaim (fun c -> c.Value = "falco") ctx
        claim.IsNone |> should equal true

    [<Fact>]
    let ``Auth.getClaim returns claim if exists`` () =
        let ctx = getHttpContextWriteable true
        let claimValue = "falco"
        ctx.User.Claims.Returns([Claim(ClaimTypes.Name, claimValue)]) |> ignore

        let claim = Auth.getClaim ClaimTypes.Name ctx
        claim.IsSome |> should equal true

        claim.Value
        |> fun c -> c.Value
        |> should equal claimValue

    [<Fact>]
    let ``Auth.getClaim returns none claim if does not exist`` () =
        let ctx = getHttpContextWriteable true
        ctx.User.Claims.Returns([]) |> ignore

        let claim = Auth.getClaim ClaimTypes.Name ctx
        claim.IsNone |> should equal true

    [<Fact>]
    let ``Auth.getClaimValue returns claim value if exists`` () =
        let ctx = getHttpContextWriteable true
        let expected = "falco"
        ctx.User.Claims.Returns([Claim(ClaimTypes.Name, expected)]) |> ignore

        let claimValue = Auth.getClaimValue ClaimTypes.Name ctx
        claimValue.IsSome |> should equal true

        claimValue.Value
        |> should equal expected

    [<Fact>]
    let ``Auth.getClaimValue returns none claim if does not exist`` () =
        let ctx = getHttpContextWriteable true
        ctx.User.Claims.Returns([]) |> ignore

        let claim = Auth.getClaimValue ClaimTypes.Name ctx
        claim.IsNone |> should equal true

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