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
    (bind : StringCollectionReader -> Result<'a, string>) 
    (ctx : HttpContext) : Result<'a, string> = 
    getQuery ctx 
    |> bind

/// Retrieve the form collection from the request as an instance of FormCollectionReader
let getForm
    (ctx : HttpContext) : Task<FormCollectionReader> = 
    ctx.Request.GetFormReaderAsync ()    

/// Attempt to bind the form collection
let tryBindForm
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = getForm ctx
        return form |> bind
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

/// Attempt to bind the FormCollectionReader of the current
/// request with the provided binder
let bindForm<'a>
    (binder : FormCollectionReader -> Result<'a, string>)        
    (handleOk : 'a -> HttpHandler)
    (handleError : string list -> HttpHandler) : HttpHandler = 
    fun ctx -> task {
        let! form = tryBindForm binder ctx
        let respondWith =
            match form with
            | Error error -> handleError [error]
            | Ok form -> handleOk form

        return! respondWith ctx
    }

/// Validate the CSRF of the current request attempt to bind 
/// the FormCollectionReader of with the provided binder
let bindFormSecure<'a>
    (binder : FormCollectionReader -> Result<'a, string>)    
    (handleOk : 'a -> HttpHandler)
    (handleError : string list -> HttpHandler) 
    (handleInvalidToken : HttpHandler) : HttpHandler =    
    validateCsrfToken 
        (bindForm binder handleOk handleError)
        handleInvalidToken