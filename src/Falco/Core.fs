[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

// ------------
// HTTP
// ------------

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


// ------------
// Errors 
// ------------

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

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

// ------------
// Multipart
// ------------

/// Represents the accumulation of form fields and binary data
type MultipartFormData = 
    {
        FormData : KeyValueAccumulator
        FormFiles : FormFileCollection
    }

type MultipartSection with
    /// Attempt to obtain encoding from content type, default to UTF8
    static member GetEncondingFromContentType (section : MultipartSection) =
        match MediaTypeHeaderValue.TryParse(StringSegment(section.ContentType)) with
        | false, _     -> System.Text.Encoding.UTF8
        | true, parsed -> 
            match System.Text.Encoding.UTF7.Equals(parsed.Encoding) with
            | true -> System.Text.Encoding.UTF8
            | false -> parsed.Encoding

    /// Safely obtain the content disposition header value
    static member TryGetContentDisposition(section : MultipartSection) =                        
        match ContentDispositionHeaderValue.TryParse(StringSegment(section.ContentDisposition)) with
        | false, _     -> None
        | true, parsed -> Some parsed