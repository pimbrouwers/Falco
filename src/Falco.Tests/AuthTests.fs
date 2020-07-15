module Falco.Tests.Auth

open System.Security.Claims
open Falco
open Falco.Security
open FSharp.Control.Tasks
open FsUnit.Xunit
open Xunit
open NSubstitute

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