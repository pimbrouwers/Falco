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
    
/// Attempts to return specific claim from IPrincipal
let getClaim
    (claim : string)
    (ctx : HttpContext) : Claim option =
    match ctx.GetUser() with
    | None      -> None
    | Some user ->         
        match user.Claims |> Seq.tryFind (fun c -> strEquals c.Type claim) with
        | None   -> None
        | Some c -> Some c

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