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


type myRoles = Admin | Other

[<Struct>]
type fastRoles = SuperUser | RegularUser
    with override this.ToString() =
            match this with
            | SuperUser   -> "SuperUser"
            | RegularUser -> "RegularUser"

type RolesData() as this =
    inherit TheoryData<obj, bool>()
    do  this.Add("Admin", true)
        this.Add("Admin", false)
        this.Add(Admin, false)
        this.Add(Admin, true)
        this.Add(Other, false)
        this.Add(Other, true)
        this.Add(SuperUser, false)
        this.Add(RegularUser, true)

[<Theory>] 
[<ClassData(typeof<RolesData>)>]
let ``Auth.isInRole returns if principal has role`` (role) (inRole : bool) = 
    let ctx = getHttpContextWriteable false
    ctx.User.IsInRole(role.ToString()).Returns(inRole) |> ignore
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
    