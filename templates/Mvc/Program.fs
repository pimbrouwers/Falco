module AppName.Program

open Falco
open Falco.HostBuilder
open Falco.Routing
open Microsoft.AspNetCore.Builder

// ------------
// Exception Handler
// ------------
let exceptionHandler : HttpHandler =
    Response.withStatusCode 500 
    >> Response.ofPlainText "Server error"

[<EntryPoint>]
let main args =    
    webHost args {
        add_antiforgery

        use_static_files
        use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
        use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler exceptionHandler)
        
        endpoints [            
            all Urls.``/value/create``
                [
                    GET,  Value.Controller.create
                    POST, Value.Controller.createSubmit
                ]

            get Urls.``/``
                Value.Controller.index
        ]
    }       
    0
    