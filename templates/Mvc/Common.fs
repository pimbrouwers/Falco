module AppName.Common

open Falco

type ServiceHandler<'input, 'output, 'error> = 'input -> Result<'output, 'error>
type ServiceQuery<'output, 'error> = ServiceHandler<unit, 'output, 'error>
type ServiceCommand<'input, 'error> = ServiceHandler<'input, unit, 'error>

module Handlers =
    let invalidCsrfToken : HttpHandler = 
        Response.withStatusCode 400 >> Response.ofPlainText "Bad request"

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

[<RequireQualifiedAccess>]
module Urls = 
    let ``/`` = "/"
    let ``/todo/create`` = "/todo/create"
    let ``/todo/complete/{index:int}`` = sprintf "/todo/complete/%i"
    let ``/todo/incomplete/{index:int}`` = sprintf "/todo/incomplete/%i"