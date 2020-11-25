module HelloWorld.Program

open Falco
open Falco.Routing
open Falco.HostBuilder

// ------------
// Handlers 
// ------------
let handlePlainText : HttpHandler =
    Response.ofPlainText "Hello from /"

[<EntryPoint>]
let main args =      
    webHost args {
        endpoints [            
            get "/" handlePlainText
        ]
    }        
    0    