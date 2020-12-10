module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

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


let handleHtmlFile : HttpHandler =
    "static/index.html"
    |> Response.ofHtmlFile


[<EntryPoint>]
let main args =      
    webHost args {
        endpoints [            
            get "/htmlfile" handleHtmlFile

            get "/html" handleHtml 

            get "/json" handleJson

            get "/" handlePlainText
        ]
    }        
    0    