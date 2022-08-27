module HelloWorld.Program

// open Falco
// open Falco.Markup
// open Falco.Routing
// open Falco.HostBuilder

// /// ANY /
// let handlePlainText : HttpHandler =
//     // Response.ofPlainText "Hello world"
//     fun ctx -> failwith "EXCEPTION"

// /// GET /json
// let handleJson : HttpHandler =
//     let message = {| Message = "Hello from /json" |}
//     Response.ofJson message

// /// GET /html
// let handleHtml : HttpHandler =
//     let html =
//         Templates.html5 "en"
//             [ Elem.link [ Attr.href "style.css"; Attr.rel "stylesheet" ] ]
//             [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]

//     Response.ofHtml html

// webHost [||] {
//     use_https

//     endpoints [
//         get "/html" handleHtml

//         get "/json" handleJson

//         any "/" handlePlainText
//     ]
// }
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    endpoints [
        any "/"      (Response.ofPlainText "/")
        get "/hello" (Response.ofPlainText "/hello")
        get "/debug" Request.debug
        all "/form"  [GET, Response.ofPlainText "/form"
                      POST, Request.mapJson Response.ofJson]
    ]
}