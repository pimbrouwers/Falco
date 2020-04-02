[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

exception InvalidDependencyException of string

type HttpFuncResult = Task<HttpContext option>

type HttpFunc = HttpContext -> HttpFuncResult

type HttpHandler = HttpFunc -> HttpFunc    
    
type HttpVerb = GET | POST | PUT | DELETE | ANY

type HttpEndpoint = 
    {
        Pattern : string   
        Verb  : HttpVerb
        Handler : HttpHandler
    }

let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
    fun (final : HttpFunc) ->
        let func = final |> handler2 |> handler1
        fun (ctx : HttpContext) ->
            match ctx.Response.HasStarted with
            | true  -> final ctx
            | false -> func ctx
        
let (>=>) = compose
    
// strings    
let toStr x = x.ToString()

let strJoin (sep : string) (lst : string array) = String.Join(sep, lst)
        
// parsing
let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
    | true, v    -> Some v
    | false, _   -> None
            
let parseInt = tryParseWith Int32.TryParse