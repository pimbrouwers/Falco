module Falco.Auth

open System.Security.Claims
open System.Security.Principal
open Microsoft.AspNetCore.Http
open Falco.ViewEngine

type IPrincipal with
    member this.IsAuthenticated() =
        match this.Identity with 
        | null -> false
        | _    -> 
            this.Identity.IsAuthenticated

type HttpContext with 
    member this.IsAuthenticated () =
        match this.User with
        | null -> false 
        | _    -> this.User.IsAuthenticated()

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