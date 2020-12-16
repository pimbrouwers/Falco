[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Falco.Multipart
open Falco.Security
open System.Security.Claims

/// Obtain the HttpVerb of the request
let getVerb 
    (ctx : HttpContext) : HttpVerb =
    ctx.Request.HttpVerb

/// Retrieve a specific header from the request
let getHeaders (ctx : HttpContext) : HeaderCollectionReader  =
    ctx.Request.GetHeaderReader ()

/// Retrieve all route values from the request as Map<string, string>
let getRoute (ctx : HttpContext) : RouteCollectionReader =
    ctx.Request.GetRouteReader ()

let tryBindRoute 
    (binder : RouteCollectionReader -> Result<'a, 'b>) 
    (ctx : HttpContext) : Result<'a, 'b> =
    getRoute ctx
    |> binder

/// Retrieve the query string from the request as an instance of QueryCollectionReader
let getQuery
    (ctx : HttpContext) : QueryCollectionReader =
    ctx.Request.GetQueryReader ()

/// Attempt to bind query collection
let tryBindQuery    
    (binder : QueryCollectionReader -> Result<'a, 'b>) 
    (ctx : HttpContext) : Result<'a, 'b> = 
    getQuery ctx 
    |> binder

/// Retrieve the form collection from the request as an instance of FormCollectionReader
let getForm
    (ctx : HttpContext) : Task<FormCollectionReader> = 
    ctx.Request.GetFormReaderAsync ()    

/// Attempt to bind the form collection
let tryBindForm
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Task<Result<'a, 'b>> = task {
        let! form = getForm ctx
        return form |> binder
    }

/// Attempt to stream the form collection for multipart form submissions
let tryStreamForm
    (ctx : HttpContext) : Task<Result<FormCollectionReader, string>> = 
    ctx.Request.TryStreamFormAsync()

/// Attempt to bind request body using System.Text.Json and provided JsonSerializerOptions
let tryBindJsonOptions<'a>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<Result<'a, string>> = task { 
    try
        let! result = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options).AsTask()
        return Ok result        
    with
    :? JsonException as ex -> return Error ex.Message
}
    
/// Attempt to bind request body using System.Text.Json
let tryBindJson<'a>
    (ctx : HttpContext) : Task<Result<'a, string>> = 
    tryBindJsonOptions Constants.defaultJsonOptions ctx

// ------------
// Handlers
// ------------

/// Attempt to bind JSON request body onto ' and provider
/// to handleOk, otherwise provide handleError with error string
let bindJson
    (handleOk : 'a -> HttpHandler)
    (handleError : string -> HttpHandler) : HttpHandler = 
    fun ctx -> task {
        let! form = tryBindJson ctx
        let respondWith =
            match form with
            | Error error -> handleError error
            | Ok form -> handleOk form

        return! respondWith ctx
    }

/// Attempt to bind the route values map onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindRoute
    (binder : RouteCollectionReader -> Result<'a, 'b>) 
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = 
    fun ctx -> 
        let route = tryBindRoute binder ctx
        let respondWith =
            match route with
            | Error error -> handleError error
            | Ok query -> handleOk query
    
        respondWith ctx

/// Attempt to bind the QueryCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindQuery
    (binder : QueryCollectionReader -> Result<'a, 'b>)        
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = 
    fun ctx -> 
        let query = tryBindQuery binder ctx
        let respondWith =
            match query with
            | Error error -> handleError error
            | Ok query -> handleOk query
    
        respondWith ctx
    
/// Attempt to bind the FormCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindForm
    (binder : FormCollectionReader -> Result<'a, 'b>)        
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = 
    fun ctx -> task {
        let! form = tryBindForm binder ctx
        let respondWith =
            match form with
            | Error error -> handleError error
            | Ok form -> handleOk form

        return! respondWith ctx
    }

    
/// Validate the CSRF of the current request
let validateCsrfToken
    (handleOk : HttpHandler) 
    (handleInvalidToken : HttpHandler) : HttpHandler =
    fun ctx -> task {
        let! isValid = Xss.validateToken ctx

        let respondWith =
            match isValid with 
            | true  -> handleOk
            | false -> handleInvalidToken

        return! respondWith ctx
    }

/// Validate the CSRF of the current request attempt to bind the
/// FormCollectionReader onto 'a and provide to handleOk,
/// otherwise provide handleError with 'b
let bindFormSecure
    (binder : FormCollectionReader -> Result<'a, 'b>)    
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) 
    (handleInvalidToken : HttpHandler) : HttpHandler =    
    validateCsrfToken 
        (bindForm binder handleOk handleError)
        handleInvalidToken

/// Project RouteCollectionReader onto 'a and provide 
/// to next HttpHandler
let mapRoute 
    (map : RouteCollectionReader -> 'a) 
    (next : 'a -> HttpHandler) : HttpHandler =
    fun ctx -> next (getRoute ctx |> map) ctx

/// Project QueryCollectionReader onto 'a and provide
/// to next HttpHandler
let mapQuery
    (map : QueryCollectionReader -> 'a)        
    (next : 'a -> HttpHandler) : HttpHandler = 
    fun ctx -> next (getQuery ctx |> map) ctx

/// Project FormCollectionReader onto 'a and provide
/// to next HttpHandler
let mapForm
    (map : FormCollectionReader -> 'a)        
    (next : 'a -> HttpHandler) : HttpHandler =
    fun ctx -> task {
        let! form = getForm ctx
        return! next (form |> map) ctx
    }

/// Project FormCollectionReader onto 'a and provide
/// to next HttpHandler
let mapFormSecure
    (map : FormCollectionReader -> 'a)        
    (next : 'a -> HttpHandler)    
    (handleInvalidToken : HttpHandler) : HttpHandler =
        validateCsrfToken
            (mapForm map next)
            handleInvalidToken

/// Proceed if the authentication status of current IPrincipal is true
let ifAuthenticated 
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        if Auth.isAuthenticated ctx then handleOk ctx
        else handleError ctx

/// Proceed if the authentication status of current IPrincipal is true
/// and they exist in a list of roles
let ifAuthenticatedInRole
    (roles : string list)
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        let isAuthenticated = Auth.isAuthenticated ctx
        let isInRole = Auth.isInRole roles ctx

        match isAuthenticated, isInRole with
        | true, true -> handleOk ctx
        | _          -> handleError ctx
        
/// Proceed if the authentication status of current IPrincipal is true
/// and has a specific scope
let ifAuthenticatedWithScope
    (scope : string)
    (issuer : string)
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        let isAuthenticated = Auth.isAuthenticated ctx
        let predicate (claim : Claim) =
            claim.Type = "scope" &&
            claim.Issuer = issuer &&
            claim.Value.Split [|' '|] |> Array.exists (fun value ->  value = scope)
        let claimOpt = Auth.tryFindClaim predicate ctx

        match isAuthenticated, claimOpt with
        | true, Some _ -> handleOk ctx
        | _            -> handleError ctx

/// Proceed if the authentication status of current IPrincipal is false
let ifNotAuthenticated 
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        if Auth.isAuthenticated ctx then handleError ctx
        else handleOk ctx