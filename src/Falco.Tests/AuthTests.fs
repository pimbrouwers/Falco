module Falco.Tests.Auth

open System.Security.Claims
open Falco
open Falco.Security
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Microsoft.AspNetCore.Authentication
open NSubstitute
open Xunit

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
    