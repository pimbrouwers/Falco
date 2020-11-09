[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Falco.Security

/// Obtain the HttpVerb of the request
let getVerb 
    (ctx : HttpContext) : HttpVerb =
    ctx.Request.HttpVerb

/// Retrieve a specific header from the request
let getHeader 
    (headerName : string)
    (ctx : HttpContext) : string[] =
    ctx.Request.GetHeader headerName

/// Retrieve all route values from the request as Map<string, string>
let getRouteValues
    (ctx : HttpContext) : Map<string, string> =
    ctx.Request.GetRouteValues ()

let tryBindRoute 
    (binder : Map<string, string> -> Result<'a, 'b>) 
    (ctx : HttpContext) : Result<'a, 'b> =
    getRouteValues ctx
    |> binder

/// Attempt to retrieve a specific route value from the request
let tryGetRouteValue
    (key : string) 
    (ctx : HttpContext) : string option =
    ctx.Request.TryGetRouteValue key

/// Retrieve the query string from the request as an instance of StringCollectionReader
let getQuery
    (ctx : HttpContext) : StringCollectionReader =
    ctx.Request.GetQueryReader ()

/// Attempt to bind query collection
let tryBindQuery    
    (binder : StringCollectionReader -> Result<'a, 'b>) 
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

/// Project route values map onto 'a and feed into next HttpHandler
let mapRoute 
    (map : Map<string, string> -> 'a) 
    (next : 'a -> HttpHandler) : HttpHandler =
    fun ctx -> next (getRouteValues ctx |> map) ctx

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
    (binder : Map<string, string> -> Result<'a, 'b>) 
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = 
    fun ctx -> 
        let route = tryBindRoute binder ctx
        let respondWith =
            match route with
            | Error error -> handleError error
            | Ok query -> handleOk query
    
        respondWith ctx

/// Attempt to bind the StringCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindQuery
    (binder : StringCollectionReader -> Result<'a, 'b>)        
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
    (handleInvalid : HttpHandler) : HttpHandler =
    fun ctx -> task {
        let! isValid = Xss.validateToken ctx

        let respondWith =
            match isValid with 
            | true  -> handleOk
            | false -> handleInvalid

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
