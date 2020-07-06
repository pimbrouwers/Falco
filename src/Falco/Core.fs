[<AutoOpen>]
module Falco.Core

open System
open System.IO
open System.Security.Claims
open System.Text
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Antiforgery    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.StringUtils
open Falco.StringParser

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

/// Http verb
type HttpVerb = 
    | GET 
    | HEAD
    | POST 
    | PUT 
    | PATCH
    | DELETE 
    | OPTIONS
    | TRACE
    | ANY

/// The eventual return of asynchronous HttpContext processing
type HttpHandler = 
    HttpContext -> Task

module HttpHandler =
    /// Convert HttpHandler to a RequestDelegate
    let toRequestDelegate (handler : HttpHandler) =        
        new RequestDelegate(handler)

/// Specifies an association of an HttpHandler to an HttpVerb and route pattern
type HttpEndpoint = 
    {
        Pattern : string   
        Verb    : HttpVerb
        Handler : HttpHandler
    }

/// The process of associating a route and handler
type MapHttpEndpoint = string -> HttpHandler -> HttpEndpoint

/// Represents an HttpHandler intended for use as the global exception handler
/// Receives the thrown exception, and logger
type ExceptionHandler = Exception -> ILogger -> HttpHandler

type ExceptionHandlingMiddleware (next : RequestDelegate, 
                                  handler: ExceptionHandler, 
                                  log : ILoggerFactory) =
    do
        if isNull next     then failwith "next cannot be null"
        else if isNull log then failwith "handler cannot be null"

    member __.Invoke(ctx : HttpContext) =
        task {
            try return! next.Invoke ctx
            with 
            | :? AggregateException as requestDelegateException -> 
                let logger = log.CreateLogger<ExceptionHandlingMiddleware>()                
                logger.LogError(requestDelegateException, "Unhandled exception throw, attempting to handle")
                try
                    let! _ = handler requestDelegateException logger ctx
                    return ()
                with
                | :? AggregateException as handlerException ->                               
                    logger.LogError(handlerException, "Exception thrown while handling exception")
        }

type HttpContext with       
    // ------------
    // IoC & Logging
    // ------------

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

    // ------------
    // XSS
    // ------------

    /// Returns (and optional creates) csrf tokens for the current session
    member this.GetCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()
        antiFrg.GetAndStoreTokens this

    /// Checks the presence and validity of CSRF token 
    member this.ValidateCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()        
        antiFrg.IsRequestValidAsync this

    // ------------
    // Auth
    // ------------
    /// Returns the current user (IPrincipal) or None
    member this.GetUser () =
        match this.User with
        | null -> None
        | _    -> Some this.User

    /// Returns authentication status of IPrincipal, false on null
    member this.IsAuthenticated () =
        let isAuthenciated (user : ClaimsPrincipal) = 
            let identity = user.Identity
            match identity with 
            | null -> false
            | _    -> identity.IsAuthenticated

        match this.GetUser () with
        | None      -> false 
        | Some user -> isAuthenciated user

type HttpRequest with   
    /// The HttpVerb of the current request
    member this.HttpVerb = 
        match this.Method with 
        | m when strEquals m HttpMethods.Get     -> GET
        | m when strEquals m HttpMethods.Head    -> HEAD
        | m when strEquals m HttpMethods.Post    -> POST
        | m when strEquals m HttpMethods.Put     -> PUT
        | m when strEquals m HttpMethods.Patch   -> PATCH
        | m when strEquals m HttpMethods.Delete  -> DELETE
        | m when strEquals m HttpMethods.Options -> OPTIONS
        | m when strEquals m HttpMethods.Trace   -> TRACE
        | _ -> ANY

    /// Obtain Map<string,string> of current route values
    member this.GetRouteValues () =
        this.RouteValues
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToString())
        |> Map.ofSeq
    
    /// Attempt to safely-acquire route value
    member this.TryGetRouteValue (key : string) =
        let parseRoute = tryParseWith this.RouteValues.TryGetValue             
        match parseRoute key with
        | Some v -> Some (v.ToString())
        | None   -> None

    /// Retrieve the HttpRequest body as string
    member this.GetBodyAsync () = task {
        use rd = new StreamReader(this.Body)
        return! rd.ReadToEndAsync()
    }

    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = task {
        let! form = this.ReadFormAsync()
        return StringCollectionReader(form)
    }        

    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        StringCollectionReader(this.Query)

type HttpResponse with
    /// Set HttpResponse header
    member this.SetHeader 
        (name : string) 
        (content : string) =            
        if not(this.Headers.ContainsKey(name)) then
            this.Headers.Add(name, StringValues(content))

    /// Set HttpResponse ContentType header
    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    member this.SetStatusCode (statusCode : int) =            
        this.StatusCode <- statusCode
            
    /// Write bytes to HttpResponse body
    member this.WriteBytes (bytes : byte[]) =
        let byteLen = bytes.Length
        this.ContentLength <- Nullable<int64>(byteLen |> int64)
        this.Body.WriteAsync(bytes, 0, byteLen)            

    /// Write UTF8 string to HttpResponse body
    member this.WriteString (encoding : Encoding) (httpBodyStr : string) =
        let httpBodyBytes = encoding.GetBytes httpBodyStr
        this.WriteBytes httpBodyBytes

type IApplicationBuilder with
    /// Enable Falco exception handling middleware. 
    ///
    /// It is recommended to specify this BEFORE any other middleware.
    member this.UseExceptionMiddleware (exceptionHandler : ExceptionHandler) =
        this.UseMiddleware<ExceptionHandlingMiddleware> exceptionHandler

    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseHttpEndPoints (endPoints : HttpEndpoint list) =
        this.UseEndpoints(fun r -> 
                for e in endPoints do            
                    let rd = HttpHandler.toRequestDelegate e.Handler
                    
                    match e.Verb with
                    | GET     -> r.MapGet(e.Pattern, rd)
                    | HEAD    -> r.MapMethods(e.Pattern, [ HttpMethods.Head ], rd)
                    | POST    -> r.MapPost(e.Pattern, rd)
                    | PUT     -> r.MapPut(e.Pattern, rd)
                    | PATCH   -> r.MapMethods(e.Pattern, [ HttpMethods.Patch ], rd)
                    | DELETE  -> r.MapDelete(e.Pattern, rd)
                    | OPTIONS -> r.MapMethods(e.Pattern, [ HttpMethods.Options ], rd)
                    | TRACE   -> r.MapMethods(e.Pattern, [ HttpMethods.Trace ], rd)
                    | ANY     -> r.Map(e.Pattern, rd)
                    |> ignore)
            
    /// Enable Falco not found handler.
    ///
    /// This handler is terminal and must be specified 
    /// AFTER all other middlewar.
    member this.UseNotFoundHandler (notFoundHandler : HttpHandler) =
        this.Run(HttpHandler.toRequestDelegate notFoundHandler)