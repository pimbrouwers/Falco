[<AutoOpen>]
module Falco.ErrorHandling

open System
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

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
            | ex -> 
                let logger = log.CreateLogger<ExceptionHandlingMiddleware>()
                logger.LogError(ex, "Unhandled exception throw, attempting to handle")
                try
                    let! _ = handler ex logger defaultHttpFunc ctx
                    return ()
                with
                | ex ->
                    logger.LogError(ex, "Exception thrown while handling exception")
        }
