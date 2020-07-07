[<RequireQualifiedAccess>]
module Falco.Request

open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

type BindStringCollection<'a> = 
    StringCollectionReader -> Result<'a, string>

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
    (bind : BindStringCollection<'a>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = ctx.Request.GetFormReaderAsync ()            
        return form |> bind
    }

let tryBindJson<'a>
    (ctx : HttpContext) : Task<'a> = 
    let opt = JsonSerializerOptions()
    opt.AllowTrailingCommas <- true
    opt.PropertyNameCaseInsensitive <- true    
    JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, opt).AsTask()

let tryBindQuery    
    (bind : BindStringCollection<'a>) 
    (ctx : HttpContext) : Result<'a, string> = 
    ctx.Request.GetQueryReader () 
    |> bind