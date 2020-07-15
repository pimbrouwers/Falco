[<RequireQualifiedAccess>]
module Falco.Security.Auth

open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Falco.StringUtils
open Falco.Extensions

let isAuthenticated 
    (ctx : HttpContext) : bool =
    ctx.IsAuthenticated()

let isInRole 
    (roles : string list)
    (ctx : HttpContext) : bool =
    match ctx.GetUser() with
    | None      -> false
    | Some user -> List.exists user.IsInRole roles
    
let getClaim
    (claim : string)
    (ctx : HttpContext) : Claim option =
    match ctx.GetUser() with
    | None      -> None
    | Some user ->         
        match user.Claims |> Seq.tryFind (fun c -> strEquals c.Type claim) with
        | None   -> None
        | Some c -> Some c
               
let signInAsync
    (authScheme : string)
    (claimsPrincipal : ClaimsPrincipal)
    (ctx : HttpContext) : Task =
    ctx.SignInAsync(authScheme, claimsPrincipal)

let signOutAsync
    (authScheme : string)
    (ctx : HttpContext) : Task = 
    ctx.SignOutAsync(authScheme)