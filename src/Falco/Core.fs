namespace Falco

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

/// The eventual return of asynchronous HttpContext processing.
type HttpHandler = HttpContext -> Task

module HttpHandler =
    /// Convert HttpHandler to a RequestDelegate.
    let toRequestDelegate (handler : HttpHandler) =
        new RequestDelegate(handler)

/// A function that extracts 'a from the HttpContext.
type HttpContextAccessor<'a> = HttpContext -> 'a

/// A function that asynchronously extracts 'a from the HttpContext.
type AsyncHttpContextAccessor<'a> = HttpContext -> Task<'a>

/// In-and-out processing of a HttpContext.
type HttpResponseModifier = HttpContext -> HttpContext

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

/// Specifies an association of a route pattern to a collection of
/// HttpEndpointHandler.
type HttpEndpoint =
    { Pattern  : string
      Handlers : (HttpVerb * HttpHandler) list }