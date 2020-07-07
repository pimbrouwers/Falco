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

let tryBindForm    
    (bind : FormCollectionReader -> Result<'a, string>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = ctx.Request.GetFormReaderAsync ()            
        return form |> bind
    }

let tryBindFormStream
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

let tryBindQuery    
    (bind : StringCollectionReader -> Result<'a, string>) 
    (ctx : HttpContext) : Result<'a, string> = 
    ctx.Request.GetQueryReader () 
    |> bind