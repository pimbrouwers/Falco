module Falco.Security.Auth

open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Falco.StringUtils
open Falco.Extensions

/// Returns authentication status of IPrincipal, false on null
let isAuthenticated 
    (ctx : HttpContext) : bool =
    ctx.IsAuthenticated()

/// Returns bool if IPrincipal is in list of roles, false on None
let isInRole 
    (roles : string list)
    (ctx : HttpContext) : bool =
    match ctx.GetUser() with
    | None      -> false
    | Some user -> List.exists user.IsInRole roles

/// Attempts to return claims from IPrincipal, empty seq on None
let getClaims
    (ctx : HttpContext) : Claim seq =
    match ctx.GetUser() with
    | None      -> Seq.empty
    | Some user -> user.Claims

/// Attempts to return a specific claim from IPrincipal with a generic predicate
let tryFindClaim
    (predicate : Claim -> bool)
    (ctx : HttpContext) : Claim option =
    match ctx.GetUser() with
    | None      -> None
    | Some user ->         
        match user.Claims |> Seq.tryFind predicate with
        | None   -> None
        | Some claim -> Some claim

/// Attempts to return specific claim from IPrincipal
let getClaim
    (claimType : string)
    (ctx : HttpContext) : Claim option =
    tryFindClaim (fun claim -> strEquals claim.Type claimType) ctx

/// Returns bool if IPrincipal has specified scope
let hasScope
    (issuer : string)
    (scope : string)
    (ctx : HttpContext) : bool =
    tryFindClaim (fun claim -> (strEquals claim.Issuer issuer) && (strEquals claim.Type "scope")) ctx
    |> function
        | None       -> false
        | Some claim -> Array.contains scope (strSplit [|' '|] claim.Value)

/// Establish an authenticated context for the provide scheme and principal
let signIn
    (authScheme : string)
    (claimsPrincipal : ClaimsPrincipal)
    (ctx : HttpContext) : Task =
    ctx.SignInAsync(authScheme, claimsPrincipal)

/// Terminate authenticated context for provided scheme
let signOut
    (authScheme : string)
    (ctx : HttpContext) : Task = 
    ctx.SignOutAsync(authScheme)