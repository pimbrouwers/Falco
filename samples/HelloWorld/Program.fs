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

[<EntryPoint>]
let main args =        
    Host.startWebHostDefault 
        args 
        [
            get "/html" (Response.ofHtml (layout message))
            get "/json" (Response.ofJson {| Message = message |})
            get "/"     (Response.ofPlainText message)
        ]
    0