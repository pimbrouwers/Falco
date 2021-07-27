module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Logging

// ------------
// Handlers 
// ------------
let handlePlainText : HttpHandler =
    "Hello world"
    |> Response.ofPlainText 

let handleJson : HttpHandler =
    {| Message = "Hello world" |}
    |> Response.ofJson 

let handleHtml : HttpHandler =
    Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello world" ] ]
    |> Response.ofHtml

[<EntryPoint>]
let main args =      
    webHost args {
        logging (fun log -> log.ClearProviders())

        endpoints [               
            get "/html" handleHtml 

            get "/json" handleJson

            get "/" handlePlainText
        ]
    }        
    0    