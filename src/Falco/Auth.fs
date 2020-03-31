module Falco.Auth

open System.Security.Claims
open System.Security.Principal
open Microsoft.AspNetCore.Http
open Falco.ViewEngine

type HttpContext with 
    member this.IsAuthenticated () =
        match this.User with
        | null -> false 
        | _    ->
            match this.User.Identity with 
            | null -> false
            | _    -> 
                this.User.Identity.IsAuthenticated

type IIdentity with    
    member this.GetNameIdentifer () =            
        let i = this :?> ClaimsIdentity
        i.FindFirst(ClaimTypes.NameIdentifier).Value
        |> parseInt 
            
type IPrincipal with
    member this.GetNameIdentifier() =
        this.Identity.GetNameIdentifer()

let ifAuthenticated notAuthenticated : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.IsAuthenticated () with
        | false -> notAuthenticated next ctx
        | true  -> next ctx

let ifNotAuthenticated authenticated : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.IsAuthenticated () with
        | false -> next ctx
        | true  -> authenticated next ctx

let authHtmlOut (view : ClaimsPrincipal option -> XmlNode) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.User with
        | null -> htmlOut (view None) next ctx
        | _    -> htmlOut (view (Some ctx.User)) next ctx