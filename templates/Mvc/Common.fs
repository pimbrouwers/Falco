module AppName.Common

open Falco

type ServiceHandler<'input, 'output, 'error> = 'input -> Result<'output, 'error>
type ServiceQuery<'output, 'error> = ServiceHandler<unit, 'output, 'error>
type ServiceCommand<'input, 'error> = ServiceHandler<'input, unit, 'error>

let runService
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