[<RequireQualifiedAccess>]
module Falco.Auth

open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http

let isAuthenticated 
    (ctx : HttpContext) : bool =
    ctx.IsAuthenticated()

let isInRole 
    (roles : string list)
    (ctx : HttpContext) : bool =
    match ctx.GetUser() with
    | None      -> false
    | Some user -> List.exists user.IsInRole roles
    
let signOut 
    (authScheme : string)
    (ctx : HttpContext) : Task = 
    ctx.SignOutAsync authScheme     