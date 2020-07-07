[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let getRouteValues
    (ctx : HttpContext) =
    ctx.Request.GetRouteValues()

let getVerb 
    (ctx : HttpContext) : HttpVerb =
    ctx.Request.HttpVerb

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
    ctx.Request.GetQueryReader () 
    |> bind

let getForm
    (ctx : HttpContext) : FormCollectionReader = 
    ctx.Request.GetFormReader()    

let getFormAsync
    (ctx : HttpContext) : Task<FormCollectionReader> = 
    ctx.Request.GetFormReaderAsync()    

let tryBindForm 
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Result<'a, string> = 
    getForm ctx
    |> bind

let tryBindFormAsync
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = ctx.Request.GetFormReaderAsync ()            
        return form |> bind
    }

let tryStreamFormAsync
    (ctx : HttpContext) : Task<Result<FormCollectionReader, string>> = task {
        return! ctx.Request.TryStreamFormAsync()
    }

let tryBindFormStreamAsync
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = ctx.Request.TryStreamFormAsync ()                    
        return form |> Result.bind bind
    }

let tryBindJson<'a>
    (ctx : HttpContext) : Task<'a> = 
    let opt = JsonSerializerOptions()
    opt.AllowTrailingCommas <- true
    opt.PropertyNameCaseInsensitive <- true    
    JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, opt).AsTask()