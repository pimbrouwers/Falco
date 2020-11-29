module Todo.Service

open Falco

/// Work to be done that has input and will generate output
type ServiceHandler<'input, 'output, 'error> = 'input -> Result<'output, 'error>

/// Work to be done that has no input and will generate output
type ServiceQuery<'output, 'error> =  ServiceHandler<unit, 'output, 'error>

/// Work to be done that has input and won't generate output
type ServiceCommand<'input, 'error> = ServiceHandler<'input, unit, 'error>

/// An HttpHandler to execute services, and can help reduce code
/// repetition by acting as a composition root for injecting
/// dependencies for logging, database, http etc.
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

let runQuery serviceQuery handleOk handleError = 
    run serviceQuery handleOk handleError ()