[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

// ------------
// Constants
// ------------
module Constants =
    let defaultJsonOptions =
        let options = Text.Json.JsonSerializerOptions()
        options.AllowTrailingCommas <- true
        options.PropertyNameCaseInsensitive <- true
        options

// ------------
// Errors
// ------------

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

// ------------
// HTTP
// ------------

/// Http verb
type HttpVerb =
    | GET
    | HEAD
    | POST
    | PUT
    | PATCH
    | DELETE
    | OPTIONS
    | TRACE
    | ANY

    override x.ToString() =
        match x with
        | GET     -> HttpMethods.Get
        | HEAD    -> HttpMethods.Head
        | POST    -> HttpMethods.Post
        | PUT     -> HttpMethods.Put
        | PATCH   -> HttpMethods.Patch
        | DELETE  -> HttpMethods.Delete
        | OPTIONS -> HttpMethods.Options
        | TRACE   -> HttpMethods.Trace
        | ANY     -> String.Empty

/// The eventual return of asynchronous HttpContext processing
type HttpHandler =
    HttpContext -> Task

module HttpHandler =
    /// Convert HttpHandler to a RequestDelegate
    let toRequestDelegate (handler : HttpHandler) =
        new RequestDelegate(handler)

/// In-and-out processing of a HttpContext
type HttpResponseModifier = HttpContext -> HttpContext

/// Specifies an association of a route pattern to a collection of HttpEndpointHandler
type HttpEndpoint =
    { Pattern  : string
      Handlers : (HttpVerb * HttpHandler) list }