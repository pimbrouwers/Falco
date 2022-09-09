module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

/// GET /
let handlePlainText : HttpHandler =
    Response.ofPlainText "Hello world"

/// GET /json
let handleJson : HttpHandler =
    let message = {| Message = "Hello from /json" |}
    Response.ofJson message

/// GET /html
let handleHtml : HttpHandler =
    let html =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "style.css"; Attr.rel "stylesheet" ] ]
            [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]

    Response.ofHtml html

webHost [||] {
    use_https

    endpoints [
        get "/json" handleJson

        get "/html" handleHtml

        any "/" handlePlainText
    ]
}
