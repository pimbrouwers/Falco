module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

// ------------
// Handlers
// ------------
let handlePlainText : HttpHandler =
    Response.ofPlainText "Hello world"

let handleJson : HttpHandler =
    let message = {| Message = "Hello world" |}
    Response.ofJson message

let handleHtml : HttpHandler =
    let html = Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello world" ] ]
    Response.ofHtml html

[<EntryPoint>]
let main args =
    webHost args {
        endpoints [
            get "/html" handleHtml

            get "/json" handleJson

            any "/" handlePlainText
        ]
    }
    0