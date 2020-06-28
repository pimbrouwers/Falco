[<AutoOpen>]
module Falco.Core

open System    
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string
    
/// The optional result of work performed against the HttpContext
type HttpFuncResult = Task<HttpContext option>

/// Specifies work to be performed against the HttpContext
type HttpFunc = HttpContext -> HttpFuncResult

/// Represents in-and-out processing of the HttpContext
type HttpHandler = HttpFunc -> HttpFunc    

/// The default HttpFunc
let defaultHttpFunc : HttpFunc = 
    Some >> Task.FromResult

/// Compose ("glue") HttpHandler's together
let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
    fun (fn : HttpFunc) ->
        let next = fn |> handler2 |> handler1
        fun (ctx : HttpContext) ->
            match ctx.Response.HasStarted with
            | true  -> fn ctx
            | false -> next ctx
        
let (>=>) = compose

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
    member this.SetHeader name (content : string) =            
        if not(this.Response.Headers.ContainsKey(name)) then
            this.Response.Headers.Add(name, StringValues(content))

    /// Set HttpResponse ContentType header
    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    /// Write bytes to HttpResponse body
    member this.WriteBytes (bytes : byte[]) =                    
        let len = bytes.Length
        this.Response.ContentLength <- Nullable<int64>(len |> int64)
        this.Response.Body
            .WriteAsync(bytes, 0, len)
            .ContinueWith(fun _ -> Some this)

    /// Write UTF8 string to HttpResponse body
    member this.WriteString (str : string) =
        this.WriteBytes (Encoding.UTF8.GetBytes str)

[<AutoOpen>]
module Extensions =            
    /// Credit: Jimmy Byrd
    /// Link: https://github.com/TheAngryByrd/TheAngryByrd.AsyncEx/blob/master/tests/TheAngryByrd.AsyncEx.Tests/Tests.fs#L54

    open System.Threading
    open System.Threading.Tasks

    type CancellableTask<'a> = CancellationToken -> Task<'a>
    type CancellableUnitTask = CancellationToken -> Task

    type AsyncBuilder with
        member inline __.Bind(t : Task<'a>, cont) = async.Bind(t |> Async.AwaitTask, cont)
        member inline __.Bind(t : Task, cont) = async.Bind(t |> Async.AwaitTask, cont)
        member inline __.ReturnFrom(t : Task<'a>) = async.ReturnFrom(t |> Async.AwaitTask)
        member inline __.ReturnFrom(t : Task) = async.ReturnFrom(t |> Async.AwaitTask)
        member inline __.Bind(t : CancellableTask<'a>, cont : 'a -> Async<'b>) =
            let augmented = async {
                let! ct = Async.CancellationToken
                return! t ct
            }
            async.Bind(augmented, cont)
        member inline __.Bind(t : CancellableUnitTask, cont : _ -> Async<_>) =
            let augmented = async {
                let! ct = Async.CancellationToken
                return! t ct
            }
            async.Bind(augmented, cont)
        member inline __.ReturnFrom(t : CancellableTask<'a>) =
            let augmented = async {
                let! ct = Async.CancellationToken
                return! t ct
            }
            async.ReturnFrom augmented

        member inline __.ReturnFrom(t : CancellableUnitTask) =
            let augmented = async {
                let! ct = Async.CancellationToken
                return! t ct
            }
            async.ReturnFrom augmented


    