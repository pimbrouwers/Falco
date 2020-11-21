namespace Falco.Basic

open Falco
open Falco.Markup
open Falco.Routing

module Main = 
    let endpoints = 
        [            
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