[<RequireQualifiedAccess>]
module Falco.Security.Auth

open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
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
    
let signInAsync
    (authScheme : string)
    (claimsPrincipal : ClaimsPrincipal)
    (ctx : HttpContext) : Task =
    ctx.SignInAsync(authScheme, claimsPrincipal)

let signOutAsync
    (authScheme : string)
    (ctx : HttpContext) : Task = 
    ctx.SignOutAsync(authScheme)