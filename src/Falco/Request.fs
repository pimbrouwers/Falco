module Falco.Request

open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

type BindStringCollection<'a> = 
    StringCollectionReader -> Result<'a, string>

let getVerb 
    (ctx : HttpContext) : HttpVerb =
    ctx.Request.HttpVerb

let tryBindForm
    (bind : BindStringCollection<'a>)
    (ctx : HttpContext) : Task<Result<'a, string>> = task {
        let! form = ctx.Request.GetFormReaderAsync ()            
        return form |> bind
    }

let tryBindQuery
    (bind : BindStringCollection<'a>)
    (ctx : HttpContext) : Result<'a, string> = 
    ctx.Request.GetQueryReader () 
    |> bind
