[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

// Exceptions
exception InvalidDependencyException of string

// Types
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

// Kleisli Composition
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

let strEquals s1 s2 = String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase)
let strJoin (sep : string) (lst : string array) = String.Join(sep, lst)
        
// parsing
let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
    | true, v    -> Some v
    | false, _   -> None
            
let parseInt            = tryParseWith Int32.TryParse
let parseInt16          = tryParseWith Int16.TryParse
let parseInt32          = parseInt
let parseInt64          = tryParseWith Int64.TryParse
let parseBoolean        = tryParseWith Boolean.TryParse
let parseFloat          = tryParseWith Double.TryParse
let parseDecimal        = tryParseWith Decimal.TryParse
let parseDateTime       = tryParseWith DateTime.TryParse
let parseDateTimeOffset = tryParseWith DateTimeOffset.TryParse
let parseTimeSpan       = tryParseWith TimeSpan.TryParse
let parseGuid           = tryParseWith Guid.TryParse
