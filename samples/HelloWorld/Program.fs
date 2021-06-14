module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

// ------------
// Handlers 
// ------------
let handleFormGet : HttpHandler =
    [
        Elem.form [ Attr.method "post"; Attr.enctype "multipart/form-data" ] [
            Elem.input [ Attr.type' "file"; Attr.name "file" ] 
            Elem.input [ Attr.type' "submit" ]
        ]
    ]
    |> Templates.html5 "en" []
    |> Response.ofHtml

let handleFormPost : HttpHandler =
    let formBinder (f : FormCollectionReader) =
        let myFile = f.TryGetFormFile "file"
        "1"

    Request.mapForm formBinder Response.ofPlainText

let handlePlainText : HttpHandler =
    "Hello world"
    |> Response.ofPlainText 

let handleJson : HttpHandler =
    {| Message = "Hello world" |}
    |> Response.ofJson 

let handleHtml : HttpHandler =
    Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello world" ] ]
    |> Response.ofHtml

[<EntryPoint>]
let main args =      
    webHost args {
        endpoints [   
            all "/form"  [
                GET,  handleFormGet
                POST, handleFormPost
            ]

            get "/html" handleHtml 

            get "/json" handleJson

            get "/" handlePlainText
        ]
    }        
    0    