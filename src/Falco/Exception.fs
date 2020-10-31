[<AutoOpen>]
module Falco.Exception

open System
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

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

