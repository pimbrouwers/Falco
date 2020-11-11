module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing

let endpoints = 
    [            
        get "/greet/{name:alpha}"
            (Request.mapRoute (fun r -> r.["name"] |> sprintf "Hi %s") Response.ofPlainText)

        get "/json" 
            (Response.ofJson {| Message = "Hello from /json" |})

        get "/html" 
            (Response.ofHtml (Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]))

        get "/" 
            (Response.ofPlainText "Hello from /")
    ]

[<EntryPoint>]
let main args =            
    Host.startWebHostDefault args endpoints
    0