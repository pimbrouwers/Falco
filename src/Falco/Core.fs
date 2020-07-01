[<AutoOpen>]
module Falco.Core

open System    
open System.Text
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

type HttpFuncResult = Task<HttpContext option>

/// HttpFunc functions are functions that have access to the 
/// HttpContext (request & response) and can be chained together
/// to sequentially process the HTTP request
type HttpFunc = HttpContext -> HttpFuncResult

module HttpFunc =      
    let ofSome : HttpFunc =
        fun (ctx : HttpContext) -> 
            Some ctx 
            |> Task.FromResult

/// The default HttpFunc which accepts an HttpContext
/// and simply passes it through
let earlyReturn : HttpFunc = 
    HttpFunc.ofSome

/// HttpHandler represents an HttpFunc to run which
/// has access to the next middleware in the pipeline.
type HttpHandler = HttpFunc -> HttpFunc

module HttpHandler =
    /// Compose ("glue") HttpHandler's together. Consider using
    /// ">=>" for more elegant handler composition (i.e. handler1 >=> handler2)
    let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
        fun (fn : HttpFunc) ->
            let next : HttpFunc = 
                fn 
                |> handler2 
                |> handler1

            fun (ctx : HttpContext) ->
                match ctx.Response.HasStarted with
                | true  -> fn ctx
                | false -> next ctx
        
let (>=>) = HttpHandler.compose

type HttpContext with         
    /// Attempt to obtain depedency from IServiceCollection
    /// Throws InvalidDependencyException on null
    member this.GetService<'a> () =
        let t = typeof<'a>
        match this.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a

    /// Obtain a named instance of ILogger
    member this.GetLogger (name : string) =
        let loggerFactory = this.GetService<ILoggerFactory>()
        loggerFactory.CreateLogger name

    /// Set HttpResponse status code
    member this.SetStatusCode (statusCode : int) =            
        this.Response.StatusCode <- statusCode

    /// Set HttpResponse header
    member this.SetHeader 
        (name : string) 
        (content : string) =            
        if not(this.Response.Headers.ContainsKey(name)) then
            this.Response.Headers.Add(name, StringValues(content))

    /// Set HttpResponse ContentType header
    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    /// Write bytes to HttpResponse body
    member this.WriteBytes (bytes : byte[]) =
        let byteLen = bytes.Length
        this.Response.ContentLength <- Nullable<int64>(byteLen |> int64)
        this.Response.Body.WriteAsync(bytes, 0, byteLen)            

    /// Write UTF8 string to HttpResponse body
    member this.WriteString (encoding : Encoding) (httpBodyStr : string) =
        let httpBodyBytes = encoding.GetBytes httpBodyStr
        this.WriteBytes httpBodyBytes