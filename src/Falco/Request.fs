[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

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