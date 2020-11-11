module HelloWorld.Program

open Falco
open Falco.Markup

let layout message =
    Elem.html [] [
            Elem.head [] [
                    Elem.title [] [ Text.raw message ]
                ]
            Elem.body [] [
                    Elem.h1 [] [ Text.raw message ]
                ]
        ]

let endpoints = 
    [            
        get "/json" 
            (Response.ofJson {| Message = "Hello from /json" |})

        get "/html" 
            (Response.ofHtml (layout "Hello from /html" ))

        get "/" 
            (Response.ofPlainText "Hello from /")
    ]

[<EntryPoint>]
let main args =            
    Host.startWebHostDefault args endpoints
    0