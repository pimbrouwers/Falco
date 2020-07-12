[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let getVerb 
    (ctx : HttpContext) : HttpVerb =
    ctx.Request.HttpVerb

let getRouteValues
    (ctx : HttpContext) =
    ctx.Request.GetRouteValues()

let tryGetRouteValue 
    (key : string) 
    (ctx : HttpContext) : string option =
    ctx.Request.TryGetRouteValue key

let getQuery
    (ctx : HttpContext) : StringCollectionReader =
    ctx.Request.GetQueryReader()

let tryBindQuery    
    (bind : StringCollectionReader -> Result<'a, string>) 
    (ctx : HttpContext) : Result<'a, string> = 
    getQuery ctx 
    |> bind

let getForm
    (ctx : HttpContext) : Task<FormCollectionReader> = 
    ctx.Request.GetFormReaderAsync()    

let tryBindForm
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = getForm ctx
        return form |> bind
    }

let tryStreamForm
    (ctx : HttpContext) : Task<Result<FormCollectionReader, string>> = task {
        return! ctx.Request.TryStreamFormAsync()
    }

let tryBindJson<'a>
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
    let options = JsonSerializerOptions()
    options.AllowTrailingCommas <- true
    options.PropertyNameCaseInsensitive <- true  
    try
        let! result = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options).AsTask()
        return Ok result        
    with
    :? JsonException as ex -> return Error ex.Message
}

let tryBindJsonOptions<'a>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<Result<'a, string>> = task { 
    try
        let! result = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options).AsTask()
        return Ok result        
    with
    :? JsonException as ex -> return Error ex.Message
}