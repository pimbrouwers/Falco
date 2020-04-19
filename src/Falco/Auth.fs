module Falco.Auth

open System.Security.Claims
open System.Security.Principal
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Falco.ViewEngine

type IPrincipal with
    // Eeturns authentication status of IIdentity, false on null
    member this.IsAuthenticated() =
        match this.Identity with 
        | null -> false
        | _    -> 
            this.Identity.IsAuthenticated

type HttpContext with 
    // Returns authentication status of IPrincipal, false on null
    member this.IsAuthenticated () =
        match this.User with
        | null -> false 
        | _    -> this.User.IsAuthenticated()

// An HttpHandler to output HTML dependent on ClaimsPrincipal
let authHtmlOut (view : ClaimsPrincipal option -> XmlNode) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.User with
        | null -> htmlOut (view None) next ctx
        | _    -> htmlOut (view (Some ctx.User)) next ctx

// An HttpHandler to determine if user is authenticated.
// Receives handler for case of not authenticated.
let ifAuthenticated (notAuthenticatedHandler : HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.IsAuthenticated () with
        | false -> notAuthenticatedHandler next ctx
        | true  -> next ctx

// An HttpHandler to determine if user is authenticated.
// Receives handler for case of being authenticated.
let ifNotAuthenticated (authenticatedHandler : HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match ctx.IsAuthenticated () with
        | false -> next ctx
        | true  -> authenticatedHandler next ctx

// An HttpHandler to determine if user is authenticated,
// and belongs to one of the specified roles
// Receives handler for case of being not possessing role.
let ifInRole (roles : string list) (notAllowedHandler : HttpHandler) : HttpHandler =    
    let inRole : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            match List.exists ctx.User.IsInRole roles with
            | false -> notAllowedHandler next ctx
            | true  -> next ctx
        
    ifAuthenticated notAllowedHandler 
    >=> inRole

// An HttpHandler to sign principal out of specific auth scheme
let signOut (authScheme : string) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            do! ctx.SignOutAsync authScheme
            return! next ctx
        }