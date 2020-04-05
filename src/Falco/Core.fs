[<AutoOpen>]
module Falco.Core

open System
open System.Text
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

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
let parseOrFail parser msg v =
    match parser v with 
    | Some v -> v
    | None   -> failwith msg

let tryParseArray tryParser ary =
    ary
    |> Seq.map tryParser
    |> Seq.fold (fun acc i ->
        match (i, acc) with
        | Some i, Some acc -> Some (Array.append acc [|i|])
        | _ -> None) (Some [||])    

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

type HttpContext with        
    member this.GetService<'a> () =
        let t = typeof<'a>
        match this.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a

    member this.SetStatusCode (statusCode : int) =            
        this.Response.StatusCode <- statusCode

    member this.SetHeader name (content : string) =            
        if not(this.Response.Headers.ContainsKey(name)) then
            this.Response.Headers.Add(name, StringValues(content))

    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    member this.WriteBytes (bytes : byte[]) =        
        task {            
            let len = bytes.Length
            bytes.CopyTo(this.Response.BodyWriter.GetMemory(len).Span)
            this.Response.BodyWriter.Advance(len)
            this.Response.BodyWriter.FlushAsync(this.RequestAborted) |> ignore
            this.Response.ContentLength <- Nullable<int64>(len |> int64)
            return Some this
        }

    member this.WriteString (str : string) =
        this.WriteBytes (Encoding.UTF8.GetBytes str)