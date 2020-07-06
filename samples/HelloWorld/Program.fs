module HelloWorld.Program

open Falco
open Falco.Markup

let message = "Hello, world!"

let handleHello =
    get "/" (Response.ofPlainText message)
 
let handleJson =
    get "/json" (Response.ofJson {| Message = message |})

let handleHtml =
    let html =
        Elem.html [] [
                Elem.head [] [
                        Elem.title [] [ raw message ]
                    ]
                Elem.body [] [
                        Elem.h1 [] [ raw message ]
                    ]
            ]

    get "/html" (Response.ofHtml html)

[<EntryPoint>]
let main args =        
    Host.startWebHostDefault 
        args 
        [
            handleHtml
            handleJson
            handleHello
        ]
    0