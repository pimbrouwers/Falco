module HelloWorld.Program

open Falco
open Falco.Markup

let message = "Hello, world!"

let layout message =
    Elem.html [] [
            Elem.head [] [
                    Elem.title [] [ Text.raw message ]
                ]
            Elem.body [] [
                    Elem.h1 [] [ Text.raw message ]
                ]
        ]

let handleIndex =
    get "/" (Response.ofPlainText message)

let handleJson =
    get "/json" (Response.ofJson {| Message = message |})

let handleHtml =
    get "/html" (Response.ofHtml (layout message))

[<EntryPoint>]
let main args =        
    Host.startWebHostDefault 
        args 
        [
            handleHtml
            handleJson
            handleIndex
        ]
    0