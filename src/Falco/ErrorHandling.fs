[<AutoOpen>]
module Falco.ErrorHandling

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging


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
        try 
            next.Invoke ctx
        with 
        | ex -> 
            let logger = log.CreateLogger<ExceptionHandlingMiddleware>()
            logger.LogError(ex, "Unhandled exception throw, attempting to handle")
            try
                Task.Run(fun _ -> handler ex logger defaultHttpFunc ctx |> ignore)
            with
            | ex ->
                logger.LogError(ex, "Exception thrown while handling exception")
                Task.CompletedTask
