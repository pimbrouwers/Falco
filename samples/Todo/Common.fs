module Todo.Common

open Falco

/// Work to be done that will generate output
type ServiceHandler<'input, 'output, 'error> = 'input -> Result<'output, 'error>

/// Work to be done that won't generate output
type ServiceCommand<'input, 'error> = ServiceHandler<'input, unit, 'error>

/// Common HttpHandler's
module ErrorHandler =
    let invalidCsrfToken : HttpHandler = 
        Response.withStatusCode 400 
        >> Response.ofPlainText "Bad request"

/// An HttpHandler to execute services, and can help reduce code
/// repetition by acting as a composition root for injecting
/// dependencies for logging, database, http etc.
module Service =
    let run
        (serviceHandler: ServiceHandler<'input, 'output, 'error>)
        (handleOk : 'output -> HttpHandler)
        (handleError : 'error -> HttpHandler)
        (input : 'input) : HttpHandler =
        fun ctx ->        
            let respondWith = 
                match serviceHandler input with
                | Ok output -> handleOk output
                | Error error -> handleError error

            respondWith ctx

/// Internal URLs
[<RequireQualifiedAccess>]
module Urls = 
    let ``/`` = "/"
    let ``/todo/create`` = "/todo/create"
    let ``/todo/complete/{id}`` = sprintf "/todo/complete/%s"
    let ``/todo/incomplete/{id}`` = sprintf "/todo/incomplete/%s"